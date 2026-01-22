using DocCoach.Web.Models;
using DocCoach.Web.Services.Azure;
using DocCoach.Web.Services.Interfaces;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace DocCoach.Web.Services.Analyzers;

/// <summary>
/// AI-powered analyzer with configurable prompts for flexible semantic analysis.
/// This is a single, unified AI analysis criterion type where the admin configures
/// the evaluation prompt, model settings, and other parameters.
/// Uses the system prompt from AI Settings for consistent behavior across all AI analysis.
/// </summary>
public class AIContentAnalyzer : ReviewAnalyzerBase
{
    private readonly ILogger<AIContentAnalyzer> _logger;
    private readonly IAIConfigurationService _aiConfigurationService;
    private readonly ChatCompletionsClient _chatClient;
    
    // Cached config parameters - initialized lazily on first access using a background task
    private IReadOnlyList<ConfigParameter>? _cachedConfigParameters;
    private string[]? _cachedAvailableModels;
    private string? _cachedDefaultModel;
    
    public AIContentAnalyzer(
        ILogger<AIContentAnalyzer> logger,
        IAIConfigurationService aiConfigurationService,
        IOptions<AzureAIOptions> options)
    {
        _logger = logger;
        _aiConfigurationService = aiConfigurationService;

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
            _logger.LogInformation("AI Content Analyzer initialized with API key authentication at {Endpoint}", inferenceEndpoint);
        }
        else
        {
            // Use DefaultAzureCredential for managed identity (Azure) or local development (Azure CLI)
            var credential = new DefaultAzureCredential();

            var clientOptions = new AzureAIInferenceClientOptions();
            var tokenPolicy = new BearerTokenAuthenticationPolicy(
                credential,
                "https://cognitiveservices.azure.com/.default");
            clientOptions.AddPolicy(tokenPolicy, HttpPipelinePosition.PerRetry);

            _chatClient = new ChatCompletionsClient(inferenceEndpoint, new AzureKeyCredential("placeholder"), clientOptions);
            _logger.LogInformation("AI Content Analyzer initialized with DefaultAzureCredential at {Endpoint}", inferenceEndpoint);
        }
        
