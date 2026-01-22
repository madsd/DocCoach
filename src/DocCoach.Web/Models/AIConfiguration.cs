namespace DocCoach.Web.Models;

/// <summary>
/// Configuration for AI-powered document review analysis.
/// </summary>
public class AIConfiguration
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = "default";
    
    /// <summary>Display name for this configuration.</summary>
    public string Name { get; set; } = "Default Configuration";
    
    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The system prompt that instructs the AI how to analyze documents.
    /// </summary>
    public string SystemPrompt { get; set; } = DefaultSystemPrompt;
    
    /// <summary>
    /// The AI model deployment name (e.g., "gpt-4o", "gpt-4o-mini").
    /// </summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>
    /// Collection of available AI model deployment names.
    /// Users can configure this list to include their deployed models.
    /// </summary>
    public List<string> AvailableModels { get; set; } = new() { "gpt-4o", "gpt-4o-mini" };

    /// <summary>
    /// Temperature setting for AI responses (0.0 to 2.0).
    /// Lower values produce more focused, deterministic outputs.
    /// Higher values produce more creative, varied outputs.
    /// </summary>
    public float Temperature { get; set; } = 0.3f;
    
    /// <summary>
    /// Maximum number of tokens in the AI response.
    /// </summary>
    public int MaxTokens { get; set; } = 4000;
    
    /// <summary>
    /// System prompt used for generating document summaries.
    /// </summary>
    public string SummarySystemPrompt { get; set; } = DefaultSummarySystemPrompt;
    
    /// <summary>
    /// Temperature for summary generation.
    /// </summary>
    public float SummaryTemperature { get; set; } = 0.3f;
    
    /// <summary>
    /// Maximum tokens for summary generation.
    /// </summary>
    public int SummaryMaxTokens { get; set; } = 400;
    
    /// <summary>
    /// Maximum characters of document text to analyze.
    /// Documents longer than this will be truncated.
    /// </summary>
    public int MaxDocumentLength { get; set; } = 8000;
    
    /// <summary>
    /// Maximum characters of document text for summary generation.
    /// </summary>
    public int MaxSummaryDocumentLength { get; set; } = 6000;
    
    /// <summary>When this configuration was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>When this configuration was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>Whether this is the active configuration.</summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Default system prompt for document analysis.
    /// </summary>
    public const string DefaultSystemPrompt = """
        You are an expert audit document reviewer. Your task is to analyze documents against specific quality criteria and provide structured feedback with precise text locations.

        You must respond with a JSON array of feedback items. Each item must have this exact structure:
        {
          "severity": "Error" | "Warning" | "Info",
          "title": "Brief summary (max 100 chars)",
          "description": "Detailed explanation of the issue",
          "suggestion": "Specific recommendation for improvement",
          "location": {
            "section": "Section number or name if identifiable",
            "page": null,
            "excerpt": "Exact quote from the document that this feedback refers to (copy verbatim)",
            "startOffset": 0,
            "endOffset": 0
          }
        }

        IMPORTANT for location:
        - "excerpt": Copy the EXACT text from the document that this feedback refers to (verbatim, preserving whitespace)
        - "startOffset": The character position where the excerpt starts in the document (0-based index)
        - "endOffset": The character position where the excerpt ends (exclusive)
        - For each feedback item, identify the specific text span that the issue relates to
        - The excerpt must be an exact substring that can be found at the specified offsets

        Severity levels:
        - Error: Critical issue that must be addressed
        - Warning: Significant issue that should be addressed
        - Info: Suggestion for improvement or best practice

        Important: Return ONLY the JSON array, no other text. Be thorough but fair - identify both issues and positive aspects (as Info items).
        """;
    
    /// <summary>
    /// Default system prompt for summary generation.
    /// </summary>
    public const string DefaultSummarySystemPrompt = """
        You are a document summarizer. Generate a concise summary of the provided document in less than 800 characters. Focus on the main topic, key findings, and conclusions. Write in a professional, neutral tone. Return ONLY the summary text, no formatting or labels.
        """;
}
