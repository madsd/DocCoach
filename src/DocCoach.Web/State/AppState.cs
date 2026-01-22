using DocCoach.Web.Models;

namespace DocCoach.Web.State;

/// <summary>
/// Application state for the current user session.
/// </summary>
public class AppState
{
    private UserRole _currentRole = UserRole.None;
    private string? _selectedGuidelineSetId;
    private bool _isDarkMode;

    /// <summary>
    /// Event raised when any state property changes.
    /// </summary>
    public event Action? OnChange;
    
    /// <summary>
    /// Event raised when the current role changes.
    /// </summary>
    public event Action? OnRoleChanged;
    
    /// <summary>
    /// Event raised when the selected guideline set changes.
    /// </summary>
    public event Action? OnGuidelineSetChanged;
    
    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    public event Action? OnThemeChanged;

    /// <summary>
    /// The currently selected user role.
    /// </summary>
    public UserRole CurrentRole
    {
        get => _currentRole;
        set
        {
            if (_currentRole != value)
            {
                _currentRole = value;
                OnRoleChanged?.Invoke();
                OnChange?.Invoke();
            }
        }
    }

    /// <summary>
    /// The currently selected guideline set ID.
    /// </summary>
    public string? SelectedGuidelineSetId
    {
        get => _selectedGuidelineSetId;
        set
        {
            if (_selectedGuidelineSetId != value)
            {
                _selectedGuidelineSetId = value;
                OnGuidelineSetChanged?.Invoke();
                OnChange?.Invoke();
            }
        }
    }

    /// <summary>
    /// Whether dark mode is enabled.
    /// </summary>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnThemeChanged?.Invoke();
                OnChange?.Invoke();
            }
        }
    }

    /// <summary>
    /// Whether the current role is Auditor.
    /// </summary>
    public bool IsAuditor => CurrentRole == UserRole.Auditor;

    /// <summary>
    /// Whether the current role is Admin.
    /// </summary>
    public bool IsAdmin => CurrentRole == UserRole.Admin;
}

/// <summary>
/// State for an active review workflow.
/// </summary>
public class ReviewSession
{
    /// <summary>
    /// Document being processed.
    /// </summary>
    public string? CurrentDocumentId { get; set; }
    
    /// <summary>
    /// Current processing status.
    /// </summary>
    public DocumentStatus ProcessingStatus { get; set; } = DocumentStatus.Uploaded;
    
    /// <summary>
    /// Progress indicator (0-100).
    /// </summary>
    public int ProgressPercent { get; set; }
    
    /// <summary>
    /// Completed review result.
    /// </summary>
    public Review? CurrentReview { get; set; }
    
    /// <summary>
    /// Whether a document is currently being processed.
    /// </summary>
    public bool IsProcessing => ProcessingStatus == DocumentStatus.Extracting || 
                                ProcessingStatus == DocumentStatus.Reviewing;
    
    /// <summary>
    /// Reset the session state.
    /// </summary>
    public void Reset()
    {
        CurrentDocumentId = null;
        ProcessingStatus = DocumentStatus.Uploaded;
        ProgressPercent = 0;
        CurrentReview = null;
    }
}
