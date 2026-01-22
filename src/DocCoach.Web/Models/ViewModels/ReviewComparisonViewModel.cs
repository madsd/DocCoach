using DocCoach.Web.Models;

namespace DocCoach.Web.Models.ViewModels;

/// <summary>
/// View model for comparing multiple review versions.
/// </summary>
public class ReviewComparisonViewModel
{
    /// <summary>The current/latest review being compared.</summary>
    public ReviewSnapshot CurrentReview { get; set; } = new();
    
    /// <summary>The previous review to compare against.</summary>
    public ReviewSnapshot? PreviousReview { get; set; }
    
    /// <summary>Overall score change.</summary>
    public ScoreDelta OverallDelta => CalculateOverallDelta();
    
    /// <summary>Category-by-category comparison.</summary>
    public List<CategoryComparison> CategoryComparisons { get; set; } = new();
    
    /// <summary>Feedback changes summary.</summary>
    public FeedbackChangeSummary FeedbackChanges { get; set; } = new();
    
    /// <summary>List of available reviews to compare against.</summary>
    public List<ReviewOption> AvailableReviews { get; set; } = new();
    
    /// <summary>Document information.</summary>
    public DocumentSummary? Document { get; set; }
    
    private ScoreDelta CalculateOverallDelta()
    {
        if (PreviousReview == null)
        {
            return new ScoreDelta
            {
                CurrentScore = CurrentReview.OverallScore,
                PreviousScore = 0,
                Change = 0,
                ChangePercent = 0,
                Direction = ChangeDirection.None
            };
        }
        
        var change = CurrentReview.OverallScore - PreviousReview.OverallScore;
        return new ScoreDelta
        {
            CurrentScore = CurrentReview.OverallScore,
            PreviousScore = PreviousReview.OverallScore,
            Change = change,
            ChangePercent = PreviousReview.OverallScore > 0 
                ? Math.Round((double)change / PreviousReview.OverallScore * 100, 1)
                : 0,
            Direction = change > 0 ? ChangeDirection.Improved 
                      : change < 0 ? ChangeDirection.Declined 
                      : ChangeDirection.Unchanged
        };
    }
    
    /// <summary>
    /// Creates a comparison view model from two reviews.
    /// </summary>
    public static ReviewComparisonViewModel FromReviews(
        Review currentReview,
        Review? previousReview,
        Document? document = null,
        List<Review>? allReviews = null)
    {
        var vm = new ReviewComparisonViewModel
        {
            CurrentReview = ReviewSnapshot.FromReview(currentReview),
            PreviousReview = previousReview != null 
                ? ReviewSnapshot.FromReview(previousReview) 
                : null
        };
        
        // Document summary
        if (document != null)
        {
            vm.Document = new DocumentSummary
            {
                Id = document.Id,
                FileName = document.FileName,
                DisplayName = document.DisplayName,
                UploadedAt = document.UploadedAt
            };
        }
        
        // Build dimension comparisons
        foreach (var dimension in currentReview.DimensionScores)
        {
            var previousScore = previousReview?.DimensionScores
                .GetValueOrDefault(dimension.Key, 0) ?? 0;
            
            var change = dimension.Value - previousScore;
            
            vm.CategoryComparisons.Add(new CategoryComparison
            {
                Category = dimension.Key.GetDisplayName(),
                CurrentScore = dimension.Value,
                PreviousScore = previousScore,
                Change = change,
                Direction = change > 0 ? ChangeDirection.Improved 
                          : change < 0 ? ChangeDirection.Declined 
                          : ChangeDirection.Unchanged
            });
        }
        
        // Calculate feedback changes
        if (previousReview != null)
        {
            vm.FeedbackChanges = new FeedbackChangeSummary
            {
                CurrentTotal = currentReview.FeedbackItems.Count,
                PreviousTotal = previousReview.FeedbackItems.Count,
                CurrentErrors = currentReview.FeedbackItems.Count(f => f.Severity == FeedbackSeverity.Error),
                PreviousErrors = previousReview.FeedbackItems.Count(f => f.Severity == FeedbackSeverity.Error),
                CurrentWarnings = currentReview.FeedbackItems.Count(f => f.Severity == FeedbackSeverity.Warning),
                PreviousWarnings = previousReview.FeedbackItems.Count(f => f.Severity == FeedbackSeverity.Warning)
            };
        }
        else
        {
            vm.FeedbackChanges = new FeedbackChangeSummary
            {
                CurrentTotal = currentReview.FeedbackItems.Count,
                CurrentErrors = currentReview.FeedbackItems.Count(f => f.Severity == FeedbackSeverity.Error),
                CurrentWarnings = currentReview.FeedbackItems.Count(f => f.Severity == FeedbackSeverity.Warning)
            };
        }
        
        // Available reviews for comparison
        if (allReviews != null)
        {
            vm.AvailableReviews = allReviews
                .Where(r => r.Id != currentReview.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewOption
                {
                    ReviewId = r.Id,
                    Label = r.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                    Score = r.OverallScore
                })
                .ToList();
        }
        
        return vm;
    }
}

