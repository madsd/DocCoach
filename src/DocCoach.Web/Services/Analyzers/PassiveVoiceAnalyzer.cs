using DocCoach.Web.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DocCoach.Web.Services.Analyzers;

/// <summary>
/// Static analyzer for passive voice detection.
/// Uses pattern matching to identify passive voice constructions.
/// </summary>
public class PassiveVoiceAnalyzer : ReviewAnalyzerBase
{
    private readonly ILogger<PassiveVoiceAnalyzer> _logger;
    
    // Common forms of "to be" followed by past participle patterns
    private static readonly string[] BeVerbs = 
    { 
        "is", "are", "was", "were", "be", "been", "being",
        "has been", "have been", "had been", "will be", "would be",
        "could be", "should be", "may be", "might be", "must be"
    };
    
    // Common past participle endings
    private static readonly Regex PastParticiplePattern = new(
        @"\b(is|are|was|were|be|been|being|has been|have been|had been|will be|would be|could be|should be|may be|might be|must be)\s+(\w+ed|\w+en|\w+t)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    public PassiveVoiceAnalyzer(ILogger<PassiveVoiceAnalyzer> logger)
    {
        _logger = logger;
    }
    
    public override string Id => "passive-voice";
    public override string Name => "Passive Voice Analyzer";
    public override string Description => "Detects passive voice constructions that may reduce clarity";
    public override EvaluationMode EvaluationMode => EvaluationMode.Static;
    public override int Priority => 20;
    
    public override IReadOnlySet<ReviewDimension> SupportedDimensions => 
        new HashSet<ReviewDimension> { ReviewDimension.Language, ReviewDimension.Clarity };
    
    public override IReadOnlySet<RuleType> SupportedRuleTypes => 
        new HashSet<RuleType> { RuleType.PassiveVoice };
    
    /// <summary>
    /// Configuration parameters for the passive voice analyzer.
    /// </summary>
    public override IReadOnlyList<ConfigParameter> ConfigParameters => new[]
    {
        ConfigParameter.Percentage(
            name: "warningThreshold",
            displayName: "Warning Threshold (%)",
            description: "Percentage of sentences with passive voice that triggers a warning",
            defaultValue: 15),
        ConfigParameter.Percentage(
            name: "errorThreshold",
            displayName: "Error Threshold (%)",
            description: "Percentage of sentences with passive voice that triggers an error",
            defaultValue: 30),
        ConfigParameter.Integer(
            name: "maxInstancesReported",
            displayName: "Max Instances Reported",
            description: "Maximum number of individual passive voice instances to report",
            defaultValue: 10,
            min: 1,
            max: 50)
    };
    
    public override async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var feedbackItems = new List<FeedbackItem>();
        
        try
        {
            var rule = context.Rules
                .FirstOrDefault(r => r.RuleType == RuleType.PassiveVoice);
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

            var maxInstancesReported = 10;
            if (!string.IsNullOrEmpty(rule.RuleConfiguration))
            {
                try
                {
                    var config = System.Text.Json.JsonSerializer.Deserialize<PassiveVoiceConfig>(
                        rule.RuleConfiguration,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (config?.MaxInstancesReported != null)
                    {
                        maxInstancesReported = config.MaxInstancesReported.Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse passive voice configuration: {Config}", rule.RuleConfiguration);
                }
            }

            var ruleDimension = rule.Dimension;
            var ruleName = rule.Name;

            var sentences = SplitIntoSentences(context.DocumentText);
            var passiveCount = 0;
            var totalSentences = sentences.Count;
            
            foreach (var sentence in sentences)
            {
                var passiveMatches = FindPassiveConstructions(sentence.Text);
                
                foreach (var match in passiveMatches)
                {
                    passiveCount++;
                    
                    // Only flag the first few instances, then summarize
                    if (feedbackItems.Count < maxInstancesReported)
                    {
                        feedbackItems.Add(CreateFeedbackItem(
                            RuleType.PassiveVoice,
                            FeedbackSeverity.Info,
                            "Passive voice detected",
                            $"The phrase \"{match.MatchedText}\" uses passive voice. Active voice is generally clearer and more direct.",
                            TryGenerateActiveSuggestion(match.MatchedText),
                            new DocumentLocation
                            {
                                StartOffset = sentence.StartPosition + match.StartIndex,
                                EndOffset = sentence.StartPosition + match.EndIndex,
                                Excerpt = GetExcerpt(sentence.Text, match.StartIndex, match.EndIndex)
                            },
                            confidence: 0.8,
                            dimension: ruleDimension,
                            ruleName: ruleName));
                    }
                }
            }
            
            // Add summary if there are many passive constructions
            var passivePercentage = totalSentences > 0 
                ? (passiveCount / (double)totalSentences) * 100 
                : 0;
            
            if (passivePercentage > 30)
            {
                feedbackItems.Insert(0, CreateFeedbackItem(
                    RuleType.PassiveVoice,
                    FeedbackSeverity.Warning,
                    $"High passive voice usage: {passivePercentage:F0}%",
                    $"Found {passiveCount} passive voice constructions in {totalSentences} sentences ({passivePercentage:F0}%). Consider using more active voice for clarity.",
                    "Review sentences and convert passive constructions to active voice where appropriate.",
                    new DocumentLocation { Section = "Overall Document" },
                    confidence: 0.85,
                    dimension: ruleDimension,
                    ruleName: ruleName));
            }
            else if (passivePercentage > 15)
            {
                feedbackItems.Insert(0, CreateFeedbackItem(
                    RuleType.PassiveVoice,
                    FeedbackSeverity.Info,
                    $"Moderate passive voice: {passivePercentage:F0}%",
                    $"Found {passiveCount} passive voice constructions in {totalSentences} sentences ({passivePercentage:F0}%). Some passive voice is acceptable, but active voice is generally preferred.",
                    null,
                    new DocumentLocation { Section = "Overall Document" },
                    confidence: 0.85,
                    dimension: ruleDimension,
                    ruleName: ruleName));
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
            _logger.LogError(ex, "Error during passive voice analysis");
            
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
    
    private List<PassiveMatch> FindPassiveConstructions(string sentence)
    {
        var matches = new List<PassiveMatch>();
        
        foreach (Match match in PastParticiplePattern.Matches(sentence))
        {
            // Filter out false positives (common acceptable phrases)
            var phrase = match.Value.ToLower();
            if (!IsCommonFalsePositive(phrase))
            {
                matches.Add(new PassiveMatch
                {
                    MatchedText = match.Value,
                    StartIndex = match.Index,
                    EndIndex = match.Index + match.Length
                });
            }
        }
        
        return matches;
    }
    
    private bool IsCommonFalsePositive(string phrase)
    {
        // Common phrases that look like passive but are acceptable
        var falsePositives = new[]
        {
            "is expected", "are expected", "is required", "are required",
            "is needed", "are needed", "is located", "are located",
            "is based", "are based", "is used", "are used",
            "is designed", "are designed", "is intended", "are intended"
        };
        
        return falsePositives.Any(fp => phrase.Contains(fp));
    }
    
    private string? TryGenerateActiveSuggestion(string passivePhrase)
    {
        // Simple suggestions for common patterns
        if (passivePhrase.Contains("was reviewed"))
            return "Consider: '[Someone] reviewed [it]'";
        if (passivePhrase.Contains("was conducted"))
            return "Consider: '[Someone] conducted [it]'";
        if (passivePhrase.Contains("was performed"))
            return "Consider: '[Someone] performed [it]'";
        if (passivePhrase.Contains("were identified"))
            return "Consider: '[Someone/We] identified [them]'";
        if (passivePhrase.Contains("was found"))
            return "Consider: '[Someone/We] found [it]'";
        
        return "Consider rewriting with the actor as the subject (e.g., 'We reviewed...' instead of 'was reviewed by us').";
    }
    
    private string GetExcerpt(string text, int start, int end)
    {
        // Get some context around the match
        var contextStart = Math.Max(0, start - 20);
        var contextEnd = Math.Min(text.Length, end + 20);
        
        var excerpt = text[contextStart..contextEnd].Trim();
        
        if (contextStart > 0) excerpt = "..." + excerpt;
        if (contextEnd < text.Length) excerpt += "...";
        
        return excerpt;
    }
    
    private List<SentenceInfo> SplitIntoSentences(string text)
    {
        var sentences = new List<SentenceInfo>();
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
    
    private class SentenceInfo
    {
        public string Text { get; init; } = "";
        public int StartPosition { get; init; }
        public int EndPosition { get; init; }
    }
    
    private class PassiveMatch
    {
        public string MatchedText { get; init; } = "";
        public int StartIndex { get; init; }
        public int EndIndex { get; init; }
    }

    private class PassiveVoiceConfig
    {
        public int? MaxInstancesReported { get; set; }
    }
}
