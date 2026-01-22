using DocCoach.Web.Models;

namespace DocCoach.Web.Services.Interfaces;

/// <summary>
/// Manages guideline sets, rule configuration, and example documents.
/// </summary>
public interface IGuidelineService
{
    // Guideline Set Operations
    
    /// <summary>
    /// Get all guideline sets.
    /// </summary>
    /// <param name="activeOnly">If true, only return active sets</param>
    Task<IReadOnlyList<GuidelineSet>> GetAllAsync(bool activeOnly = true);

    /// <summary>
    /// Get guideline set by ID with all rules.
    /// </summary>
    Task<GuidelineSet?> GetByIdAsync(string id);

    /// <summary>
    /// Get the default guideline set.
    /// </summary>
    Task<GuidelineSet?> GetDefaultAsync();

    /// <summary>
    /// Create a new guideline set.
    /// </summary>
    Task<GuidelineSet> CreateAsync(string name, string? description = null);

    /// <summary>
    /// Update guideline set metadata.
    /// </summary>
    Task<GuidelineSet> UpdateAsync(string id, string name, string? description, bool isActive, bool isDefault);

    /// <summary>
    /// Delete a guideline set and all associated data.
    /// </summary>
    Task DeleteAsync(string id);

    // Rule Operations

    /// <summary>
    /// Add a rule to a guideline set.
    /// </summary>
    Task<Rule> AddRuleAsync(string guidelineSetId, Rule rule);

    /// <summary>
    /// Update a rule.
    /// </summary>
    Task<Rule> UpdateRuleAsync(string guidelineSetId, Rule rule);

    /// <summary>
    /// Remove a rule from a guideline set.
    /// </summary>
    Task RemoveRuleAsync(string guidelineSetId, string ruleId);

    // AI-Assisted Configuration

    /// <summary>
    /// Extract rules from an uploaded guideline document using AI.
    /// </summary>
    /// <param name="guidelineSetId">Target guideline set</param>
    /// <param name="fileName">Document filename</param>
    /// <param name="content">Document content stream</param>
    /// <returns>List of extracted rules (not yet saved)</returns>
    Task<IReadOnlyList<Rule>> ExtractRulesAsync(string guidelineSetId, string fileName, Stream content);

    /// <summary>
    /// Confirm and save extracted rules to guideline set.
    /// </summary>
    Task<GuidelineSet> ConfirmRulesAsync(string guidelineSetId, IEnumerable<Rule> rules);

    // Example Documents

    /// <summary>
    /// Get example documents for a guideline set.
    /// </summary>
    Task<IReadOnlyList<ExampleDocument>> GetExamplesAsync(string guidelineSetId);

    /// <summary>
    /// Upload an example document.
    /// </summary>
    Task<ExampleDocument> AddExampleAsync(string guidelineSetId, string fileName, string? description, Stream content);

    /// <summary>
    /// Remove an example document.
    /// </summary>
    Task RemoveExampleAsync(string guidelineSetId, string exampleId);
}
