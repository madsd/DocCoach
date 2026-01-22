namespace DocCoach.Web.Models;

/// <summary>
/// Severity level for feedback items.
/// </summary>
public enum FeedbackSeverity
{
    /// <summary>Minor suggestion, low impact.</summary>
    Info,
    
    /// <summary>Should be addressed, moderate impact.</summary>
    Warning,
    
    /// <summary>Must be fixed, high impact.</summary>
    Error
}
