namespace DocCoach.Web.Models;

/// <summary>
/// A reference document demonstrating compliance.
/// </summary>
public class ExampleDocument
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>Parent guideline set.</summary>
    public string GuidelineSetId { get; set; } = string.Empty;
    
    /// <summary>Original filename.</summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>Path/URI to stored file.</summary>
    public string StoragePath { get; set; } = string.Empty;
    
    /// <summary>When uploaded.</summary>
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>What this example demonstrates.</summary>
    public string? Description { get; set; }
    
    /// <summary>Cached extracted text.</summary>
    public string? ExtractedText { get; set; }
}
