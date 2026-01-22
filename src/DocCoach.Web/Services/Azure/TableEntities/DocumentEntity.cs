using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using DocCoach.Web.Models;

namespace DocCoach.Web.Services.Azure.TableEntities;

/// <summary>
/// Azure Table Storage entity for Document metadata.
/// PartitionKey = "document", RowKey = document ID
/// </summary>
public class DocumentEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "document";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string GuidelineSetId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Path to the blob containing extracted text (stored separately due to size).
    /// </summary>
    public string? ExtractedTextBlobPath { get; set; }

    public static DocumentEntity FromModel(Document document, string? extractedTextBlobPath = null)
    {
        return new DocumentEntity
        {
            RowKey = document.Id,
            FileName = document.FileName,
            DisplayName = document.DisplayName,
            FileSize = document.FileSize,
            ContentType = document.ContentType,
            StoragePath = document.StoragePath,
            UploadedAt = document.UploadedAt,
            UploadedBy = document.UploadedBy,
            GuidelineSetId = document.GuidelineSetId,
            Status = document.Status.ToString(),
            ExtractedTextBlobPath = extractedTextBlobPath
        };
    }

    public Document ToModel()
    {
        return new Document
        {
            Id = RowKey,
            FileName = FileName,
            DisplayName = DisplayName,
            FileSize = FileSize,
            ContentType = ContentType,
            StoragePath = StoragePath,
            UploadedAt = UploadedAt,
            UploadedBy = UploadedBy,
            GuidelineSetId = GuidelineSetId,
            Status = Enum.TryParse<DocumentStatus>(Status, out var status) ? status : DocumentStatus.Uploaded,
            // ExtractedText is stored in blob storage and loaded on demand
            ExtractedText = null
        };
    }

    /// <summary>
    /// Gets the blob path where extracted text is stored.
    /// </summary>
    public string? GetExtractedTextBlobPath() => ExtractedTextBlobPath;
}
