namespace DocCoach.Web.Models;

/// <summary>
/// Specific types of rules that can be evaluated.
/// Each rule type belongs to a ReviewDimension and defines what specific aspect is checked.
/// </summary>
public enum RuleType
{
    // ===== Language Dimension =====
    
    /// <summary>Checks sentence length against maximum word count threshold.</summary>
    SentenceLength,
    
    /// <summary>Detects passive voice constructions.</summary>
    PassiveVoice,
    
    
    // ===== Clarity Dimension =====
    
    /// <summary>Evaluates overall readability score (e.g., Flesch-Kincaid).</summary>
    ReadabilityScore,
       
    /// <summary>Evaluates use of plain language principles.</summary>
    PlainLanguage,
    
    // ===== General =====
    
    /// <summary>Custom or unspecified rule type.</summary>
    Custom,
    
    /// <summary>AI-powered semantic analysis with configurable prompt.</summary>
    AIAnalysis
}

/// <summary>
/// Defines how a rule should be evaluated.
/// </summary>
public enum EvaluationMode
{
    /// <summary>Static evaluation using patterns, regex, or algorithms.</summary>
    Static,
    
    /// <summary>AI/LLM-based evaluation for complex semantic analysis.</summary>
    AIBased
}

/// <summary>
/// Defines the scope at which a rule is evaluated.
/// </summary>
public enum EvaluationScope
{
    /// <summary>Evaluated at individual word level.</summary>
    Word,
    
    /// <summary>Evaluated at sentence level.</summary>
    Sentence,
    
    /// <summary>Evaluated at paragraph level.</summary>
    Paragraph,
    
    /// <summary>Evaluated at section level.</summary>
    Section,
    
    /// <summary>Evaluated at full document level.</summary>
    Document
}

/// <summary>
/// Extension methods for RuleType enum.
/// </summary>
public static class RuleTypeExtensions
{
    /// <summary>
    /// Gets the ReviewDimension this rule type belongs to.
    /// </summary>
    public static ReviewDimension GetDimension(this RuleType type) => type switch
    {
        // Language
        RuleType.SentenceLength => ReviewDimension.Language,
        RuleType.PassiveVoice => ReviewDimension.Language,
        
        
        // Clarity
        RuleType.ReadabilityScore => ReviewDimension.Clarity,        
        RuleType.PlainLanguage => ReviewDimension.Clarity,
      
        // AI Analysis - can span multiple dimensions
        RuleType.AIAnalysis => ReviewDimension.Clarity,
        
        _ => ReviewDimension.Language
    };
    
    /// <summary>
    /// Gets a human-readable display name for the rule type.
    /// </summary>
    public static string GetDisplayName(this RuleType type) => type switch
    {
        RuleType.SentenceLength => "Sentence Length",
        RuleType.PassiveVoice => "Passive Voice",        
        RuleType.ReadabilityScore => "Readability Score",        
        RuleType.PlainLanguage => "Plain Language",        
        RuleType.Custom => "Custom",
        RuleType.AIAnalysis => "AI Analysis",
        _ => type.ToString()
    };
    
    /// <summary>
    /// Gets the recommended evaluation mode for this rule type.
    /// </summary>
    public static EvaluationMode GetRecommendedEvaluationMode(this RuleType type) => type switch
    {
        // Static (fast, deterministic)
        RuleType.SentenceLength => EvaluationMode.Static,       
        RuleType.ReadabilityScore => EvaluationMode.Static,
        RuleType.PassiveVoice => EvaluationMode.Static,
        RuleType.PlainLanguage => EvaluationMode.Static,
        
        // AI Analysis
        RuleType.AIAnalysis => EvaluationMode.AIBased,
        
        _ => EvaluationMode.AIBased
    };
    
    /// <summary>
    /// Gets the typical evaluation scope for this rule type.
    /// </summary>
    public static EvaluationScope GetTypicalScope(this RuleType type) => type switch
    {
       
        // Sentence-level
        RuleType.SentenceLength => EvaluationScope.Sentence,
        RuleType.PassiveVoice => EvaluationScope.Sentence,
        RuleType.PlainLanguage => EvaluationScope.Sentence,
        
        // Document-level
        RuleType.ReadabilityScore => EvaluationScope.Document,
        RuleType.AIAnalysis => EvaluationScope.Document,
        
        _ => EvaluationScope.Document
    };
}
