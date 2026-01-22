using DocCoach.Web.Models;

namespace DocCoach.Web.Services.Interfaces;

/// <summary>
/// Service for managing AI configuration settings.
/// </summary>
public interface IAIConfigurationService
{
    /// <summary>
    /// Gets the currently active AI configuration.
    /// Returns default configuration if none is set.
    /// </summary>
    Task<AIConfiguration> GetActiveConfigurationAsync();
    
    /// <summary>
    /// Gets a specific AI configuration by ID.
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <returns>Configuration or null if not found</returns>
    Task<AIConfiguration?> GetByIdAsync(string id);
    
    /// <summary>
    /// Gets all AI configurations.
    /// </summary>
    Task<IReadOnlyList<AIConfiguration>> GetAllAsync();
    
    /// <summary>
    /// Creates or updates an AI configuration.
    /// </summary>
    /// <param name="configuration">Configuration to save</param>
    Task<AIConfiguration> SaveAsync(AIConfiguration configuration);
    
    /// <summary>
    /// Sets a configuration as the active one.
    /// </summary>
    /// <param name="id">Configuration ID to activate</param>
    Task SetActiveAsync(string id);
    
    /// <summary>
    /// Deletes an AI configuration.
    /// </summary>
    /// <param name="id">Configuration ID to delete</param>
    Task DeleteAsync(string id);
    
    /// <summary>
    /// Resets the configuration to default values.
    /// </summary>
    Task<AIConfiguration> ResetToDefaultAsync();
}
