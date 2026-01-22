using Azure.Data.Tables;
using DocCoach.Web.Exceptions;
using DocCoach.Web.Models;
using DocCoach.Web.Services.Azure.TableEntities;
using DocCoach.Web.Services.Interfaces;
using System.Xml.Serialization;

namespace DocCoach.Web.Services.Azure;

/// <summary>
/// Azure Table Storage implementation of IGuidelineService.
/// </summary>
public class AzureGuidelineService : IGuidelineService
{
    private readonly TableClient _guidelineTable;
    private readonly TableClient _exampleTable;
    private readonly IStorageService _storageService;
    private readonly ILogger<AzureGuidelineService> _logger;
    private bool _seeded;

    public AzureGuidelineService(
        TableServiceClient tableServiceClient,
        IStorageService storageService,
        ILogger<AzureGuidelineService> logger)
    {
        _guidelineTable = tableServiceClient.GetTableClient("guidelines");
        _exampleTable = tableServiceClient.GetTableClient("examples");
        _storageService = storageService;
        _logger = logger;

        // Ensure tables exist
        _guidelineTable.CreateIfNotExists();
        _exampleTable.CreateIfNotExists();
    }

    private async Task EnsureSeededAsync()
    {
        if (_seeded) return;

        // Check if any guidelines exist
        var hasData = false;
        await foreach (var _ in _guidelineTable.QueryAsync<GuidelineSetEntity>(e => e.PartitionKey == "guideline", maxPerPage: 1))
        {
            hasData = true;
            break;
        }

        if (!hasData)
        {
            _logger.LogInformation("Seeding default guideline sets");
            await SeedDataAsync();
        }

        _seeded = true;
    }

    private async Task SeedDataAsync()
    {
        // Create default guideline set with dimension-aware rules
        var defaultSet = new GuidelineSet
        {
            Id = "sao-2026",
            Name = "Supreme Audit Office Guidelines 2026",
            Description = "Standard review rules for SAO audit reports",
            IsActive = true,
            IsDefault = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Rules = new List<Rule>
            {
                // Language dimension - static
                new() { 
                    Id = "rule-001", 
                    Dimension = ReviewDimension.Language,
                    RuleType = RuleType.SentenceLength,
                    EvaluationMode = EvaluationMode.Static,
                    Scope = EvaluationScope.Sentence,
                    Name = "Sentence Length", 
                    Description = "Sentences should be concise, ideally under 100 words", 
                    Weight = 6, 
                    IsEnabled = true,
                    RuleConfiguration = "{\"maxWords\": 100, \"warningThreshold\": 80}"
                }
            }
        };

        await _guidelineTable.UpsertEntityAsync(GuidelineSetEntity.FromModel(defaultSet));
    }

    public async Task<IReadOnlyList<GuidelineSet>> GetAllAsync(bool activeOnly = true)
    {
        await EnsureSeededAsync();

        var sets = new List<GuidelineSet>();
        await foreach (var entity in _guidelineTable.QueryAsync<GuidelineSetEntity>(e => e.PartitionKey == "guideline"))
        {
            var set = entity.ToModel();
            if (!activeOnly || set.IsActive)
            {
                sets.Add(set);
            }
        }

        return sets.OrderByDescending(g => g.IsDefault).ThenBy(g => g.Name).ToList();
    }

