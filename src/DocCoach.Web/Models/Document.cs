namespace DocCoach.Web.Models;

/// <summary>
/// Location reference within a document for feedback items.
/// Supports both logical references (page, section) and precise character offsets for text highlighting.
/// </summary>
public class DocumentLocation
{
    /// <summary>Page number (1-based, null if unknown).</summary>
    public int? Page { get; set; }
    
    /// <summary>Section identifier (e.g., "3.2").</summary>
    public string? Section { get; set; }
    
    /// <summary>Line number (1-based, null if unknown).</summary>
    public int? LineNumber { get; set; }
    
    /// <summary>Brief text excerpt for context.</summary>
    public string? Excerpt { get; set; }
    
    /// <summary>
    /// Character offset where the relevant text starts (0-based).
    /// Used for precise text highlighting in the document viewer.
    /// </summary>
    public int? StartOffset { get; set; }
    
    /// <summary>
    /// Character offset where the relevant text ends (0-based, exclusive).
    /// Used for precise text highlighting in the document viewer.
    /// </summary>
    public int? EndOffset { get; set; }
    
    /// <summary>
    /// Returns true if this location has precise offset information for highlighting.
    /// </summary>
    public bool HasPreciseLocation => StartOffset.HasValue && EndOffset.HasValue && EndOffset > StartOffset;
}

/// <summary>
/// An uploaded audit report awaiting or having completed review.
/// </summary>
public class Document
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>Original uploaded filename.</summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>Display name for the document review (defaults to FileName if not specified).</summary>
    public string? DisplayName { get; set; }
    
    /// <summary>Gets the effective name to display (DisplayName if set, otherwise FileName).</summary>
    public string Name => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : FileName;
    
    /// <summary>Size in bytes.</summary>
    public long FileSize { get; set; }
    
    /// <summary>Size in bytes (alias for FileSize).</summary>
    public long FileSizeBytes => FileSize;
    
    /// <summary>MIME type (application/pdf or docx).</summary>
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>Path/URI to stored file (blob or local).</summary>
    public string StoragePath { get; set; } = string.Empty;
    
    /// <summary>When document was uploaded.</summary>
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>Role that uploaded (for demo: "auditor").</summary>
    public string UploadedBy { get; set; } = "auditor";
    
    /// <summary>Selected guideline set for review.</summary>
    public string GuidelineSetId { get; set; } = string.Empty;
    
    /// <summary>Processing status.</summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    
    /// <summary>Cached extracted text content (nullable until processed).</summary>
    public string? ExtractedText { get; set; }
    
    /// <summary>
    /// Validates the document meets requirements.
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(FileName))
        {
            errorMessage = "Filename is required";
            return false;
        }
        
        if (FileName.Length > 255)
        {
            errorMessage = "Filename must be 255 characters or less";
            return false;
        }
        
        const long maxSize = 50 * 1024 * 1024; // 50MB
        if (FileSize > maxSize)
        {
            errorMessage = "File size must be 50MB or less";
            return false;
        }
        
        var validTypes = new[] 
        { 
            "application/pdf", 
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" 
        };
        
        if (!validTypes.Contains(ContentType, StringComparer.OrdinalIgnoreCase))
        {
            errorMessage = "File must be PDF or DOCX format";
            return false;
        }
        
        errorMessage = null;
        return true;
    }
}
