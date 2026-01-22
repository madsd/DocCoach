using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using DocCoach.Web.Models;

namespace DocCoach.Web.Services.Azure.TableEntities;

/// <summary>
/// Azure Table Storage entity for GuidelineSet.
/// PartitionKey = "guideline", RowKey = guideline ID
/// </summary>
public class GuidelineSetEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "guideline";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SourceDocumentPath { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public string RulesJson { get; set; } = "[]";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static GuidelineSetEntity FromModel(GuidelineSet guidelineSet)
    {
        return new GuidelineSetEntity
        {
            RowKey = guidelineSet.Id,
            Name = guidelineSet.Name,
            Description = guidelineSet.Description,
            SourceDocumentPath = guidelineSet.SourceDocumentPath,
            CreatedAt = guidelineSet.CreatedAt,
            UpdatedAt = guidelineSet.UpdatedAt,
            IsActive = guidelineSet.IsActive,
            IsDefault = guidelineSet.IsDefault,
            RulesJson = JsonSerializer.Serialize(guidelineSet.Rules, JsonOptions)
        };
    }

    public GuidelineSet ToModel()
    {
        return new GuidelineSet
        {
            Id = RowKey,
            Name = Name,
            Description = Description,
            SourceDocumentPath = SourceDocumentPath,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            IsActive = IsActive,
            IsDefault = IsDefault,
            Rules = JsonSerializer.Deserialize<List<Rule>>(RulesJson, JsonOptions) ?? new()
        };
    }
}
