# Contract: IGuidelineService

**Feature**: 001-audit-review-tool  
**Date**: 2026-01-05

## Purpose

Manages guideline sets, criteria configuration, and example documents.

## Interface Definition

```csharp
public interface IGuidelineService
{
    // Guideline Set Operations
    
    /// <summary>
    /// Get all guideline sets.
    /// </summary>
    /// <param name="activeOnly">If true, only return active sets</param>
    Task<IReadOnlyList<GuidelineSet>> GetAllAsync(bool activeOnly = true);

    /// <summary>
    /// Get guideline set by ID with all criteria.
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

    // Criteria Operations

    /// <summary>
    /// Add a criterion to a guideline set.
    /// </summary>
    Task<Criterion> AddCriterionAsync(string guidelineSetId, Criterion criterion);

    /// <summary>
    /// Update a criterion.
    /// </summary>
    Task<Criterion> UpdateCriterionAsync(string guidelineSetId, Criterion criterion);

    /// <summary>
    /// Remove a criterion from a guideline set.
    /// </summary>
    Task RemoveCriterionAsync(string guidelineSetId, string criterionId);

    // AI-Assisted Configuration

    /// <summary>
    /// Extract criteria from an uploaded guideline document using AI.
    /// </summary>
    /// <param name="guidelineSetId">Target guideline set</param>
    /// <param name="fileName">Document filename</param>
    /// <param name="content">Document content stream</param>
    /// <returns>List of extracted criteria (not yet saved)</returns>
    Task<IReadOnlyList<Criterion>> ExtractCriteriaAsync(string guidelineSetId, string fileName, Stream content);

    /// <summary>
    /// Confirm and save extracted criteria to guideline set.
    /// </summary>
    Task<GuidelineSet> ConfirmCriteriaAsync(string guidelineSetId, IEnumerable<Criterion> criteria);

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
```

## Behaviors

### GetDefaultAsync

- Returns the guideline set with `IsDefault = true`
- If no default set, returns the first active set
- If no active sets, returns null

### CreateAsync

- Generates new ID
- Sets `IsActive = true`, `IsDefault = false`
- Initializes empty criteria list
- If first guideline set, sets as default

### UpdateAsync (IsDefault = true)

- Unsets `IsDefault` on all other guideline sets
- Only one default allowed

### ExtractCriteriaAsync

1. Extract text from uploaded guideline document (PDF/DOCX)
2. Build AI prompt to identify review criteria
3. Parse AI response into `Criterion` objects
4. Return extracted criteria WITHOUT saving
5. Admin reviews and confirms before save

### AI Prompt for Criteria Extraction

```
System:
You are analyzing a document that contains guidelines for reviewing audit reports.
Extract distinct review criteria that can be used to evaluate audit documents.

For each criterion, provide:
- category: One of [Clarity, Completeness, FactualSupport]
- name: Short name (max 50 chars)
- description: Full description of what to check
- weight: Importance 1-10 (10 = critical)
- examples: List of example violations or good practices

User:
{extracted guideline document text}

Respond in JSON array format.
```

### ConfirmCriteriaAsync

- Replaces or merges criteria in guideline set
- Updates `GuidelineSet.UpdatedAt`
- Stores reference to source document

## Error Cases

| Scenario | Exception |
|----------|-----------|
| Guideline set not found | `NotFoundException` |
| Duplicate set name | `ValidationException` |
| Delete default set without replacement | `InvalidOperationException` |
| Criterion not found | `NotFoundException` |
| Document extraction failure | `DocumentProcessingException` |
| AI extraction failure | `ExternalServiceException` |

## Mock Implementation Notes

- Pre-populate with "Supreme Audit Office Guidelines 2025" default set
- Include 5-8 realistic criteria across all categories
- `ExtractCriteriaAsync` returns hardcoded sample criteria after delay
- Store in JSON file: `data/guidelines.json`