    public async Task<GuidelineSet?> GetByIdAsync(string id)
    {
        await EnsureSeededAsync();

        try
        {
            var response = await _guidelineTable.GetEntityAsync<GuidelineSetEntity>("guideline", id);
            return response.Value.ToModel();
        }
        catch (global::Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<GuidelineSet?> GetDefaultAsync()
    {
        await EnsureSeededAsync();

        var all = await GetAllAsync(true);
        return all.FirstOrDefault(g => g.IsDefault) ?? all.FirstOrDefault();
    }

    public async Task<GuidelineSet> CreateAsync(string name, string? description = null)
    {
        await EnsureSeededAsync();

        var existingDefault = await GetDefaultAsync();

        var set = new GuidelineSet
        {
            Name = name,
            Description = description,
            IsActive = true,
            IsDefault = existingDefault == null
        };

        await _guidelineTable.UpsertEntityAsync(GuidelineSetEntity.FromModel(set));
        _logger.LogInformation("Created guideline set: {Id}, Name: {Name}", set.Id, name);

        return set;
    }

    public async Task<GuidelineSet> UpdateAsync(string id, string name, string? description, bool isActive, bool isDefault)
    {
        var set = await GetByIdAsync(id)
            ?? throw new NotFoundException("GuidelineSet", id);

        // If setting as default, unset other defaults
        if (isDefault && !set.IsDefault)
        {
            var all = await GetAllAsync(false);
            foreach (var other in all.Where(g => g.IsDefault && g.Id != id))
            {
                other.IsDefault = false;
                await _guidelineTable.UpsertEntityAsync(GuidelineSetEntity.FromModel(other));
            }
        }

        set.Name = name;
        set.Description = description;
        set.IsActive = isActive;
        set.IsDefault = isDefault;
        set.UpdatedAt = DateTimeOffset.UtcNow;

        await _guidelineTable.UpsertEntityAsync(GuidelineSetEntity.FromModel(set));
        _logger.LogInformation("Updated guideline set: {Id}", id);

        return set;
    }

    public async Task DeleteAsync(string id)
    {
        var set = await GetByIdAsync(id)
            ?? throw new NotFoundException("GuidelineSet", id);

        if (set.IsDefault)
        {
            throw new InvalidOperationException("Cannot delete the default guideline set");
        }

        // Delete examples
        var examples = await GetExamplesAsync(id);
        foreach (var example in examples)
        {
            await RemoveExampleAsync(id, example.Id);
        }

        await _guidelineTable.DeleteEntityAsync("guideline", id);
        _logger.LogInformation("Deleted guideline set: {Id}", id);
    }

    public async Task<Rule> AddRuleAsync(string guidelineSetId, Rule rule)
    {
        var set = await GetByIdAsync(guidelineSetId)
            ?? throw new NotFoundException("GuidelineSet", guidelineSetId);

        rule.Id = Guid.NewGuid().ToString();
        set.Rules.Add(rule);
        set.UpdatedAt = DateTimeOffset.UtcNow;

        await _guidelineTable.UpsertEntityAsync(GuidelineSetEntity.FromModel(set));

        return rule;
    }

    public async Task<Rule> UpdateRuleAsync(string guidelineSetId, Rule rule)
    {
        var set = await GetByIdAsync(guidelineSetId)
            ?? throw new NotFoundException("GuidelineSet", guidelineSetId);

        var index = set.Rules.FindIndex(r => r.Id == rule.Id);
        if (index < 0)
        {
            throw new NotFoundException("Rule", rule.Id);
        }

        set.Rules[index] = rule;
        set.UpdatedAt = DateTimeOffset.UtcNow;

        await _guidelineTable.UpsertEntityAsync(GuidelineSetEntity.FromModel(set));

        return rule;
    }

    public async Task RemoveRuleAsync(string guidelineSetId, string ruleId)
    {
        var set = await GetByIdAsync(guidelineSetId)
            ?? throw new NotFoundException("GuidelineSet", guidelineSetId);

        var removed = set.Rules.RemoveAll(r => r.Id == ruleId);
        if (removed == 0)
        {
            throw new NotFoundException("Rule", ruleId);
        }

        set.UpdatedAt = DateTimeOffset.UtcNow;
        await _guidelineTable.UpsertEntityAsync(GuidelineSetEntity.FromModel(set));
    }

    public Task<IReadOnlyList<Rule>> ExtractRulesAsync(string guidelineSetId, string fileName, Stream content)
    {
        // AI-based rule extraction is not implemented yet
        // This feature would analyze uploaded guideline documents and extract rules automatically
        throw new NotImplementedException("AI-based rule extraction is not yet available. Please add rules manually.");
    }

    public async Task<GuidelineSet> ConfirmRulesAsync(string guidelineSetId, IEnumerable<Rule> rules)
    {
        var set = await GetByIdAsync(guidelineSetId)
            ?? throw new NotFoundException("GuidelineSet", guidelineSetId);

        foreach (var rule in rules)
        {
            rule.Id = Guid.NewGuid().ToString();
            set.Rules.Add(rule);
        }

        set.UpdatedAt = DateTimeOffset.UtcNow;
        await _guidelineTable.UpsertEntityAsync(GuidelineSetEntity.FromModel(set));

        return set;
    }

    public async Task<IReadOnlyList<ExampleDocument>> GetExamplesAsync(string guidelineSetId)
    {
        var examples = new List<ExampleDocument>();
        await foreach (var entity in _exampleTable.QueryAsync<ExampleDocumentEntity>(e => e.PartitionKey == guidelineSetId))
        {
            examples.Add(entity.ToModel());
        }
        return examples;
    }

    public async Task<ExampleDocument> AddExampleAsync(string guidelineSetId, string fileName, string? description, Stream content)
    {
        _ = await GetByIdAsync(guidelineSetId)
            ?? throw new NotFoundException("GuidelineSet", guidelineSetId);

        var storagePath = await _storageService.StoreAsync("examples", fileName, content, "application/octet-stream");

        var example = new ExampleDocument
        {
            GuidelineSetId = guidelineSetId,
            FileName = fileName,
            Description = description,
            StoragePath = storagePath
        };

        await _exampleTable.UpsertEntityAsync(ExampleDocumentEntity.FromModel(example));
        _logger.LogInformation("Added example document: {Id}, GuidelineSetId: {GuidelineSetId}", example.Id, guidelineSetId);

        return example;
    }

    public async Task RemoveExampleAsync(string guidelineSetId, string exampleId)
    {
        try
        {
            var response = await _exampleTable.GetEntityAsync<ExampleDocumentEntity>(guidelineSetId, exampleId);
            var example = response.Value.ToModel();

            await _storageService.DeleteAsync(example.StoragePath);
            await _exampleTable.DeleteEntityAsync(guidelineSetId, exampleId);

            _logger.LogInformation("Removed example document: {Id}", exampleId);
        }
        catch (global::Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            throw new NotFoundException("ExampleDocument", exampleId);
        }
    }
}
