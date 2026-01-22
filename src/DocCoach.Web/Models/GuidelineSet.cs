namespace DocCoach.Web.Models;

/// <summary>
/// A configured collection of review rules.
/// </summary>
public class GuidelineSet
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>Display name (e.g., "Supreme Audit Office 2025").</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
    
    /// <summary>Path to uploaded guideline document.</summary>
    public string? SourceDocumentPath { get; set; }
    
    /// <summary>When created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>Last modification.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>Whether available for selection.</summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>Whether selected by default for new reviews.</summary>
    public bool IsDefault { get; set; }
    
    /// <summary>Extracted review rules.</summary>
    public List<Rule> Rules { get; set; } = new();
    
    /// <summary>
    /// Gets enabled rules only.
    /// </summary>
    public IEnumerable<Rule> EnabledRules => Rules.Where(r => r.IsEnabled);
}
