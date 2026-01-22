using DocCoach.Web.Models;
using System.Diagnostics;

namespace DocCoach.Web.Services.Analyzers;

/// <summary>
/// Orchestrates multiple analyzers to produce a comprehensive review.
/// Manages analyzer execution order, aggregates results, and calculates scores.
/// </summary>
public interface IReviewAnalyzerPipeline
{
    /// <summary>
    /// Executes all applicable analyzers and produces aggregated results.
    /// </summary>
    /// <param name="context">Analysis context with document and criteria.</param>
    /// <returns>Aggregated analysis result from all analyzers.</returns>
    Task<PipelineResult> ExecuteAsync(AnalysisContext context);
    
    /// <summary>
    /// Gets all registered analyzers.
    /// </summary>
    IReadOnlyList<IReviewAnalyzer> Analyzers { get; }
}

/// <summary>
/// Result from the analyzer pipeline containing aggregated feedback.
/// </summary>
public class PipelineResult
{
    /// <summary>All feedback items from all analyzers.</summary>
    public List<FeedbackItem> FeedbackItems { get; init; } = new();
    
    /// <summary>Results from individual analyzers.</summary>
    public List<AnalysisResult> AnalyzerResults { get; init; } = new();
    
    /// <summary>Total processing time in milliseconds.</summary>
    public long TotalProcessingTimeMs { get; init; }
    
    /// <summary>Overall quality score (0-100).</summary>
    public int OverallScore { get; init; }
    
    /// <summary>Scores by dimension.</summary>
    public Dictionary<ReviewDimension, int> DimensionScores { get; init; } = new();
    
    /// <summary>Whether the pipeline completed successfully.</summary>
    public bool Success { get; init; } = true;
    
    /// <summary>Errors from failed analyzers.</summary>
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Default implementation of the review analyzer pipeline.
/// </summary>
public class ReviewAnalyzerPipeline : IReviewAnalyzerPipeline
{
    private readonly IEnumerable<IReviewAnalyzer> _analyzers;
    private readonly ILogger<ReviewAnalyzerPipeline> _logger;
    
    public ReviewAnalyzerPipeline(
        IEnumerable<IReviewAnalyzer> analyzers,
        ILogger<ReviewAnalyzerPipeline> logger)
    {
        _analyzers = analyzers.OrderBy(a => a.Priority).ToList();
        _logger = logger;
    }
    
    public IReadOnlyList<IReviewAnalyzer> Analyzers => _analyzers.ToList();
    
