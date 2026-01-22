using DocCoach.Web.Models;
using DocCoach.Web.Services.Analyzers;

namespace DocCoach.Web.Services.Interfaces;

/// <summary>
/// Comparison result between two reviews.
/// </summary>
public record ReviewComparison(
    Review Review1,
    Review Review2,
    int ScoreDelta,
    Dictionary<ReviewDimension, int> DimensionDeltas,
    int NewIssuesCount,
    int ResolvedIssuesCount
);

/// <summary>
/// Performs AI-powered document analysis and manages review results.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Analyze a document against guidelines and generate review feedback.
    /// </summary>
    /// <param name="documentId">Document to review (must have extracted text)</param>
    /// <param name="guidelineSetId">Guidelines to apply</param>
    /// <param name="onProgress">Optional callback for progress updates</param>
    /// <returns>Completed review with score and feedback items</returns>
    Task<Review> AnalyzeAsync(string documentId, string guidelineSetId, Action<AnalysisProgress>? onProgress = null);

    /// <summary>
    /// Get review by ID.
    /// </summary>
    Task<Review?> GetByIdAsync(string id);

    /// <summary>
    /// Get the most recent review for a specific document.
    /// </summary>
    Task<Review?> GetByDocumentIdAsync(string documentId);

    /// <summary>
    /// Get all reviews for a specific document (for comparison/history).
    /// </summary>
    Task<IReadOnlyList<Review>> GetAllByDocumentIdAsync(string documentId);

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
