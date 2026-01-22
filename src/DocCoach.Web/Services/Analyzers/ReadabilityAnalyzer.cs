using DocCoach.Web.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DocCoach.Web.Services.Analyzers;

/// <summary>
/// Static analyzer for readability metrics.
/// Calculates Flesch-Kincaid readability scores and identifies complex text.
/// </summary>
public class ReadabilityAnalyzer : ReviewAnalyzerBase
{
    private readonly ILogger<ReadabilityAnalyzer> _logger;
    
    /// <summary>Target reading level (grade). Documents above this get warnings.</summary>
    public const double DefaultTargetGradeLevel = 12.0;
    
    /// <summary>Maximum acceptable reading level before error.</summary>
    public const double DefaultMaxGradeLevel = 16.0;
    
    public ReadabilityAnalyzer(ILogger<ReadabilityAnalyzer> logger)
    {
        _logger = logger;
    }
    
    public override string Id => "readability";
    public override string Name => "Readability Analyzer";
    public override string Description => "Calculates readability scores and identifies complex passages";
    public override EvaluationMode EvaluationMode => EvaluationMode.Static;
    public override int Priority => 15; // Run early (fast)
    
    public override IReadOnlySet<ReviewDimension> SupportedDimensions => 
        new HashSet<ReviewDimension> { ReviewDimension.Clarity };
    
    public override IReadOnlySet<RuleType> SupportedRuleTypes => 
        new HashSet<RuleType> 
        { 
            RuleType.ReadabilityScore,
            RuleType.PlainLanguage
        };
    
    /// <summary>
    /// Configuration parameters for the readability analyzer.
    /// </summary>
    public override IReadOnlyList<ConfigParameter> ConfigParameters => new[]
    {
        ConfigParameter.Integer(
            name: "targetGradeLevel",
            displayName: "Target Grade Level",
            description: "Target reading grade level for warnings (e.g., 12 = high school senior)",
            defaultValue: (int)DefaultTargetGradeLevel,
            min: 6,
            max: 18),
        ConfigParameter.Integer(
            name: "maxGradeLevel",
            displayName: "Maximum Grade Level",
            description: "Maximum reading grade level before triggering errors",
            defaultValue: (int)DefaultMaxGradeLevel,
            min: 10,
            max: 20),
        ConfigParameter.Integer(
            name: "minFleschScore",
            displayName: "Minimum Flesch Score",
            description: "Minimum Flesch Reading Ease score (0-100, higher is easier)",
            defaultValue: 40,
            min: 0,
            max: 100)
    };
    
    public override async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var feedbackItems = new List<FeedbackItem>();
        
        try
        {
            var rulesByType = context.Rules
                .Where(r => r.IsEnabled)
                .GroupBy(r => r.RuleType)
                .ToDictionary(g => g.Key, g => g.First());

            var readabilityRule = rulesByType.GetValueOrDefault(RuleType.ReadabilityScore);
            var plainLanguageRule = rulesByType.GetValueOrDefault(RuleType.PlainLanguage);

            // Calculate overall document readability
            var documentStats = CalculateTextStatistics(context.DocumentText);
            var fleschKincaid = CalculateFleschKincaidGrade(documentStats);
            var fleschEase = CalculateFleschReadingEase(documentStats);
            
            _logger.LogDebug(
                "Document readability: FK Grade {Grade:F1}, Flesch Ease {Ease:F1}",
                fleschKincaid,
                fleschEase);
            
            // Check overall document readability
                if (readabilityRule != null && fleschKincaid > DefaultMaxGradeLevel)
            {
                feedbackItems.Add(CreateFeedbackItem(
                    RuleType.ReadabilityScore,
                    FeedbackSeverity.Error,
                    $"Document reading level: Grade {fleschKincaid:F1}",
                    $"The overall document requires a Grade {fleschKincaid:F1} reading level, which is very difficult for general audiences. The target is Grade {DefaultTargetGradeLevel} or below.",
                    "Consider simplifying vocabulary, shortening sentences, and breaking up complex paragraphs.",
                    new DocumentLocation { Section = "Overall Document" },
                    confidence: 1.0,
                    dimension: readabilityRule.Dimension,
                    ruleName: readabilityRule.Name));
            }
                else if (readabilityRule != null && fleschKincaid > DefaultTargetGradeLevel)
            {
                feedbackItems.Add(CreateFeedbackItem(
                    RuleType.ReadabilityScore,
                    FeedbackSeverity.Warning,
                    $"Document reading level: Grade {fleschKincaid:F1}",
                    $"The overall document requires a Grade {fleschKincaid:F1} reading level. Consider targeting Grade {DefaultTargetGradeLevel} or below for better accessibility.",
                    "Try using shorter sentences and simpler words where possible.",
                    new DocumentLocation { Section = "Overall Document" },
                    confidence: 1.0,
                    dimension: readabilityRule.Dimension,
                    ruleName: readabilityRule.Name));
            }
                else if (readabilityRule != null)
            {
                feedbackItems.Add(CreateFeedbackItem(
                    RuleType.ReadabilityScore,
                    FeedbackSeverity.Info,
                    $"Good readability: Grade {fleschKincaid:F1}",
                    $"The document has good readability at Grade {fleschKincaid:F1}, which is at or below the target of Grade {DefaultTargetGradeLevel}.",
                    null,
                    new DocumentLocation { Section = "Overall Document" },
                    confidence: 1.0,
                    dimension: readabilityRule.Dimension,
                    ruleName: readabilityRule.Name));
            }
            
            // Analyze individual paragraphs for problem areas
            var paragraphs = SplitIntoParagraphs(context.DocumentText);
            var paragraphNumber = 1;
            
            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Text.Length < 100) // Skip very short paragraphs
                {
                    paragraphNumber++;
                    continue;
                }
                
