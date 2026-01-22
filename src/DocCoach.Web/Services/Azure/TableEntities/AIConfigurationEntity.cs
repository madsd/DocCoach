using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using DocCoach.Web.Models;

namespace DocCoach.Web.Services.Azure.TableEntities;

/// <summary>
/// Azure Table Storage entity for AIConfiguration.
/// PartitionKey = "aiconfig", RowKey = configuration ID
/// </summary>
public class AIConfigurationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "aiconfig";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o";
    public string AvailableModelsJson { get; set; } = "[\"gpt-4o\",\"gpt-4o-mini\"]";
    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 4000;
    public string SummarySystemPrompt { get; set; } = string.Empty;
    public double SummaryTemperature { get; set; } = 0.3;
    public int SummaryMaxTokens { get; set; } = 400;
    public int MaxDocumentLength { get; set; } = 8000;
    public int MaxSummaryDocumentLength { get; set; } = 6000;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsActive { get; set; }

    public static AIConfigurationEntity FromModel(AIConfiguration config)
    {
        return new AIConfigurationEntity
        {
            RowKey = config.Id,
            Name = config.Name,
            Description = config.Description,
            SystemPrompt = config.SystemPrompt,
            Model = config.Model,
            AvailableModelsJson = JsonSerializer.Serialize(config.AvailableModels),
            Temperature = config.Temperature,
            MaxTokens = config.MaxTokens,
            SummarySystemPrompt = config.SummarySystemPrompt,
            SummaryTemperature = config.SummaryTemperature,
            SummaryMaxTokens = config.SummaryMaxTokens,
            MaxDocumentLength = config.MaxDocumentLength,
            MaxSummaryDocumentLength = config.MaxSummaryDocumentLength,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            IsActive = config.IsActive
        };
    }

    public AIConfiguration ToModel()
    {
        return new AIConfiguration
        {
            Id = RowKey,
            Name = Name,
            Description = Description,
            SystemPrompt = SystemPrompt,
            Model = Model,
            AvailableModels = DeserializeAvailableModels(AvailableModelsJson),
            Temperature = (float)Temperature,
            MaxTokens = MaxTokens,
            SummarySystemPrompt = SummarySystemPrompt,
            SummaryTemperature = (float)SummaryTemperature,
            SummaryMaxTokens = SummaryMaxTokens,
            MaxDocumentLength = MaxDocumentLength,
            MaxSummaryDocumentLength = MaxSummaryDocumentLength,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            IsActive = IsActive
        };
    }

    private static List<string> DeserializeAvailableModels(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string> { "gpt-4o", "gpt-4o-mini" };
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string> { "gpt-4o", "gpt-4o-mini" };
        }
        catch
        {
            return new List<string> { "gpt-4o", "gpt-4o-mini" };
        }
    }
}
