namespace DocCoach.Web.Models;

/// <summary>
/// A single piece of feedback within a review.
/// </summary>
public class FeedbackItem
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>High-level review dimension this feedback belongs to.</summary>
    public ReviewDimension Dimension { get; set; }
    
    /// <summary>Specific rule type that triggered this feedback.</summary>
    public RuleType RuleType { get; set; } = RuleType.Custom;
    
    /// <summary>Name of the specific rule that triggered this feedback (for display).</summary>
    public string? RuleName { get; set; }
    
    /// <summary>How critical the issue is.</summary>
    public FeedbackSeverity Severity { get; set; }
    
    /// <summary>Brief summary (≤100 chars).</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Detailed explanation.</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>Recommended improvement (optional).</summary>
    public string? Suggestion { get; set; }
    
    /// <summary>Where in document this applies.</summary>
    public DocumentLocation Location { get; set; } = new();
    
    /// <summary>ID of the analyzer that generated this feedback.</summary>
    public string? AnalyzerId { get; set; }
    
    /// <summary>Confidence score from the analyzer (0.0 to 1.0).</summary>
    public double? Confidence { get; set; }
    
    /// <summary>
    /// Gets the effective dimension, falling back to Category mapping if Dimension not set.
    /// </summary>
    public ReviewDimension EffectiveDimension => Dimension;
}