                var paragraphStats = CalculateTextStatistics(paragraph.Text);
                var paragraphGrade = CalculateFleschKincaidGrade(paragraphStats);
                
                if (plainLanguageRule != null && paragraphGrade > DefaultMaxGradeLevel + 2) // Higher threshold for individual paragraphs
                {
                    var excerpt = paragraph.Text.Length > 60 
                        ? paragraph.Text[..60] + "..." 
                        : paragraph.Text;
                    
                    feedbackItems.Add(CreateFeedbackItem(
                        RuleType.PlainLanguage,
                        FeedbackSeverity.Warning,
                        $"Complex paragraph (Grade {paragraphGrade:F1})",
                        $"This paragraph has a high reading level of Grade {paragraphGrade:F1}, making it difficult to understand.",
                        "Consider breaking this paragraph into smaller chunks and using simpler language.",
                        new DocumentLocation
                        {
                            StartOffset = paragraph.StartPosition,
                            EndOffset = paragraph.EndPosition,
                            Excerpt = excerpt
                        },
                        confidence: 0.9,
                        dimension: plainLanguageRule.Dimension,
                        ruleName: plainLanguageRule.Name));
                }
                
                paragraphNumber++;
            }
            
            stopwatch.Stop();
            
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
            _logger.LogError(ex, "Error during readability analysis");
            
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
    
    private TextStatistics CalculateTextStatistics(string text)
    {
        var words = Regex.Matches(text, @"\b[\w']+\b");
        var sentences = Regex.Matches(text, @"[.!?]+");
        var syllables = words.Sum(w => CountSyllables(w.Value));
        
        return new TextStatistics
        {
            WordCount = words.Count,
            SentenceCount = Math.Max(1, sentences.Count),
            SyllableCount = syllables,
            CharacterCount = text.Length
        };
    }
    
    /// <summary>
    /// Calculates Flesch-Kincaid Grade Level.
    /// Higher = more difficult. Target for public documents is typically 8-12.
    /// </summary>
    private double CalculateFleschKincaidGrade(TextStatistics stats)
    {
        if (stats.WordCount == 0 || stats.SentenceCount == 0) return 0;
        
        var avgWordsPerSentence = (double)stats.WordCount / stats.SentenceCount;
        var avgSyllablesPerWord = (double)stats.SyllableCount / stats.WordCount;
        
        return 0.39 * avgWordsPerSentence + 11.8 * avgSyllablesPerWord - 15.59;
    }
    
    /// <summary>
    /// Calculates Flesch Reading Ease score.
    /// 0-30 = Very Difficult, 30-50 = Difficult, 50-60 = Fairly Difficult,
    /// 60-70 = Standard, 70-80 = Fairly Easy, 80-90 = Easy, 90-100 = Very Easy
    /// </summary>
    private double CalculateFleschReadingEase(TextStatistics stats)
    {
        if (stats.WordCount == 0 || stats.SentenceCount == 0) return 100;
        
        var avgWordsPerSentence = (double)stats.WordCount / stats.SentenceCount;
        var avgSyllablesPerWord = (double)stats.SyllableCount / stats.WordCount;
        
        return 206.835 - 1.015 * avgWordsPerSentence - 84.6 * avgSyllablesPerWord;
    }
    
    /// <summary>
    /// Estimates syllable count for a word using simple heuristics.
    /// </summary>
    private int CountSyllables(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0;
        
        word = word.ToLower().Trim();
        if (word.Length <= 3) return 1;
        
        // Count vowel groups
        var vowelGroups = Regex.Matches(word, @"[aeiouy]+").Count;
        
        // Subtract silent e at end
        if (word.EndsWith("e") && !word.EndsWith("le"))
        {
            vowelGroups = Math.Max(1, vowelGroups - 1);
        }
        
        // Handle common suffixes
        if (word.EndsWith("ed") && !word.EndsWith("ted") && !word.EndsWith("ded"))
        {
            vowelGroups = Math.Max(1, vowelGroups - 1);
        }
        
        return Math.Max(1, vowelGroups);
    }
    
    private List<ParagraphInfo> SplitIntoParagraphs(string text)
    {
        var paragraphs = new List<ParagraphInfo>();
        var pattern = @"\n\s*\n";
        var parts = Regex.Split(text, pattern);
        
        var position = 0;
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                paragraphs.Add(new ParagraphInfo
                {
                    Text = trimmed,
                    StartPosition = position,
                    EndPosition = position + part.Length
                });
            }
            position += part.Length + 2; // Account for newlines
        }
        
        return paragraphs;
    }
    
    private class TextStatistics
    {
        public int WordCount { get; init; }
        public int SentenceCount { get; init; }
        public int SyllableCount { get; init; }
        public int CharacterCount { get; init; }
    }
    
    private class ParagraphInfo
    {
        public string Text { get; init; } = "";
        public int StartPosition { get; init; }
        public int EndPosition { get; init; }
    }
}
