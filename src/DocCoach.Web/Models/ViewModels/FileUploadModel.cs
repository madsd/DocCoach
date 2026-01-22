using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace DocCoach.Web.Models.ViewModels;

/// <summary>
/// View model for document upload form.
/// </summary>
public class FileUploadModel
{
    /// <summary>
    /// The file being uploaded.
    /// </summary>
    public IBrowserFile? File { get; set; }
    
    /// <summary>
    /// The selected guideline set ID for reviewing the document.
    /// </summary>
    [Required(ErrorMessage = "Please select a guideline set")]
    public string? GuidelineSetId { get; set; }
    
    /// <summary>
    /// Optional notes about the document.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Maximum allowed file size in bytes (50 MB).
    /// </summary>
    public const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB
    
    /// <summary>
    /// Allowed file extensions.
    /// </summary>
    public static readonly string[] AllowedExtensions = [".pdf", ".docx"];
    
    /// <summary>
    /// Validates that the file is present and meets requirements.
    /// </summary>
    public (bool IsValid, string? ErrorMessage) ValidateFile()
    {
        if (File == null)
        {
            return (false, "Please select a file to upload");
        }
        
        var extension = Path.GetExtension(File.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return (false, $"File type '{extension}' is not supported. Please upload a PDF or DOCX file.");
        }
        
        if (File.Size > MaxFileSizeBytes)
        {
            var sizeMb = File.Size / (1024.0 * 1024.0);
            return (false, $"File size ({sizeMb:F1} MB) exceeds the maximum allowed size of 50 MB.");
        }
        
        return (true, null);
    }
    
    /// <summary>
    /// Gets a human-readable file size string.
    /// </summary>
    public string GetFileSizeDisplay()
    {
        if (File == null) return "No file selected";
        
        var sizeBytes = File.Size;
        return sizeBytes switch
        {
            < 1024 => $"{sizeBytes} B",
            < 1024 * 1024 => $"{sizeBytes / 1024.0:F1} KB",
            _ => $"{sizeBytes / (1024.0 * 1024.0):F1} MB"
        };
    }
}