/// <summary>
/// A snapshot of a review for comparison purposes.
/// </summary>
public class ReviewSnapshot
{
    /// <summary>Review ID.</summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>Overall score.</summary>
    public int OverallScore { get; set; }
    
    /// <summary>Dimension scores.</summary>
    public Dictionary<ReviewDimension, int> DimensionScores { get; set; } = new();
    
    /// <summary>When the review was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>Total feedback count.</summary>
    public int FeedbackCount { get; set; }
    
    /// <summary>
    /// Creates a snapshot from a Review.
    /// </summary>
    public static ReviewSnapshot FromReview(Review review)
    {
        return new ReviewSnapshot
        {
            Id = review.Id,
            OverallScore = review.OverallScore,
            DimensionScores = new Dictionary<ReviewDimension, int>(review.DimensionScores),
            CreatedAt = review.CreatedAt,
            FeedbackCount = review.FeedbackItems.Count
        };
    }
}

/// <summary>
/// Score change information.
/// </summary>
public class ScoreDelta
{
    /// <summary>Current score.</summary>
    public int CurrentScore { get; set; }
    
    /// <summary>Previous score.</summary>
    public int PreviousScore { get; set; }
    
    /// <summary>Absolute change (+/-).</summary>
    public int Change { get; set; }
    
    /// <summary>Percentage change.</summary>
    public double ChangePercent { get; set; }
    
    /// <summary>Direction of change.</summary>
    public ChangeDirection Direction { get; set; }
    
    /// <summary>Formatted change string with +/- prefix.</summary>
    public string FormattedChange => Change > 0 ? $"+{Change}" : Change.ToString();
}

/// <summary>
/// Direction of score change.
/// </summary>
public enum ChangeDirection
{
    /// <summary>No previous data to compare.</summary>
    None,
    
    /// <summary>Score improved.</summary>
    Improved,
    
    /// <summary>Score stayed the same.</summary>
    Unchanged,
    
    /// <summary>Score declined.</summary>
    Declined
}

/// <summary>
/// Category-level comparison.
/// </summary>
public class CategoryComparison
{
    /// <summary>Category name.</summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>Current score.</summary>
    public int CurrentScore { get; set; }
    
    /// <summary>Previous score.</summary>
    public int PreviousScore { get; set; }
    
    /// <summary>Score change.</summary>
    public int Change { get; set; }
    
    /// <summary>Direction of change.</summary>
    public ChangeDirection Direction { get; set; }
    
    /// <summary>Display name with proper spacing.</summary>
    public string DisplayName => string.Concat(Category.Select((c, i) => 
        i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    
    /// <summary>Formatted change string.</summary>
    public string FormattedChange => Change > 0 ? $"+{Change}" : Change.ToString();
}

/// <summary>
/// Summary of feedback item changes between reviews.
/// </summary>
public class FeedbackChangeSummary
{
    /// <summary>Current total feedback count.</summary>
    public int CurrentTotal { get; set; }
    
    /// <summary>Previous total feedback count.</summary>
    public int PreviousTotal { get; set; }
    
    /// <summary>Change in total count.</summary>
    public int TotalChange => CurrentTotal - PreviousTotal;
    
    /// <summary>Current error count.</summary>
    public int CurrentErrors { get; set; }
    
    /// <summary>Previous error count.</summary>
    public int PreviousErrors { get; set; }
    
    /// <summary>Change in error count.</summary>
    public int ErrorChange => CurrentErrors - PreviousErrors;
    
    /// <summary>Current warning count.</summary>
    public int CurrentWarnings { get; set; }
    
    /// <summary>Previous warning count.</summary>
    public int PreviousWarnings { get; set; }
    
    /// <summary>Change in warning count.</summary>
    public int WarningChange => CurrentWarnings - PreviousWarnings;
}

/// <summary>
/// An available review option for comparison selection.
/// </summary>
public class ReviewOption
{
    /// <summary>Review ID.</summary>
    public string ReviewId { get; set; } = string.Empty;
    
    /// <summary>Display label.</summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>Review score.</summary>
    public int Score { get; set; }
}
