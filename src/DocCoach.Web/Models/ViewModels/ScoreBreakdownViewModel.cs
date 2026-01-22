using DocCoach.Web.Models;

namespace DocCoach.Web.Models.ViewModels;

/// <summary>
/// View model for the score dashboard with detailed breakdown.
/// </summary>
public class ScoreBreakdownViewModel
{
    /// <summary>Overall quality score (0-100).</summary>
    public int OverallScore { get; set; }
    
    /// <summary>Score label (Excellent, Good, etc.).</summary>
    public string ScoreLabel => GetScoreLabel();
    
    /// <summary>Scores per category.</summary>
    public List<CategoryScoreItem> CategoryBreakdown { get; set; } = new();
    
    /// <summary>Count of issues by severity.</summary>
    public Dictionary<FeedbackSeverity, int> SeverityCounts { get; set; } = new();
    
    /// <summary>Total number of feedback items.</summary>
    public int TotalFeedbackItems { get; set; }
    
    /// <summary>Historical scores for trend chart.</summary>
    public List<ScoreHistoryPoint> ScoreHistory { get; set; } = new();
    
    /// <summary>Document information.</summary>
    public DocumentSummary? Document { get; set; }
    
    /// <summary>Guideline set used for review.</summary>
    public string GuidelineSetName { get; set; } = string.Empty;
    
    /// <summary>When the review was completed.</summary>
    public DateTimeOffset ReviewedAt { get; set; }
    
    /// <summary>Processing time in milliseconds.</summary>
    public long ProcessingTimeMs { get; set; }
    
    private string GetScoreLabel()
    {
        return OverallScore switch
        {
            >= 90 => "Excellent",
            >= 80 => "Good",
            >= 70 => "Satisfactory",
            >= 60 => "Needs Improvement",
            >= 50 => "Below Standard",
            _ => "Critical Issues"
        };
    }
    
    /// <summary>
    /// Creates a view model from a Review and related data.
    /// </summary>
    public static ScoreBreakdownViewModel FromReview(
        Review review, 
        Document? document = null, 
        GuidelineSet? guidelineSet = null,
        List<Review>? previousReviews = null)
    {
        var vm = new ScoreBreakdownViewModel
        {
            OverallScore = review.OverallScore,
            TotalFeedbackItems = review.FeedbackItems.Count,
            SeverityCounts = review.GetSeverityCounts(),
            ReviewedAt = review.CreatedAt,
            ProcessingTimeMs = review.ProcessingTimeMs,
            GuidelineSetName = guidelineSet?.Name ?? "Unknown"
        };
        
        // Build dimension breakdown
        foreach (var dimension in review.DimensionScores)
        {
            var feedbackCount = review.FeedbackItems.Count(f => 
                f.EffectiveDimension == dimension.Key);
            
            vm.CategoryBreakdown.Add(new CategoryScoreItem
            {
                Category = dimension.Key.GetDisplayName(),
                Score = dimension.Value,
                FeedbackCount = feedbackCount
            });
        }
        
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
        
        // Build score history from previous reviews
        if (previousReviews != null && previousReviews.Count > 0)
        {
            vm.ScoreHistory = previousReviews
                .OrderBy(r => r.CreatedAt)
                .Select(r => new ScoreHistoryPoint
                {
                    ReviewId = r.Id,
                    Score = r.OverallScore,
                    Date = r.CreatedAt,
                    Label = r.CreatedAt.ToString("MMM dd")
                })
                .ToList();
        }
        
        // Add current review to history
        vm.ScoreHistory.Add(new ScoreHistoryPoint
        {
            ReviewId = review.Id,
            Score = review.OverallScore,
            Date = review.CreatedAt,
            Label = "Current",
            IsCurrent = true
        });
        
        return vm;
    }
}

/// <summary>
/// Category score with additional metadata.
/// </summary>
public class CategoryScoreItem
{
    /// <summary>Category name (Clarity, Completeness, FactualSupport).</summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>Score for this category (0-100).</summary>
    public int Score { get; set; }
    
    /// <summary>Number of feedback items in this category.</summary>
    public int FeedbackCount { get; set; }
    
    /// <summary>Display name with proper spacing.</summary>
    public string DisplayName => FormatCategoryName(Category);
    
    /// <summary>Score quality level.</summary>
    public string Level => Score switch
    {
        >= 80 => "Good",
        >= 60 => "Fair",
        _ => "Poor"
    };
    
    private static string FormatCategoryName(string category)
    {
        // Add space before capitals (e.g., FactualSupport -> Factual Support)
        return string.Concat(category.Select((c, i) => 
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }
}

/// <summary>
/// A point in the score history for trend charts.
/// </summary>
public class ScoreHistoryPoint
{
    /// <summary>Review ID for navigation.</summary>
    public string ReviewId { get; set; } = string.Empty;
    
    /// <summary>Score at this point.</summary>
    public int Score { get; set; }
    
    /// <summary>When this review was created.</summary>
    public DateTimeOffset Date { get; set; }
    
    /// <summary>Label for display on chart.</summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>Whether this is the current review.</summary>
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Summary information about a document.
/// </summary>
public class DocumentSummary
{
    /// <summary>Document ID.</summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>File name.</summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>Display name for the document review.</summary>
    public string? DisplayName { get; set; }
    
    /// <summary>Gets the effective name to display.</summary>
    public string Name => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : FileName;
    
    /// <summary>When uploaded.</summary>
    public DateTimeOffset UploadedAt { get; set; }
}
