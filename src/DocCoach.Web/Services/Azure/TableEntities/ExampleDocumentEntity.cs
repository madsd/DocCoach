using Azure;
using Azure.Data.Tables;
using DocCoach.Web.Models;

namespace DocCoach.Web.Services.Azure.TableEntities;

/// <summary>
/// Azure Table Storage entity for ExampleDocument.
/// PartitionKey = guideline set ID, RowKey = example ID
/// </summary>
public class ExampleDocumentEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // GuidelineSetId
    public string RowKey { get; set; } = string.Empty; // ExampleId
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string GuidelineSetId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public string? Description { get; set; }
    public string? ExtractedText { get; set; }

    public static ExampleDocumentEntity FromModel(ExampleDocument example)
    {
        return new ExampleDocumentEntity
        {
            PartitionKey = example.GuidelineSetId,
            RowKey = example.Id,
            GuidelineSetId = example.GuidelineSetId,
            FileName = example.FileName,
            StoragePath = example.StoragePath,
            UploadedAt = example.UploadedAt,
            Description = example.Description,
            ExtractedText = example.ExtractedText
        };
    }

    public ExampleDocument ToModel()
    {
        return new ExampleDocument
        {
            Id = RowKey,
            GuidelineSetId = GuidelineSetId,
            FileName = FileName,
            StoragePath = StoragePath,
            UploadedAt = UploadedAt,
            Description = Description,
            ExtractedText = ExtractedText
        };
    }
}
