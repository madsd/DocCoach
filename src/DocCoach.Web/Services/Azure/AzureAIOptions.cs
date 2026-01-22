namespace DocCoach.Web.Services.Azure;

/// <summary>
/// Configuration for Azure AI Foundry integration.
/// </summary>
public class AzureAIOptions
{
    public const string SectionName = "AzureAI";
    
    /// <summary>
    /// The Azure AI Foundry project endpoint URL.
    /// Example: https://<service name>.services.ai.azure.com/api/projects/proj-default
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// The model deployment name (e.g., "gpt-4o").
    /// This is the default model, but can be overridden by AIConfiguration.
    /// </summary>
    public string ModelDeployment { get; set; } = "gpt-4o";
    
    /// <summary>
    /// Optional API key. If not provided, uses DefaultAzureCredential (managed identity).
    /// </summary>
    public string? ApiKey { get; set; }
}
