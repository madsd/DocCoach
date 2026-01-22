namespace DocCoach.Web.Models;

/// <summary>
/// Processing status for uploaded documents.
/// </summary>
public enum DocumentStatus
{
    /// <summary>File received, not yet processed.</summary>
    Uploaded,
    
    /// <summary>Text extraction in progress.</summary>
    Extracting,
    
    /// <summary>Document is being processed.</summary>
    Processing,
    
    /// <summary>AI analysis in progress.</summary>
    Reviewing,
    
    /// <summary>Review finished successfully.</summary>
    Completed,
    
    /// <summary>Document has been reviewed (alias for Completed).</summary>
    Reviewed,
    
    /// <summary>Processing error occurred.</summary>
    Failed,
    
    /// <summary>Processing error (alias for Failed).</summary>
    Error
}
