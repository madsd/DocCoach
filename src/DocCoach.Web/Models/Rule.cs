namespace DocCoach.Web.Models;

/// <summary>
/// A single review rule within a guideline set.
/// </summary>
public class Rule
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>High-level review dimension this rule belongs to.</summary>
    public ReviewDimension Dimension { get; set; }
    
    /// <summary>Specific type of rule for granular categorization.</summary>
    public RuleType RuleType { get; set; } = RuleType.Custom;
    
    /// <summary>How this rule should be evaluated.</summary>
    public EvaluationMode EvaluationMode { get; set; } = EvaluationMode.AIBased;
    
    /// <summary>The scope at which this rule is evaluated.</summary>
    public EvaluationScope Scope { get; set; } = EvaluationScope.Paragraph;
    
    /// <summary>Short name (e.g., "Active Voice").</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Full description of the rule.</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>Importance 1-10 (affects score calculation).</summary>
    public int Weight { get; set; } = 5;
    
    /// <summary>Example violations or good practices.</summary>
    public List<string> Examples { get; set; } = new();
    
    /// <summary>Whether to apply this rule.</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>Display order within the category.</summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Configuration for static evaluation (e.g., thresholds, patterns).
    /// Stored as JSON for flexibility.
    /// </summary>
    public string? RuleConfiguration { get; set; }
    
    /// <summary>
    /// Custom prompt template for AI-based evaluation.
    /// </summary>
    public string? AIPromptTemplate { get; set; }
    
    /// <summary>
    /// Gets the effective dimension, preferring explicit Dimension for typed rules.
    /// Falls back to legacy Category only for Custom rules when Dimension is not set.
    /// </summary>
    public ReviewDimension EffectiveDimension => Dimension;
}
