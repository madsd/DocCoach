namespace DocCoach.Web.Models;

/// <summary>
/// The result of AI analysis on a document.
/// </summary>
public class Review
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>Reference to reviewed document.</summary>
    public string DocumentId { get; set; } = string.Empty;
    
    /// <summary>Guideline set used for this review.</summary>
    public string GuidelineSetId { get; set; } = string.Empty;
    
    /// <summary>When review completed.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>Quality score 0-100.</summary>
    public int OverallScore { get; set; }
    
    /// <summary>Scores per review dimension (Language, Clarity, Completeness, etc.).</summary>
    public Dictionary<ReviewDimension, int> DimensionScores { get; set; } = new();
    
    /// <summary>Collection of feedback items.</summary>
    public List<FeedbackItem> FeedbackItems { get; set; } = new();
    
    /// <summary>How long AI analysis took in milliseconds.</summary>
    public long ProcessingTimeMs { get; set; }
    
    /// <summary>AI model identifier (e.g., "gpt-4o").</summary>
    public string ModelUsed { get; set; } = string.Empty;
    
    /// <summary>Brief summary of the document content (max 1000 chars).</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Extracted document text length (characters).</summary>
    public int ExtractedTextLength { get; set; }
    
    /// <summary>
    /// Gets feedback items filtered by dimension.
    /// </summary>
    public IEnumerable<FeedbackItem> GetFeedbackByDimension(ReviewDimension dimension)
        => FeedbackItems.Where(f => f.EffectiveDimension == dimension);
    
    /// <summary>
    /// Gets feedback items filtered by rule type.
    /// </summary>
    public IEnumerable<FeedbackItem> GetFeedbackByRuleType(RuleType ruleType)
        => FeedbackItems.Where(f => f.RuleType == ruleType);
    
    /// <summary>
    /// Gets feedback items filtered by severity.
    /// </summary>
    public IEnumerable<FeedbackItem> GetFeedbackBySeverity(FeedbackSeverity severity)
        => FeedbackItems.Where(f => f.Severity == severity);
    
    /// <summary>
    /// Gets the count of feedback items by severity.
    /// </summary>
    public Dictionary<FeedbackSeverity, int> GetSeverityCounts()
        => FeedbackItems
            .GroupBy(f => f.Severity)
            .ToDictionary(g => g.Key, g => g.Count());
    
    /// <summary>
    /// Gets the count of feedback items by dimension.
    /// </summary>
    public Dictionary<ReviewDimension, int> GetDimensionCounts()
        => FeedbackItems
            .GroupBy(f => f.EffectiveDimension)
            .ToDictionary(g => g.Key, g => g.Count());
}
