using System.ComponentModel.DataAnnotations;

namespace DocCoach.Web.Models.ViewModels;

/// <summary>
/// View model for creating/editing a guideline set.
/// </summary>
public class GuidelineSetViewModel
{
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string Name { get; set; } = "";
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    public bool IsDefault { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Create a view model from an existing guideline set.
    /// </summary>
    public static GuidelineSetViewModel FromModel(GuidelineSet model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Description = model.Description,
        IsDefault = model.IsDefault,
        IsActive = model.IsActive
    };
    
    /// <summary>
    /// Apply changes to an existing guideline set model.
    /// </summary>
    public void ApplyTo(GuidelineSet model)
    {
        model.Name = Name;
        model.Description = Description;
        model.IsDefault = IsDefault;
        model.IsActive = IsActive;
        model.UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Create a new guideline set model from this view model.
    /// </summary>
    public GuidelineSet ToModel() => new()
    {
        Name = Name,
        Description = Description,
        IsDefault = IsDefault,
        IsActive = IsActive
    };
}
