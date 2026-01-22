using DocCoach.Web.Services.Analyzers;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering review analyzer services.
/// </summary>
public static class AnalyzerServiceCollectionExtensions
{
    /// <summary>
    /// Adds all review analyzer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddReviewAnalyzers(this IServiceCollection services)
    {
        // Register individual analyzers
        services.AddSingleton<IReviewAnalyzer, SentenceLengthAnalyzer>();
        services.AddSingleton<IReviewAnalyzer, ReadabilityAnalyzer>();
        services.AddSingleton<IReviewAnalyzer, PassiveVoiceAnalyzer>();
        services.AddSingleton<IReviewAnalyzer, AIContentAnalyzer>();
        
        // Register the pipeline
        services.AddSingleton<IReviewAnalyzerPipeline, ReviewAnalyzerPipeline>();
        
        return services;
    }
}
