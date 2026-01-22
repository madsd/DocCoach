using DocCoach.Web.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DocCoach.Web.Services.Analyzers;

/// <summary>
/// Static analyzer for sentence length checking.
/// Identifies sentences that exceed configurable word count thresholds.
/// </summary>
public class SentenceLengthAnalyzer : ReviewAnalyzerBase
{
    private readonly ILogger<SentenceLengthAnalyzer> _logger;
    
    /// <summary>Default maximum words per sentence before warning.</summary>
    public const int DefaultWarningThreshold = 25;
    
    /// <summary>Default maximum words per sentence before error.</summary>
    public const int DefaultErrorThreshold = 40;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
    
    public SentenceLengthAnalyzer(ILogger<SentenceLengthAnalyzer> logger)
    {
        _logger = logger;
    }
    
    public override string Id => "sentence-length";
    public override string Name => "Sentence Length Analyzer";
    public override string Description => "Checks for overly long sentences that may impact readability";
    public override EvaluationMode EvaluationMode => EvaluationMode.Static;
    public override int Priority => 10; // Run early (fast)
    
    public override IReadOnlySet<ReviewDimension> SupportedDimensions => 
        new HashSet<ReviewDimension> { ReviewDimension.Language, ReviewDimension.Clarity };
    
    public override IReadOnlySet<RuleType> SupportedRuleTypes => 
        new HashSet<RuleType> { RuleType.SentenceLength };
    
    /// <summary>
    /// Configuration parameters for the sentence length analyzer.
    /// </summary>
    public override IReadOnlyList<ConfigParameter> ConfigParameters => new[]
    {
        ConfigParameter.Integer(
            name: "warningThreshold",
            displayName: "Warning Threshold",
            description: "Number of words at which a sentence triggers a warning",
            defaultValue: DefaultWarningThreshold,
            min: 10,
            max: 100),
        ConfigParameter.Integer(
            name: "maxWords",
            displayName: "Maximum Words (Error)",
            description: "Number of words at which a sentence triggers an error",
            defaultValue: DefaultErrorThreshold,
            min: 15,
            max: 200)
    };
    
    public override async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var feedbackItems = new List<FeedbackItem>();
        
        try
        {
            // Get thresholds from rules configuration if available
            var rule = context.Rules
                .FirstOrDefault(r => r.RuleType == RuleType.SentenceLength);
            if (rule == null)
            {
                return new AnalysisResult
                {
                    AnalyzerId = Id,
                    AnalyzerName = Name,
                    Success = true,
                    FeedbackItems = feedbackItems,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    CoveredDimensions = SupportedDimensions,
                    EvaluatedRuleTypes = SupportedRuleTypes
                };
            }

            var ruleDimension = rule.Dimension;
            var ruleName = rule.Name;
            
            var warningThreshold = DefaultWarningThreshold;
            var errorThreshold = DefaultErrorThreshold;
            
            if (!string.IsNullOrEmpty(rule?.RuleConfiguration))
            {
                try
                {
                    var config = JsonSerializer.Deserialize<SentenceLengthConfig>(
                        rule.RuleConfiguration, JsonOptions);
                    if (config != null)
                    {
                        // Support both naming conventions: warningThreshold and WarningThreshold
                        warningThreshold = config.WarningThreshold ?? warningThreshold;
                        // Support both: maxWords (new) and ErrorThreshold (legacy)
                        errorThreshold = config.MaxWords ?? config.ErrorThreshold ?? errorThreshold;
                        
                        _logger.LogDebug(
                            "Loaded sentence length config: warningThreshold={Warning}, maxWords={Max}",
                            warningThreshold, errorThreshold);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse sentence length configuration: {Config}", 
                        rule.RuleConfiguration);
                }
            }
            
            // Split into sentences
            var sentences = SplitIntoSentences(context.DocumentText);
            var lineNumber = 1;
            var charPosition = 0;
            
            foreach (var sentence in sentences)
            {
                var wordCount = CountWords(sentence.Text);
                
                if (wordCount > errorThreshold)
                {
                    feedbackItems.Add(CreateFeedbackItem(
                        RuleType.SentenceLength,
                        FeedbackSeverity.Error,
                        $"Sentence has {wordCount} words",
                        $"This sentence contains {wordCount} words, which exceeds the maximum recommended length of {errorThreshold} words. Long sentences can be difficult to read and understand.",
                        "Consider breaking this into two or more shorter sentences.",
                        CreateLocation(sentence, lineNumber),
                        confidence: 1.0,
                        dimension: ruleDimension,
                        ruleName: ruleName));
                }
                else if (wordCount > warningThreshold)
                {
                    feedbackItems.Add(CreateFeedbackItem(
                        RuleType.SentenceLength,
                        FeedbackSeverity.Warning,
                        $"Sentence has {wordCount} words",
                        $"This sentence contains {wordCount} words, which exceeds the recommended length of {warningThreshold} words.",
                        "Consider simplifying or splitting this sentence for better readability.",
                        CreateLocation(sentence, lineNumber),
                        confidence: 1.0,
                        dimension: ruleDimension,
                        ruleName: ruleName));
                }
                
                // Track line numbers
                lineNumber += sentence.Text.Count(c => c == '\n');
                charPosition += sentence.Text.Length;
            }
            
            stopwatch.Stop();
            
            _logger.LogDebug(
                "Sentence length analysis complete: {SentenceCount} sentences, {IssueCount} issues found",
                sentences.Count,
                feedbackItems.Count);
            
            return new AnalysisResult
            {
                AnalyzerId = Id,
                AnalyzerName = Name,
                Success = true,
                FeedbackItems = feedbackItems,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                CoveredDimensions = SupportedDimensions,
                EvaluatedRuleTypes = SupportedRuleTypes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sentence length analysis");
            
            return new AnalysisResult
            {
                AnalyzerId = Id,
                AnalyzerName = Name,
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }
    
    private List<SentenceInfo> SplitIntoSentences(string text)
    {
        var sentences = new List<SentenceInfo>();
        
        // Split on sentence-ending punctuation, keeping the delimiter
        var pattern = @"(?<=[.!?])\s+";
        var parts = Regex.Split(text, pattern);
        
        var position = 0;
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                sentences.Add(new SentenceInfo
                {
                    Text = trimmed,
                    StartPosition = position,
                    EndPosition = position + part.Length
                });
            }
            position += part.Length;
        }
        
        return sentences;
    }
    
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        
        // Split on whitespace and count non-empty parts
        return Regex.Matches(text, @"\b[\w']+\b").Count;
    }
    
    private DocumentLocation CreateLocation(SentenceInfo sentence, int lineNumber)
    {
        // Create an excerpt (first 50 chars)
        var excerpt = sentence.Text.Length > 50 
            ? sentence.Text[..50] + "..." 
            : sentence.Text;
        
        return new DocumentLocation
        {
            StartOffset = sentence.StartPosition,
            EndOffset = sentence.EndPosition,
            Excerpt = excerpt
        };
    }
    
    private class SentenceInfo
    {
        public string Text { get; init; } = "";
        public int StartPosition { get; init; }
        public int EndPosition { get; init; }
    }
    
    /// <summary>
    /// Configuration model supporting both legacy and new property names.
    /// </summary>
    private class SentenceLengthConfig
    {
        /// <summary>Warning threshold (words).</summary>
        public int? WarningThreshold { get; set; }
        
        /// <summary>Error threshold - new naming convention (words).</summary>
        public int? MaxWords { get; set; }
        
        /// <summary>Error threshold - legacy naming convention (words).</summary>
        public int? ErrorThreshold { get; set; }
    }
}