    public async Task<PipelineResult> ExecuteAsync(AnalysisContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var allFeedbackItems = new List<FeedbackItem>();
        var analyzerResults = new List<AnalysisResult>();
        var errors = new List<string>();
        
        // Get applicable rules
        var enabledRules = context.Rules.Where(r => r.IsEnabled).ToList();
        
        if (!enabledRules.Any())
        {
            _logger.LogWarning("No enabled rules found for analysis");
            return new PipelineResult
            {
                Success = true,
                TotalProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                OverallScore = 100,
                DimensionScores = new Dictionary<ReviewDimension, int>()
            };
        }
        
        // Find applicable analyzers
        var applicableAnalyzers = _analyzers
            .Where(a => a.CanAnalyze(enabledRules))
            .OrderBy(a => a.Priority)
            .ToList();
        
        _logger.LogInformation(
            "Starting analysis pipeline with {AnalyzerCount} analyzers for {RuleCount} rules",
            applicableAnalyzers.Count,
            enabledRules.Count);
        
        // Calculate total rule count for accurate progress reporting
        var totalRules = enabledRules.Count;
        var completedRules = 0;
        
        foreach (var analyzer in applicableAnalyzers)
        {
            // Filter rules relevant to this analyzer
            var relevantRules = enabledRules
                .Where(r => analyzer.SupportedRuleTypes.Contains(r.RuleType))
                .ToList();
            
            if (!relevantRules.Any())
            {
                continue;
            }
            
            // Report progress before starting this analyzer's rules
            context.OnProgress?.Invoke(AnalysisProgress.Analyzing(
                relevantRules.First().Name,
                completedRules + 1,
                totalRules,
                (int)((completedRules / (double)totalRules) * 100)));
            
            try
            {
                _logger.LogDebug("Running analyzer: {AnalyzerName} with {RuleCount} rules", 
                    analyzer.Name, relevantRules.Count);
                
                var analyzerContext = new AnalysisContext
                {
                    Document = context.Document,
                    DocumentText = context.DocumentText,
                    GuidelineSet = context.GuidelineSet,
                    Rules = relevantRules,
                    CancellationToken = context.CancellationToken,
                    RuleStartIndex = completedRules,
                    TotalRuleCount = totalRules,
                    OnProgress = context.OnProgress // Pass through progress callback
                };
                
                var result = await analyzer.AnalyzeAsync(analyzerContext);
                analyzerResults.Add(result);
                
                if (result.Success)
                {
                    allFeedbackItems.AddRange(result.FeedbackItems);
                    _logger.LogDebug(
                        "Analyzer {AnalyzerName} completed with {FeedbackCount} feedback items",
                        analyzer.Name,
                        result.FeedbackItems.Count);
                }
                else
                {
                    errors.Add($"{analyzer.Name}: {result.ErrorMessage}");
                    _logger.LogWarning(
                        "Analyzer {AnalyzerName} failed: {Error}",
                        analyzer.Name,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{analyzer.Name}: {ex.Message}");
                _logger.LogError(ex, "Error running analyzer {AnalyzerName}", analyzer.Name);
                
                analyzerResults.Add(new AnalysisResult
                {
                    AnalyzerId = analyzer.Id,
                    AnalyzerName = analyzer.Name,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
            
            completedRules += relevantRules.Count;
            var progress = (int)((completedRules / (double)totalRules) * 100);
            context.OnProgress?.Invoke(AnalysisProgress.Analyzing(
                relevantRules.Last().Name,
                completedRules,
                totalRules,
                progress));
        }
        
        stopwatch.Stop();
        
        // Calculate scores
        var dimensionScores = CalculateDimensionScores(allFeedbackItems, enabledRules);
        var overallScore = CalculateOverallScore(dimensionScores, enabledRules);
        
        _logger.LogInformation(
            "Analysis pipeline completed in {ElapsedMs}ms with {FeedbackCount} total feedback items, overall score: {Score}",
            stopwatch.ElapsedMilliseconds,
            allFeedbackItems.Count,
            overallScore);
        
        return new PipelineResult
        {
            FeedbackItems = allFeedbackItems,
            AnalyzerResults = analyzerResults,
            TotalProcessingTimeMs = stopwatch.ElapsedMilliseconds,
            OverallScore = overallScore,
            DimensionScores = dimensionScores,
            Success = errors.Count == 0,
            Errors = errors
        };
    }
    
    private Dictionary<ReviewDimension, int> CalculateDimensionScores(
        List<FeedbackItem> feedbackItems,
        List<Rule> rules)
    {
        var scores = new Dictionary<ReviewDimension, int>();
        var dimensions = Enum.GetValues<ReviewDimension>();
        
        foreach (var dimension in dimensions)
        {
            var dimensionRules = rules.Where(r => r.EffectiveDimension == dimension).ToList();
            var dimensionFeedback = feedbackItems.Where(f => f.EffectiveDimension == dimension).ToList();
            
            if (!dimensionRules.Any())
            {
                // No rules for this dimension, don't include in scores
                continue;
            }
            
            // Calculate penalty based on severity and weight
            var maxWeight = dimensionRules.Sum(r => r.Weight * 10); // Max possible penalty
            var actualPenalty = dimensionFeedback.Sum(f =>
            {
                var severityMultiplier = f.Severity switch
                {
                    FeedbackSeverity.Error => 3.0,
                    FeedbackSeverity.Warning => 1.5,
                    FeedbackSeverity.Info => 0.5,
                    _ => 1.0
                };
                
                var rule = rules.FirstOrDefault(r => r.RuleType == f.RuleType);
                var weight = rule?.Weight ?? 5;
                
                return (int)(weight * severityMultiplier);
            });
            
            // Score is 100 minus penalty percentage
            var penaltyPercentage = maxWeight > 0 ? (actualPenalty / (double)maxWeight) * 100 : 0;
            scores[dimension] = Math.Max(0, Math.Min(100, 100 - (int)penaltyPercentage));
        }
        
        return scores;
    }
    
    private int CalculateOverallScore(
        Dictionary<ReviewDimension, int> dimensionScores,
        List<Rule> rules)
    {
        if (!dimensionScores.Any()) return 100;
        
        // Weight the dimensions based on rule weights
        var weightedSum = 0.0;
        var totalWeight = 0.0;
        
        foreach (var (dimension, score) in dimensionScores)
        {
            var dimensionWeight = rules
                .Where(r => r.EffectiveDimension == dimension)
                .Sum(r => r.Weight);
            
            if (dimensionWeight > 0)
            {
                weightedSum += score * dimensionWeight;
                totalWeight += dimensionWeight;
            }
        }
        
        if (totalWeight == 0)
        {
            return (int)dimensionScores.Values.Average();
        }
        
        return Math.Max(0, Math.Min(100, (int)(weightedSum / totalWeight)));
    }
}
