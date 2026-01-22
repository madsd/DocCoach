namespace DocCoach.Web.Models;

/// <summary>
/// User roles for the application.
/// </summary>
public enum UserRole
{
    /// <summary>No role selected yet.</summary>
    None,
    
    /// <summary>Can upload documents and view reviews.</summary>
    Auditor,
    
    /// <summary>Can configure guidelines and upload examples.</summary>
    Admin
}
