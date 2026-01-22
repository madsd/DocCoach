using Azure;
using Azure.Data.Tables;
using DocCoach.Web.Exceptions;
using DocCoach.Web.Models;
using DocCoach.Web.Services.Azure.TableEntities;
using DocCoach.Web.Services.Interfaces;

namespace DocCoach.Web.Services.Azure;

/// <summary>
/// Azure Table Storage implementation of IAIConfigurationService.
/// </summary>
public class AzureAIConfigurationService : IAIConfigurationService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureAIConfigurationService> _logger;
    private const string TableName = "aiconfigurations";

    public AzureAIConfigurationService(
        TableServiceClient tableServiceClient,
        ILogger<AzureAIConfigurationService> logger)
    {
        _tableClient = tableServiceClient.GetTableClient(TableName);
        _logger = logger;
        
        // Ensure table exists
        _tableClient.CreateIfNotExists();
    }

    public async Task<AIConfiguration> GetActiveConfigurationAsync()
    {
        try
        {
            // First try to find an active configuration
            await foreach (var entity in _tableClient.QueryAsync<AIConfigurationEntity>(e => e.IsActive))
            {
                _logger.LogDebug("Found active AI configuration: {Id}", entity.RowKey);
                return entity.ToModel();
            }

            // If no active configuration exists, try to get the default one
            try
            {
                var defaultEntity = await _tableClient.GetEntityAsync<AIConfigurationEntity>("aiconfig", "default");
                _logger.LogDebug("Returning default AI configuration");
                return defaultEntity.Value.ToModel();
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Default doesn't exist, create it
                _logger.LogInformation("No AI configuration found, creating default configuration");
                return await ResetToDefaultAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving AI configuration, returning in-memory default");
            return new AIConfiguration();
        }
    }

    public async Task<AIConfiguration?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<AIConfigurationEntity>("aiconfig", id);
            return response.Value.ToModel();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<AIConfiguration>> GetAllAsync()
    {
        var configurations = new List<AIConfiguration>();
        
        await foreach (var entity in _tableClient.QueryAsync<AIConfigurationEntity>())
        {
            configurations.Add(entity.ToModel());
        }
        
        return configurations.OrderByDescending(c => c.IsActive).ThenBy(c => c.Name).ToList();
    }

    public async Task<AIConfiguration> SaveAsync(AIConfiguration configuration)
    {
        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        
        var entity = AIConfigurationEntity.FromModel(configuration);
        await _tableClient.UpsertEntityAsync(entity);
        
        _logger.LogInformation("Saved AI configuration: {Id} - {Name}", configuration.Id, configuration.Name);
        
        return configuration;
    }

    public async Task SetActiveAsync(string id)
    {
        var targetConfig = await GetByIdAsync(id)
            ?? throw new NotFoundException("AIConfiguration", id);

        // Deactivate all other configurations
        await foreach (var entity in _tableClient.QueryAsync<AIConfigurationEntity>(e => e.IsActive))
        {
            if (entity.RowKey != id)
            {
                entity.IsActive = false;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
                await _tableClient.UpsertEntityAsync(entity);
            }
        }

        // Activate the target configuration
        targetConfig.IsActive = true;
        targetConfig.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveAsync(targetConfig);
        
        _logger.LogInformation("Set AI configuration as active: {Id}", id);
    }

    public async Task DeleteAsync(string id)
    {
        if (id == "default")
        {
            throw new InvalidOperationException("Cannot delete the default AI configuration");
        }

        var config = await GetByIdAsync(id)
            ?? throw new NotFoundException("AIConfiguration", id);

        if (config.IsActive)
        {
            throw new InvalidOperationException("Cannot delete the active AI configuration. Please activate a different configuration first.");
        }

        await _tableClient.DeleteEntityAsync("aiconfig", id);
        _logger.LogInformation("Deleted AI configuration: {Id}", id);
    }

    public async Task<AIConfiguration> ResetToDefaultAsync()
    {
        var defaultConfig = new AIConfiguration
        {
            Id = "default",
            Name = "Default Configuration",
            Description = "Default AI configuration for document review analysis",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        await SaveAsync(defaultConfig);
        _logger.LogInformation("Reset AI configuration to defaults");
        
        return defaultConfig;
    }
}
