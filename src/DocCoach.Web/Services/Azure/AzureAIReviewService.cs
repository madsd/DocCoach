using System.Diagnostics;
using Azure.Data.Tables;
using DocCoach.Web.Exceptions;
using DocCoach.Web.Models;
using DocCoach.Web.Services.Analyzers;
using DocCoach.Web.Services.Azure.TableEntities;
using DocCoach.Web.Services.Interfaces;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace DocCoach.Web.Services.Azure;

/// <summary>
/// Azure AI Foundry implementation of IReviewService using GPT-4o for document analysis.
/// Persists reviews to Azure Table Storage with feedback items in Blob Storage.
/// </summary>
public class AzureAIReviewService : IReviewService
{
    private readonly ChatCompletionsClient _chatClient;
    private readonly IDocumentService _documentService;
    private readonly IGuidelineService _guidelineService;
    private readonly IAIConfigurationService _aiConfigurationService;
    private readonly IReviewAnalyzerPipeline _analyzerPipeline;
    private readonly IStorageService _storageService;
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureAIReviewService> _logger;
    private const string TableName = "reviews";
    private const string FeedbackContainer = "review-feedback";

    public AzureAIReviewService(
        IDocumentService documentService,
        IGuidelineService guidelineService,
        IAIConfigurationService aiConfigurationService,
        IReviewAnalyzerPipeline analyzerPipeline,
        IStorageService storageService,
        TableServiceClient tableServiceClient,
        IOptions<AzureAIOptions> options,
        ILogger<AzureAIReviewService> logger)
    {
        _documentService = documentService;
        _guidelineService = guidelineService;
        _aiConfigurationService = aiConfigurationService;
        _analyzerPipeline = analyzerPipeline;
        _storageService = storageService;
        _tableClient = tableServiceClient.GetTableClient(TableName);
        _logger = logger;
        
        var config = options.Value;
        
        // Build the inference endpoint
        // Supports formats:
        // 1. Azure OpenAI: https://<name>.openai.azure.com/
        // 2. Azure AI Services (Foundry): https://<name>.services.ai.azure.com/models
        // 3. Azure AI Foundry Project: https://<resource>.services.ai.azure.com/api/projects/<project>
        var baseEndpoint = config.Endpoint.TrimEnd('/');
        Uri inferenceEndpoint;
        
        if (baseEndpoint.Contains(".openai.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            // Azure OpenAI format - use directly
            inferenceEndpoint = new Uri(baseEndpoint);
        }
        else if (baseEndpoint.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
        {
            // Azure AI Services format - already has /models suffix
            inferenceEndpoint = new Uri(baseEndpoint);
        }
        else if (baseEndpoint.Contains("/api/projects", StringComparison.OrdinalIgnoreCase))
        {
            // Azure AI Foundry Project format - extract base and add /models
            var apiProjectsIndex = baseEndpoint.IndexOf("/api/projects", StringComparison.OrdinalIgnoreCase);
            baseEndpoint = baseEndpoint[..apiProjectsIndex];
            inferenceEndpoint = new Uri(baseEndpoint + "/models");
        }
        else
        {
            // Assume services.ai.azure.com style, append /models
            inferenceEndpoint = new Uri(baseEndpoint + "/models");
        }
        
        if (!string.IsNullOrEmpty(config.ApiKey) && config.ApiKey != "YOUR_API_KEY_HERE")
        {
            _chatClient = new ChatCompletionsClient(inferenceEndpoint, new AzureKeyCredential(config.ApiKey));
            _logger.LogInformation("Azure AI Review Service initialized with API key authentication at {Endpoint}", inferenceEndpoint);
        }
        else
        {
            // Use DefaultAzureCredential for managed identity (Azure) or local development (Azure CLI)
            var credential = new DefaultAzureCredential();
            
            // For Azure AI Services, we need the cognitiveservices scope
            var clientOptions = new AzureAIInferenceClientOptions();
            var tokenPolicy = new BearerTokenAuthenticationPolicy(
                credential, 
                "https://cognitiveservices.azure.com/.default");
            clientOptions.AddPolicy(tokenPolicy, HttpPipelinePosition.PerRetry);
            
            // Create client without passing credential to constructor since we're handling auth via policy
            _chatClient = new ChatCompletionsClient(inferenceEndpoint, new AzureKeyCredential("placeholder"), clientOptions);
            _logger.LogInformation("Azure AI Review Service initialized with DefaultAzureCredential at {Endpoint}", inferenceEndpoint);
        }
        
        // Ensure table exists
        _tableClient.CreateIfNotExists();
    }

    public async Task<Review> AnalyzeAsync(string documentId, string guidelineSetId, Action<AnalysisProgress>? onProgress = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Get AI configuration
        var aiConfig = await _aiConfigurationService.GetActiveConfigurationAsync();
        
        var document = await _documentService.GetByIdAsync(documentId)
            ?? throw new NotFoundException("Document", documentId);

        var guidelineSet = await _guidelineService.GetByIdAsync(guidelineSetId)
            ?? throw new NotFoundException("GuidelineSet", guidelineSetId);

        // Ensure text is extracted
        if (string.IsNullOrEmpty(document.ExtractedText))
        {
            onProgress?.Invoke(AnalysisProgress.Extracting(10));
            await _documentService.ExtractTextAsync(documentId);
            document = await _documentService.GetByIdAsync(documentId);
        }

        onProgress?.Invoke(AnalysisProgress.Extracting(20));

        var enabledRules = guidelineSet.EnabledRules.ToList();

        var extractedText = document!.ExtractedText;
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            throw new InvalidOperationException("Document text is not available for analysis.");
        }

        // Check if any AI-based rules exist for summary generation
        var hasAIRules = enabledRules.Any(r => 
            r.EvaluationMode == EvaluationMode.AIBased);

        string summary = string.Empty;

        // Determine total steps: summary (optional) + pipeline + scoring
        // We'll report summary and scoring as separate steps, pipeline reports its own analyzers
        
        if (hasAIRules)
        {
            onProgress?.Invoke(AnalysisProgress.Analyzing("Generating Summary", 0, 0, 25));

            // Generate document summary
            _logger.LogInformation("Generating summary for document {DocumentId}", documentId);
            summary = await GenerateSummaryAsync(extractedText, aiConfig);

            onProgress?.Invoke(AnalysisProgress.Analyzing("Summary Generated", 0, 0, 30));
        }

        try
        {
            // Run ALL rules through the analyzer pipeline
            // The pipeline routes rules to appropriate analyzers based on RuleType
            _logger.LogInformation("Running analyzer pipeline for document {DocumentId} with {RuleCount} rules", 
                documentId, enabledRules.Count);
            
            var analyzerContext = new AnalysisContext
            {
                Document = document!,
                DocumentText = extractedText,
                GuidelineSet = guidelineSet,
                Rules = enabledRules,
                OnProgress = progress =>
                {
                    // Map pipeline's per-criterion progress (0-100%) to our range (30-95%)
                    var adjustedPercent = 30 + (int)(progress.PercentComplete * 0.65);
                    var adjustedProgress = AnalysisProgress.Analyzing(
                        progress.CurrentAnalyzerName ?? "Analyzing",
                        progress.CurrentAnalyzerIndex,
                        progress.TotalAnalyzers,
                        adjustedPercent);
                    onProgress?.Invoke(adjustedProgress);
                }
            };
            
            var pipelineResult = await _analyzerPipeline.ExecuteAsync(analyzerContext);
            
            onProgress?.Invoke(AnalysisProgress.Analyzing("Calculating Scores", 0, 0, 96));
            
            _logger.LogInformation(
                "Pipeline completed with {FeedbackCount} feedback items",
                pipelineResult.FeedbackItems.Count);

            // Calculate scores using dimension-aware scoring
            var dimensionScores = CalculateDimensionScores(pipelineResult.FeedbackItems, guidelineSet);
            var overallScore = CalculateOverallScoreFromDimensions(dimensionScores, guidelineSet);

            stopwatch.Stop();

            var review = new Review
            {
                DocumentId = documentId,
                GuidelineSetId = guidelineSetId,
                OverallScore = overallScore,
                DimensionScores = dimensionScores,
                FeedbackItems = pipelineResult.FeedbackItems,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                ModelUsed = aiConfig.Model,
                Summary = summary,
                ExtractedTextLength = extractedText.Length
            };

            // Store feedback items in blob storage (to avoid 64KB table property limit)
            var feedbackJson = ReviewEntity.SerializeFeedbackItems(pipelineResult.FeedbackItems);
            var feedbackFileName = $"{review.Id}_feedback.json";
            string feedbackBlobPath;
            using (var feedbackStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(feedbackJson)))
            {
                feedbackBlobPath = await _storageService.StoreAsync(FeedbackContainer, feedbackFileName, feedbackStream, "application/json");
            }
            
            _logger.LogDebug("Stored {Count} feedback items to blob storage at {Path}", 
                pipelineResult.FeedbackItems.Count, feedbackBlobPath);

            // Persist review metadata to Table Storage (pass the actual blob path)
            await _tableClient.UpsertEntityAsync(ReviewEntity.FromModel(review, feedbackBlobPath));

            _logger.LogInformation(
                "Completed analysis for document {DocumentId}. Score: {Score}, Items: {ItemCount}, Time: {Time}ms",
                documentId, overallScore, pipelineResult.FeedbackItems.Count, stopwatch.ElapsedMilliseconds);

            onProgress?.Invoke(AnalysisProgress.Complete());

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze document {DocumentId}", documentId);
            throw new InvalidOperationException($"Analysis failed: {ex.Message}", ex);
        }
    }

