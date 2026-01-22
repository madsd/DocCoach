using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using DocCoach.Web.Models;

namespace DocCoach.Web.Services.Azure.TableEntities;

/// <summary>
/// Azure Table Storage entity for Review results.
/// PartitionKey = document ID, RowKey = review ID
/// Feedback items are stored in blob storage to avoid 64KB property limit.
/// </summary>
public class ReviewEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // DocumentId
    public string RowKey { get; set; } = string.Empty; // ReviewId
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string DocumentId { get; set; } = string.Empty;
    public string GuidelineSetId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int OverallScore { get; set; }
    public string DimensionScoresJson { get; set; } = "{}";
    
    /// <summary>
    /// Path to feedback items blob in storage (reviews/{reviewId}/feedback.json)
    /// </summary>
    public string FeedbackItemsBlobPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of feedback items (stored for quick access without loading blob)
    /// </summary>
    public int FeedbackItemCount { get; set; }
    
    public long ProcessingTimeMs { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int ExtractedTextLength { get; set; }

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates entity from model. Note: feedback items must be stored separately in blob storage.
    /// The feedbackBlobPath should be provided after storing the feedback.
    /// </summary>
    public static ReviewEntity FromModel(Review review, string? feedbackBlobPath = null)
    {
        return new ReviewEntity
        {
            PartitionKey = review.DocumentId,
            RowKey = review.Id,
            DocumentId = review.DocumentId,
            GuidelineSetId = review.GuidelineSetId,
            CreatedAt = review.CreatedAt,
            OverallScore = review.OverallScore,
            DimensionScoresJson = JsonSerializer.Serialize(review.DimensionScores, JsonOptions),
            FeedbackItemsBlobPath = feedbackBlobPath ?? string.Empty,
            FeedbackItemCount = review.FeedbackItems.Count,
            ProcessingTimeMs = review.ProcessingTimeMs,
            ModelUsed = review.ModelUsed,
            Summary = review.Summary,
            ExtractedTextLength = review.ExtractedTextLength
        };
    }

    /// <summary>
    /// Converts to model without feedback items. Call LoadFeedbackItemsAsync to populate feedback.
    /// </summary>
    public Review ToModel()
    {
        return new Review
        {
            Id = RowKey,
            DocumentId = DocumentId,
            GuidelineSetId = GuidelineSetId,
            CreatedAt = CreatedAt,
            OverallScore = OverallScore,
            DimensionScores = DeserializeDimensionScores(),
            FeedbackItems = new List<FeedbackItem>(), // Loaded separately from blob
            ProcessingTimeMs = ProcessingTimeMs,
            ModelUsed = ModelUsed,
            Summary = Summary,
            ExtractedTextLength = ExtractedTextLength
        };
    }

    private Dictionary<ReviewDimension, int> DeserializeDimensionScores()
    {
        if (string.IsNullOrEmpty(DimensionScoresJson) || DimensionScoresJson == "{}")
            return new Dictionary<ReviewDimension, int>();

        try
        {
            // DimensionScores are stored with string keys, need to parse back to enum
            var stringDict = JsonSerializer.Deserialize<Dictionary<string, int>>(DimensionScoresJson, JsonOptions);
            if (stringDict == null) return new Dictionary<ReviewDimension, int>();

            var result = new Dictionary<ReviewDimension, int>();
            foreach (var kvp in stringDict)
            {
                if (Enum.TryParse<ReviewDimension>(kvp.Key, out var dimension))
                {
                    result[dimension] = kvp.Value;
                }
            }
            return result;
        }
        catch
        {
            return new Dictionary<ReviewDimension, int>();
        }
    }

    /// <summary>
    /// Serializes feedback items to JSON for blob storage.
    /// </summary>
    public static string SerializeFeedbackItems(List<FeedbackItem> items)
    {
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    /// <summary>
    /// Deserializes feedback items from blob storage JSON.
    /// </summary>
    public static List<FeedbackItem> DeserializeFeedbackItems(string json)
    {
        return JsonSerializer.Deserialize<List<FeedbackItem>>(json, JsonOptions) ?? new();
    }
}
