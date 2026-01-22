using System.ComponentModel.DataAnnotations;

namespace DocCoach.Web.Models.ViewModels;

/// <summary>
/// View model for creating/editing a rule.
/// </summary>
public class RuleViewModel
{
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string Name { get; set; } = "";
    
    [Required(ErrorMessage = "Description is required")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
    public string Description { get; set; } = "";
    
    /// <summary>
    /// High-level review dimension this rule belongs to.
    /// </summary>
    [Required(ErrorMessage = "Review dimension is required")]
    public ReviewDimension Dimension { get; set; } = ReviewDimension.Language;
    
    /// <summary>
    /// Specific type of rule for granular categorization.
    /// </summary>
    public RuleType RuleType { get; set; } = RuleType.Custom;
    
    /// <summary>
    /// How this rule should be evaluated.
    /// </summary>
    public EvaluationMode EvaluationMode { get; set; } = EvaluationMode.AIBased;
    
    /// <summary>
    /// The scope at which this rule is evaluated.
    /// </summary>
    public EvaluationScope Scope { get; set; } = EvaluationScope.Paragraph;
    
    [Range(1, 10, ErrorMessage = "Weight must be between 1 and 10")]
    public int Weight { get; set; } = 5;
    
    public bool IsEnabled { get; set; } = true;
    
    public int Order { get; set; }
    
    /// <summary>
    /// Configuration for static evaluation (JSON).
    /// </summary>
    public string? RuleConfiguration { get; set; }
    
    /// <summary>
    /// Custom prompt template for AI-based evaluation.
    /// </summary>
    public string? AIPromptTemplate { get; set; }
    
    /// <summary>
    /// Create a view model from an existing rule.
    /// </summary>
    public static RuleViewModel FromModel(Rule model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Description = model.Description,
        Dimension = model.Dimension,
        RuleType = model.RuleType,
        EvaluationMode = model.EvaluationMode,
        Scope = model.Scope,
        Weight = model.Weight,
        IsEnabled = model.IsEnabled,
        Order = model.Order,
        RuleConfiguration = model.RuleConfiguration,
        AIPromptTemplate = model.AIPromptTemplate
    };
    
    /// <summary>
    /// Apply changes to an existing rule model.
    /// </summary>
    public void ApplyTo(Rule model)
    {
        model.Name = Name;
        model.Description = Description;
        model.Dimension = Dimension;
        model.RuleType = RuleType;
        model.EvaluationMode = EvaluationMode;
        model.Scope = Scope;
        model.Weight = Weight;
        model.IsEnabled = IsEnabled;
        model.Order = Order;
        model.RuleConfiguration = RuleConfiguration;
        model.AIPromptTemplate = AIPromptTemplate;
    }
    
    /// <summary>
    /// Create a new rule model from this view model.
    /// </summary>
    public Rule ToModel() => new()
    {
        Name = Name,
        Description = Description,
        Dimension = Dimension,
        RuleType = RuleType,
        EvaluationMode = EvaluationMode,
        Scope = Scope,
        Weight = Weight,
        IsEnabled = IsEnabled,
        Order = Order,
        RuleConfiguration = RuleConfiguration,
        AIPromptTemplate = AIPromptTemplate
    };
}