    private async Task<string> GenerateSummaryAsync(string documentText, AIConfiguration aiConfig)
    {
        try
        {
            // Take first portion of document for summary using configurable length
            var textForSummary = documentText.Length > aiConfig.MaxSummaryDocumentLength 
                ? documentText[..aiConfig.MaxSummaryDocumentLength] 
                : documentText;

            var response = await _chatClient.CompleteAsync(
                new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatRequestSystemMessage(aiConfig.SummarySystemPrompt),
                        new ChatRequestUserMessage(textForSummary)
                    },
                    Model = aiConfig.Model,
                    Temperature = aiConfig.SummaryTemperature,
                    MaxTokens = aiConfig.SummaryMaxTokens
                });

            var summary = response.Value.Content.Trim();
            
            // Ensure it's under 1000 chars
            if (summary.Length > 1000)
            {
                summary = summary[..997] + "...";
            }
            
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate document summary");
            return "Summary generation failed.";
        }
    }

    /// <summary>
    /// Calculates scores per review dimension.
    /// </summary>
    private static Dictionary<ReviewDimension, int> CalculateDimensionScores(
        List<FeedbackItem> feedbackItems, 
        GuidelineSet guidelineSet)
    {
        var scores = new Dictionary<ReviewDimension, int>();

        foreach (ReviewDimension dimension in Enum.GetValues<ReviewDimension>())
        {
            // Check if any rules exist for this dimension
            var hasRules = guidelineSet.EnabledRules
                .Any(r => r.EffectiveDimension == dimension);
            
            if (!hasRules)
            {
                // No rules for this dimension, don't include in scores
                continue;
            }
            
            var dimensionFeedback = feedbackItems
                .Where(f => f.EffectiveDimension == dimension)
                .ToList();
            
            // Start with 100 and deduct based on severity, weighted by confidence
            var score = 100.0;
            foreach (var item in dimensionFeedback)
            {
                var confidence = item.Confidence ?? 1.0;
                var deduction = item.Severity switch
                {
                    FeedbackSeverity.Error => 15,
                    FeedbackSeverity.Warning => 8,
                    FeedbackSeverity.Info => 2,
                    _ => 0
                };
                score -= deduction * confidence;
            }

            scores[dimension] = Math.Max(0, (int)Math.Round(score));
        }

        return scores;
    }

    /// <summary>
    /// Calculates overall score from dimension scores.
    /// </summary>
    private static int CalculateOverallScoreFromDimensions(
        Dictionary<ReviewDimension, int> dimensionScores, 
        GuidelineSet guidelineSet)
    {
        var totalWeight = 0;
        var weightedSum = 0.0;

        foreach (var dimension in dimensionScores)
        {
            // Calculate weight for this dimension from rules
            var dimensionWeight = guidelineSet.EnabledRules
                .Where(r => r.Dimension == dimension.Key)
                .Sum(r => r.Weight);

            // Default weight if no rules defined for dimension
            if (dimensionWeight == 0)
            {
                dimensionWeight = 10; // Default weight
            }

            totalWeight += dimensionWeight;
            weightedSum += dimension.Value * dimensionWeight;
        }

        return totalWeight > 0 ? (int)Math.Round(weightedSum / totalWeight) : 0;
    }

    public async Task<Review?> GetByIdAsync(string id)
    {
        // Reviews are partitioned by documentId, so we need to scan all partitions
        await foreach (var entity in _tableClient.QueryAsync<ReviewEntity>(e => e.RowKey == id))
        {
            var review = entity.ToModel();
            await LoadFeedbackItemsAsync(review, entity);
            return review;
        }
        return null;
    }

    public async Task<Review?> GetByDocumentIdAsync(string documentId)
    {
        var entities = new List<ReviewEntity>();
        await foreach (var entity in _tableClient.QueryAsync<ReviewEntity>(e => e.PartitionKey == documentId))
        {
            entities.Add(entity);
        }
        
        var latestEntity = entities.OrderByDescending(e => e.CreatedAt).FirstOrDefault();
        if (latestEntity == null) return null;
        
        var review = latestEntity.ToModel();
        await LoadFeedbackItemsAsync(review, latestEntity);
        return review;
    }

    public async Task<IReadOnlyList<Review>> GetAllByDocumentIdAsync(string documentId)
    {
        var reviews = new List<Review>();
        var entities = new List<ReviewEntity>();
        
        await foreach (var entity in _tableClient.QueryAsync<ReviewEntity>(e => e.PartitionKey == documentId))
        {
            entities.Add(entity);
        }
        
        foreach (var entity in entities.OrderByDescending(e => e.CreatedAt))
        {
            var review = entity.ToModel();
            await LoadFeedbackItemsAsync(review, entity);
            reviews.Add(review);
        }
        
        return reviews;
    }

    public async Task<IReadOnlyList<Review>> GetAllAsync(int limit = 50)
    {
        var entities = new List<ReviewEntity>();
        await foreach (var entity in _tableClient.QueryAsync<ReviewEntity>())
        {
            entities.Add(entity);
            if (entities.Count >= limit * 2) break; // Get more than needed for sorting
        }
        
        var reviews = new List<Review>();
        foreach (var entity in entities.OrderByDescending(e => e.CreatedAt).Take(limit))
        {
            var review = entity.ToModel();
            await LoadFeedbackItemsAsync(review, entity);
            reviews.Add(review);
        }
        
        return reviews;
    }

    public async Task DeleteAsync(string id)
    {
        // Find the entity first to get the feedback blob path
        await foreach (var entity in _tableClient.QueryAsync<ReviewEntity>(e => e.RowKey == id))
        {
            // Delete feedback blob if it exists (path is already the full storage path)
            if (!string.IsNullOrEmpty(entity.FeedbackItemsBlobPath))
            {
                if (await _storageService.ExistsAsync(entity.FeedbackItemsBlobPath))
                {
                    await _storageService.DeleteAsync(entity.FeedbackItemsBlobPath);
                }
            }
            
            // Delete the table entity
            await _tableClient.DeleteEntityAsync(entity.PartitionKey, id);
            return;
        }
        
        throw new NotFoundException("Review", id);
    }

    public async Task<ReviewComparison> CompareAsync(string reviewId1, string reviewId2)
    {
        var review1 = await GetByIdAsync(reviewId1)
            ?? throw new NotFoundException("Review", reviewId1);
        var review2 = await GetByIdAsync(reviewId2)
            ?? throw new NotFoundException("Review", reviewId2);

        var dimensionDeltas = new Dictionary<ReviewDimension, int>();
        foreach (var dimension in review2.DimensionScores)
        {
            var oldScore = review1.DimensionScores.GetValueOrDefault(dimension.Key, 0);
            dimensionDeltas[dimension.Key] = dimension.Value - oldScore;
        }

        var newIssues = Math.Max(0, review2.FeedbackItems.Count - review1.FeedbackItems.Count);
        var resolvedIssues = Math.Max(0, review1.FeedbackItems.Count - review2.FeedbackItems.Count);

        return new ReviewComparison(
            Review1: review1,
            Review2: review2,
            ScoreDelta: review2.OverallScore - review1.OverallScore,
            DimensionDeltas: dimensionDeltas,
            NewIssuesCount: newIssues,
            ResolvedIssuesCount: resolvedIssues
        );
    }

    /// <summary>
    /// Loads feedback items from blob storage into the review object.
    /// </summary>
    private async Task LoadFeedbackItemsAsync(Review review, ReviewEntity entity)
    {
        if (string.IsNullOrEmpty(entity.FeedbackItemsBlobPath))
        {
            _logger.LogDebug("No feedback blob path for review {ReviewId}, feedback items empty", review.Id);
            return;
        }

        // FeedbackItemsBlobPath is the full storage path returned by StoreAsync (includes container)
        var blobPath = entity.FeedbackItemsBlobPath;
        
        try
        {
            if (!await _storageService.ExistsAsync(blobPath))
            {
                _logger.LogWarning("Feedback blob not found at {Path} for review {ReviewId}", blobPath, review.Id);
                return;
            }

            using var stream = await _storageService.RetrieveAsync(blobPath);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            
            review.FeedbackItems = ReviewEntity.DeserializeFeedbackItems(json);
            _logger.LogDebug("Loaded {Count} feedback items from blob for review {ReviewId}", 
                review.FeedbackItems.Count, review.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load feedback items from blob {Path} for review {ReviewId}", 
                blobPath, review.Id);
            // Don't fail the whole operation, just return empty feedback
            review.FeedbackItems = new List<FeedbackItem>();
        }
    }
}
