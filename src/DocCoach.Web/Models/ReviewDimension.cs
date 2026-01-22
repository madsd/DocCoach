namespace DocCoach.Web.Models;

/// <summary>
/// High-level dimensions for document review categorization.
/// Each dimension groups related criteria and feedback items.
/// </summary>
public enum ReviewDimension
{
    /// <summary>
    /// Grammar, spelling, punctuation, and sentence construction.
    /// Includes: sentence length, passive voice, jargon usage.
    /// </summary>
    Language,
    
    /// <summary>
    /// Readability, logical flow, and comprehension.
    /// Includes: plain language, paragraph structure, transitions.
    /// </summary>
    Clarity,
    
    /// <summary>
    /// Required sections, content coverage, and thoroughness.
    /// Includes: executive summary, methodology, findings coverage.
    /// </summary>
    Completeness,
    
    /// <summary>
    /// Evidence citations, data references, and source attribution.
    /// Includes: supporting evidence, data accuracy, reference quality.
    /// </summary>
    FactualSupport,
    
    /// <summary>
    /// Adherence to templates, format standards, and structural requirements.
    /// Includes: template conformance, naming conventions, formatting rules.
    /// </summary>
    Compliance,
    
    /// <summary>
    /// Objectivity, professional tone, and stylistic consistency.
    /// Includes: neutral language, formal style, consistent voice.
    /// </summary>
    ToneAndStyle,
    
    /// <summary>
    /// Document organization, section flow, and logical structure.
    /// Includes: heading hierarchy, section ordering, content grouping.
    /// </summary>
    Structure
}

/// <summary>
/// Extension methods for ReviewDimension enum.
/// </summary>
public static class ReviewDimensionExtensions
{
    /// <summary>
    /// Gets a human-readable display name for the dimension.
    /// </summary>
    public static string GetDisplayName(this ReviewDimension dimension) => dimension switch
    {
        ReviewDimension.Language => "Language Quality",
        ReviewDimension.Clarity => "Clarity & Readability",
        ReviewDimension.Completeness => "Content Completeness",
        ReviewDimension.FactualSupport => "Factual Support",
        ReviewDimension.Compliance => "Specification Compliance",
        ReviewDimension.ToneAndStyle => "Tone & Style",
        ReviewDimension.Structure => "Document Structure",
        _ => dimension.ToString()
    };
    
    /// <summary>
    /// Gets a short description of what the dimension evaluates.
    /// </summary>
    public static string GetDescription(this ReviewDimension dimension) => dimension switch
    {
        ReviewDimension.Language => "Grammar, spelling, punctuation, and sentence construction",
        ReviewDimension.Clarity => "Readability, logical flow, and comprehension",
        ReviewDimension.Completeness => "Required sections, content coverage, and thoroughness",
        ReviewDimension.FactualSupport => "Evidence citations, data references, and source attribution",
        ReviewDimension.Compliance => "Adherence to templates, format standards, and structural requirements",
        ReviewDimension.ToneAndStyle => "Objectivity, professional tone, and stylistic consistency",
        ReviewDimension.Structure => "Document organization, section flow, and logical structure",
        _ => string.Empty
    };
    
    /// <summary>
    /// Gets an icon identifier for UI display.
    /// </summary>
    public static string GetIcon(this ReviewDimension dimension) => dimension switch
    {
        ReviewDimension.Language => "Spellcheck",
        ReviewDimension.Clarity => "Visibility",
        ReviewDimension.Completeness => "Checklist",
        ReviewDimension.FactualSupport => "FactCheck",
        ReviewDimension.Compliance => "Rule",
        ReviewDimension.ToneAndStyle => "RecordVoiceOver",
        ReviewDimension.Structure => "AccountTree",
        _ => "Help"
    };
}
