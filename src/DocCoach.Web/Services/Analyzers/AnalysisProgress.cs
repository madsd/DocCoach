namespace DocCoach.Web.Services.Analyzers;

/// <summary>
/// Progress information during document analysis.
/// </summary>
public record AnalysisProgress
{
    /// <summary>Current phase of analysis.</summary>
    public required AnalysisPhase Phase { get; init; }
    
    /// <summary>Name of the current analyzer (null if not in analyzer phase).</summary>
    public string? CurrentAnalyzerName { get; init; }
    
    /// <summary>Index of current analyzer (1-based).</summary>
    public int CurrentAnalyzerIndex { get; init; }
    
    /// <summary>Total number of analyzers to run.</summary>
    public int TotalAnalyzers { get; init; }
    
    /// <summary>Overall progress percentage (0-100).</summary>
    public int PercentComplete { get; init; }
    
    /// <summary>Human-readable status message.</summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>Creates progress for uploading phase.</summary>
    public static AnalysisProgress Uploading(int percent) => new()
    {
        Phase = AnalysisPhase.Uploading,
        PercentComplete = percent,
        Message = "Uploading document..."
    };
    
    /// <summary>Creates progress for text extraction phase.</summary>
    public static AnalysisProgress Extracting(int percent) => new()
    {
        Phase = AnalysisPhase.ExtractingText,
        PercentComplete = percent,
        Message = "Extracting text from document..."
    };
    
    /// <summary>Creates progress for analyzer execution phase.</summary>
    public static AnalysisProgress Analyzing(
        string analyzerName, 
        int analyzerIndex, 
        int totalAnalyzers,
        int overallPercent) => new()
    {
        Phase = AnalysisPhase.Analyzing,
        CurrentAnalyzerName = analyzerName,
        CurrentAnalyzerIndex = analyzerIndex,
        TotalAnalyzers = totalAnalyzers,
        PercentComplete = overallPercent,
        Message = $"Running {analyzerName}..."
    };
    
    /// <summary>Creates progress for completion.</summary>
    public static AnalysisProgress Complete() => new()
    {
        Phase = AnalysisPhase.Complete,
        PercentComplete = 100,
        Message = "Analysis complete!"
    };
}

/// <summary>
/// Phases of the document analysis process.
/// </summary>
public enum AnalysisPhase
{
    /// <summary>Document is being uploaded.</summary>
    Uploading,
    
    /// <summary>Text is being extracted from the document.</summary>
    ExtractingText,
    
    /// <summary>Analyzers are running.</summary>
    Analyzing,
    
    /// <summary>Analysis is complete.</summary>
    Complete
}