        // Initialize cached models asynchronously to avoid blocking
        _ = InitializeCachedModelsAsync();
    }
    
    private async Task InitializeCachedModelsAsync()
    {
        try
        {
            var aiConfig = await _aiConfigurationService.GetActiveConfigurationAsync();
            _cachedAvailableModels = aiConfig.AvailableModels.Count > 0
                ? aiConfig.AvailableModels.ToArray()
                : new[] { "gpt-4o", "gpt-4o-mini" };
            _cachedDefaultModel = aiConfig.Model;
            _cachedConfigParameters = null; // Force rebuild on next access
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize cached AI models, using defaults");
            _cachedAvailableModels = new[] { "gpt-4o", "gpt-4o-mini" };
            _cachedDefaultModel = "gpt-4o";
        }
    }
    
    public override string Id => "ai-analysis";
    public override string Name => "AI Analysis";
    public override string Description => "Configurable AI-powered analysis with custom prompts for semantic document evaluation";
    public override EvaluationMode EvaluationMode => EvaluationMode.AIBased;
    public override int Priority => 50; // Run after static analyzers
    
    public override IReadOnlySet<ReviewDimension> SupportedDimensions => 
        new HashSet<ReviewDimension> 
        { 
            ReviewDimension.Clarity,
            ReviewDimension.Completeness,
            ReviewDimension.FactualSupport,
            ReviewDimension.Compliance,
            ReviewDimension.ToneAndStyle,
            ReviewDimension.Language
        };
    
    public override IReadOnlySet<RuleType> SupportedRuleTypes => 
        new HashSet<RuleType> { RuleType.AIAnalysis };
    
    /// <summary>
    /// Configuration parameters for the AI analyzer.
    /// Administrators can configure prompt, model settings, and behavior.
    /// The model options are loaded from AI Settings (cached at startup).
    /// </summary>
    public override IReadOnlyList<ConfigParameter> ConfigParameters
    {
        get
        {
            if (_cachedConfigParameters != null)
            {
                return _cachedConfigParameters;
            }
            
            // Use cached values or defaults (non-blocking)
            var availableModels = _cachedAvailableModels ?? new[] { "gpt-4o", "gpt-4o-mini" };
            var defaultModel = _cachedDefaultModel ?? "gpt-4o";

            _cachedConfigParameters = new[]
            {
                ConfigParameter.Text(
                    name: "prompt",
                    displayName: "Analysis Prompt",
                    description: "The prompt that guides the AI analysis. Describe what aspects of the document to evaluate. The document text will be provided separately.",
                    defaultValue: "Analyze this audit document for: 1) Completeness of required sections (executive summary, methodology, findings, recommendations), 2) Quality of evidence and citations, 3) Clarity and professional tone, 4) Logical structure and flow."),
                ConfigParameter.Enum(
                    name: "model",
                    displayName: "AI Model",
                    description: "The AI model to use for analysis. Models are configured in AI Settings.",
                    options: availableModels,
                    defaultValue: defaultModel),
                ConfigParameter.Decimal(
                    name: "temperature",
                    displayName: "Temperature",
                    description: "Controls randomness in AI responses. Lower values (0.0-0.3) are more focused and deterministic, higher values (0.7-1.0) are more creative.",
                    minValue: 0,
                    maxValue: 1,
                    defaultValue: 0.3m),
                ConfigParameter.Integer(
                    name: "maxTokens",
                    displayName: "Max Response Tokens",
                    description: "Maximum number of tokens in the AI response. Higher values allow for more detailed feedback.",
                    min: 500,
                    max: 4000,
                    defaultValue: 2000),
                ConfigParameter.Percentage(
                    name: "confidenceThreshold",
                    displayName: "Confidence Threshold (%)",
                    description: "Only report findings with confidence above this threshold",
                    defaultValue: 50)
            };
            
            return _cachedConfigParameters;
        }
    }
    
    /// <summary>
    /// Refreshes the cached available models from AI Settings.
    /// Call this after AI Settings are updated.
    /// </summary>
    public async Task RefreshAvailableModelsAsync()
    {
        await InitializeCachedModelsAsync();
    }
    
    /// <summary>
    /// Configuration record for parsing the criterion's RuleConfiguration.
    /// </summary>
    private record AIAnalysisConfig(
        string? Prompt = null,
        string? Model = null,
        decimal? Temperature = null,
        int? MaxTokens = null,
        int? ConfidenceThreshold = null,
        string? Dimension = null);
    
    /// <summary>
    /// Represents a parsed AI feedback item from the model response.
    /// Matches the JSON structure defined in the system prompt.
    /// </summary>
    private record AIFeedbackItem(
        string? Category,
        string? Severity,
        string Title,
        string Description,
        string? Suggestion,
        AILocationItem? Location);
    
    private record AILocationItem(
        string? Section,
        int? Page,
        string? Excerpt,
        int? StartOffset,
        int? EndOffset);
    
    public override async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var feedbackItems = new List<FeedbackItem>();
        
        try
        {
            // Filter rules that this analyzer should handle
            var relevantRules = context.Rules
                .Where(r => r.IsEnabled && r.RuleType == RuleType.AIAnalysis)
                .ToList();
            
            if (!relevantRules.Any())
            {
                _logger.LogDebug("No AI Analysis rules to evaluate");
                return new AnalysisResult
                {
                    AnalyzerId = Id,
                    AnalyzerName = Name,
                    Success = true,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            
            _logger.LogInformation(
                "AI Content Analyzer evaluating {RuleCount} AI Analysis rules",
                relevantRules.Count);
            
            // Process each AI Analysis rule with its own configuration
            var ruleIndex = 0;
            foreach (var rule in relevantRules)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                
                // Report progress for this rule
                var globalIndex = context.RuleStartIndex + ruleIndex + 1;
                context.OnProgress?.Invoke(AnalysisProgress.Analyzing(
                    rule.Name,
                    globalIndex,
                    context.TotalRuleCount,
                    (int)((globalIndex - 1) / (double)context.TotalRuleCount * 100)));
                
                var ruleFeedback = await AnalyzeRuleAsync(rule, context);
                feedbackItems.AddRange(ruleFeedback);
                
                ruleIndex++;
            }
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "AI Content Analyzer completed with {FeedbackCount} feedback items in {ElapsedMs}ms",
                feedbackItems.Count,
                stopwatch.ElapsedMilliseconds);
            
            return new AnalysisResult
            {
                AnalyzerId = Id,
                AnalyzerName = Name,
                Success = true,
                FeedbackItems = feedbackItems,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                CoveredDimensions = feedbackItems
                    .Select(f => f.Dimension)
                    .Distinct()
                    .ToHashSet(),
                EvaluatedRuleTypes = new HashSet<RuleType> { RuleType.AIAnalysis }
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AI Content Analysis was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI content analysis");
            
            return new AnalysisResult
            {
                AnalyzerId = Id,
                AnalyzerName = Name,
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }
    
    private async Task<List<FeedbackItem>> AnalyzeRuleAsync(Rule rule, AnalysisContext context)
    {
        var feedbackItems = new List<FeedbackItem>();
        
        // Parse configuration from rule
        var config = ParseConfiguration(rule);
        
        // Get AI configuration (includes system prompt)
        var aiConfig = await _aiConfigurationService.GetActiveConfigurationAsync();
        
        var model = config.Model ?? aiConfig.Model;
        var temperature = (float)(config.Temperature ?? (decimal)aiConfig.Temperature);
        var maxTokens = config.MaxTokens ?? aiConfig.MaxTokens;
        
        _logger.LogDebug(
            "Analyzing with AI rule '{RuleName}' using model {Model}, temperature {Temperature}",
            rule.Name,
            model,
            temperature);
        
        // Build the user prompt
        var userPrompt = BuildUserPrompt(rule, config, context, aiConfig);

        var response = await _chatClient.CompleteAsync(
            new ChatCompletionsOptions
            {
                Messages =
                {
                    new ChatRequestSystemMessage(aiConfig.SystemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                Model = model,
                Temperature = temperature,
                MaxTokens = maxTokens
            });

        var aiResponse = response.Value.Content;
        
        // Parse AI response into feedback items
        var parsedFeedback = ParseAIResponse(aiResponse, rule, config);
        feedbackItems.AddRange(parsedFeedback);
        
        return feedbackItems;
    }
    
    private AIAnalysisConfig ParseConfiguration(Rule rule)
    {
        if (string.IsNullOrEmpty(rule.RuleConfiguration))
        {
            return new AIAnalysisConfig();
        }
        
        try
        {
            return JsonSerializer.Deserialize<AIAnalysisConfig>(
                rule.RuleConfiguration,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new AIAnalysisConfig();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI analysis configuration, using defaults");
            return new AIAnalysisConfig();
        }
    }
    
    private static string BuildUserPrompt(Rule rule, AIAnalysisConfig config, AnalysisContext context, AIConfiguration aiConfig)
    {
        // Use the custom prompt from config, or fall back to rule's AIPromptTemplate, or default
        var analysisPrompt = config.Prompt 
            ?? rule.AIPromptTemplate 
            ?? "Analyze this document for quality, completeness, and clarity.";
        
        // Truncate document text if too long using configured max length
        var docText = context.DocumentText;
        var maxDocLength = aiConfig.MaxDocumentLength;
        if (docText.Length > maxDocLength)
        {
            var halfLength = maxDocLength / 2 - 50;
            docText = docText[..halfLength] + 
                      "\n\n[... Document truncated for analysis ...]\n\n" + 
                      docText[^halfLength..];
        }
        
        return $"""
            Please analyze the following document and provide your analysis as a JSON array of feedback items.
            
            ANALYSIS RULE TO APPLY:
            - [{rule.EffectiveDimension}] {rule.Name}: {rule.Description}
            
            SPECIFIC FOCUS:
            {analysisPrompt}
            
            === DOCUMENT START ===
            {docText}
            === DOCUMENT END ===
            
            Provide your analysis as a JSON array of feedback items following the format specified in the system prompt.
            Return ONLY the JSON array, no other text or markdown formatting.
            """;
    }
    
    
    private List<FeedbackItem> ParseAIResponse(string aiResponse, Rule rule, AIAnalysisConfig config)
    {
        var feedbackItems = new List<FeedbackItem>();
        
        // Parse the dimension from config
        var dimension = config.Dimension switch
        {
            "Clarity" => ReviewDimension.Clarity,
            "Completeness" => ReviewDimension.Completeness,
            "FactualSupport" => ReviewDimension.FactualSupport,
            "Compliance" => ReviewDimension.Compliance,
            "ToneAndStyle" => ReviewDimension.ToneAndStyle,
            "Language" => ReviewDimension.Language,
            _ => rule.EffectiveDimension
        };
        
        // Strip markdown code blocks if present (AI sometimes wraps JSON in ```json...```)
        var jsonText = aiResponse.Trim();
        if (jsonText.StartsWith("```"))
        {
            // Find the end of the first line (```json or ```)
            var firstNewline = jsonText.IndexOf('\n');
            if (firstNewline > 0)
            {
                jsonText = jsonText[(firstNewline + 1)..];
            }
            // Remove trailing ```
            var lastBackticks = jsonText.LastIndexOf("```");
            if (lastBackticks > 0)
            {
                jsonText = jsonText[..lastBackticks].Trim();
            }
        }
        
        try
        {
            var items = JsonSerializer.Deserialize<List<AIFeedbackItem>>(
                jsonText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            
            if (items == null) return feedbackItems;
            
            foreach (var item in items)
            {
                var severity = item.Severity?.ToLower() switch
                {
                    "error" => FeedbackSeverity.Error,
                    "warning" => FeedbackSeverity.Warning,
                    _ => FeedbackSeverity.Info
                };
                
                feedbackItems.Add(CreateFeedbackItem(
                    RuleType.AIAnalysis,
                    severity,
                    item.Title,
                    item.Description,
                    item.Suggestion,
                    new DocumentLocation
                    {
                        Section = item.Location?.Section ?? "General",
                        Page = item.Location?.Page,
                        Excerpt = item.Location?.Excerpt,
                        StartOffset = item.Location?.StartOffset,
                        EndOffset = item.Location?.EndOffset
                    },
                    confidence: 0.8, // Default confidence since system prompt doesn't ask for it
                    dimension: dimension,
                    ruleName: rule.Name));
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON");
            
            // If parsing fails, create a single feedback item with the raw response
            feedbackItems.Add(CreateFeedbackItem(
                RuleType.AIAnalysis,
                FeedbackSeverity.Info,
                "AI Analysis Result",
                aiResponse,
                null,
                new DocumentLocation { Section = "General" },
                confidence: 0.5,
                dimension: dimension,
                ruleName: rule.Name));
        }
        
        return feedbackItems;
    }
}
