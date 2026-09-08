using Azure.AI.OpenAI;
using Azure.Identity;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Infrastructure.AzureAI.Embedding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ClientModel.Primitives;

namespace IncidentIQ.Infrastructure.AzureAI;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Azure OpenAI embedding services used to generate semantic vectors.
    /// Can be used independently by applications that require embeddings without incident analysis.
    /// </summary>
    public static IServiceCollection AddAzureEmbeddingDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AzureAIOptions>()
            .Bind(configuration.GetSection(AzureAIOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<AzureEmbeddingOptions>()
            .Bind(configuration.GetSection(AzureEmbeddingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // AzureOpenAIClient is thread-safe and can be reused across requests.
        // DefaultAzureCredential allows deployed workloads to authenticate using Managed Identity.
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureAIOptions>>().Value;

            var clientOptions = new AzureOpenAIClientOptions
            {
                NetworkTimeout = TimeSpan.FromSeconds(options.NetworkTimeoutSeconds),
                RetryPolicy = new ClientRetryPolicy(maxRetries: options.MaxRetries)
            };

            return new AzureOpenAIClient(
                new Uri(options.Endpoint),
                new DefaultAzureCredential(),
                clientOptions);
        });

        // EmbeddingClient targets the Azure OpenAI deployment used to vectorise
        // Runbook content and semantic search queries.
        services.AddSingleton(sp =>
        {
            var azureOpenAIClient = sp.GetRequiredService<AzureOpenAIClient>();
            var options = sp.GetRequiredService<IOptions<AzureEmbeddingOptions>>().Value;

            return azureOpenAIClient.GetEmbeddingClient(options.DeploymentName);
        });

        services.AddScoped<IEmbeddingGenerator, AzureEmbeddingGenerator>();

        return services;
    }

    /// <summary>
    /// Registers the full Azure OpenAI dependency set used by the Worker,
    /// including embeddings and structured incident analysis.
    /// </summary>
    public static IServiceCollection AddAzureAIDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // The Worker requires embeddings as well as incident analysis, so build
        // the full AI dependency set on top of the shared embedding services.
        services.AddAzureEmbeddingDependencies(configuration);

        // ChatClient represents the Azure OpenAI deployment used for incident analysis.
        services.AddSingleton(sp =>
        {
            var azureOpenAIClient = sp.GetRequiredService<AzureOpenAIClient>();
            var options = sp.GetRequiredService<IOptions<AzureAIOptions>>().Value;

            return azureOpenAIClient.GetChatClient(options.DeploymentName);
        });

        services.AddScoped<IIncidentAnalyzer, AzureIncidentAnalyzer>();

        return services;
    }

    /// <summary>
    /// Registers the deterministic embedding generator used during local development
    /// without requiring Azure OpenAI.
    /// </summary>
    public static IServiceCollection AddDevelopmentEmbeddingDependencies(
        this IServiceCollection services)
    {
        services.AddScoped<IEmbeddingGenerator, DevelopmentDummyEmbeddingGenerator>();

        return services;
    }

    /// <summary>
    /// Registers the deterministic AI implementations used during local development.
    /// This allows the asynchronous analysis and indexing workflows to run without Azure OpenAI.
    /// </summary>
    public static IServiceCollection AddDevelopmentAIDependencies(
        this IServiceCollection services)
    {
        services.AddDevelopmentEmbeddingDependencies();
        services.AddScoped<IIncidentAnalyzer, DevelopmentDummyIncidentAnalyzer>();

        return services;
    }
}