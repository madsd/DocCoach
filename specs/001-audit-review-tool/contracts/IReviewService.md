# Contract: IReviewService

**Feature**: 001-audit-review-tool  
**Date**: 2026-01-05

## Purpose

Performs AI-powered document analysis and manages review results.

## Interface Definition

```csharp
public interface IReviewService
{
    /// <summary>
    /// Analyze a document against guidelines and generate review feedback.
    /// </summary>
    /// <param name="documentId">Document to review (must have extracted text)</param>
    /// <param name="guidelineSetId">Guidelines to apply</param>
    /// <param name="onProgress">Optional callback for progress updates (0-100)</param>
    /// <returns>Completed review with score and feedback items</returns>
    Task<Review> AnalyzeAsync(string documentId, string guidelineSetId, Action<int>? onProgress = null);

    /// <summary>
    /// Get review by ID.
    /// </summary>
    Task<Review?> GetByIdAsync(string id);

    /// <summary>
    /// Get review for a specific document.
    /// </summary>
    Task<Review?> GetByDocumentIdAsync(string documentId);

    /// <summary>
    /// Get all reviews (for history view).
    /// </summary>
    /// <param name="limit">Max results to return</param>
    Task<IReadOnlyList<Review>> GetAllAsync(int limit = 50);

    /// <summary>
    /// Delete a review.
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Compare two reviews (for score comparison feature).
    /// </summary>
    Task<ReviewComparison> CompareAsync(string reviewId1, string reviewId2);
}

public record ReviewComparison(
    Review Review1,
    Review Review2,
    int ScoreDelta,
    Dictionary<string, int> CategoryDeltas,
    int NewIssuesCount,
    int ResolvedIssuesCount
);
```

## Behaviors

### AnalyzeAsync

1. Retrieve document (must have `Status = Completed` for extraction)
2. Retrieve guideline set with criteria
3. Update document status to `Reviewing`
4. Build AI prompt with:
   - System: Guidelines criteria as review instructions
   - User: Document extracted text
   - Format: Request structured JSON response
5. Call Azure OpenAI (or mock)
6. Parse response into `Review` with `FeedbackItems`
7. Calculate scores:
   - Category scores based on feedback severity/count
   - Overall score as weighted average
8. Persist review
9. Update document status to `Completed`

### Progress Callback

The `onProgress` callback enables UI progress indicators:
- 0-10%: Preparing request
- 10-80%: AI processing (may update during streaming)
- 80-95%: Parsing response
- 95-100%: Saving results

### Score Calculation

```
CategoryScore = 100 - (Errors × 15) - (Warnings × 8) - (Infos × 2)
OverallScore = Σ(CategoryScore × CriterionWeight) / Σ(Weights)
```

Minimum score: 0 (capped, no negative scores)

### Error Cases

| Scenario | Exception | Document Status |
|----------|-----------|-----------------|
| Document not found | `NotFoundException` | N/A |
| Document not extracted | `InvalidOperationException` | No change |
| Guideline set not found | `NotFoundException` | No change |
| AI service unavailable | `ExternalServiceException` | `Failed` |
| AI response parse error | `ReviewProcessingException` | `Failed` |

## AI Prompt Structure

### System Prompt Template

```
You are an expert document reviewer for public sector audit reports. 
Review the following document against these criteria:

{foreach criterion in guidelines.criteria}
- [{criterion.Category}] {criterion.Name}: {criterion.Description}
{/foreach}

For each issue found, provide:
- category: One of [Clarity, Completeness, FactualSupport]
- severity: One of [Info, Warning, Error]
- title: Brief summary (max 50 chars)
- description: Detailed explanation
- suggestion: How to fix (optional)
- location: {page, section, excerpt} if identifiable

Respond in JSON format:
{
  "feedbackItems": [...],
  "summary": "Brief overall assessment"
}
```

### User Prompt

```
Document to review:

{document.ExtractedText}
```

## Mock Implementation Notes

- Return pre-defined review results based on document filename patterns
- Simulate 2-3 second processing delay with progress updates
- Vary scores and feedback count for different sample documents
- Include realistic Czech audit report terminology in feedback
