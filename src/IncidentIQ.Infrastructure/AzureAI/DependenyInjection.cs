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
    /// Registers the real Azure OpenAI incident analyzer and its required clients.
    /// Intended for deployed environments where Azure AI configuration and authentication are available.
    /// </summary>
    public static IServiceCollection AddAzureAIDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        #region Azure AI
        services
            .AddOptions<AzureAIOptions>()
            .Bind(configuration.GetSection(AzureAIOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // AzureOpenAIClient is thread-safe and can be reused across requests.
        // DefaultAzureCredential allows the Worker to authenticate using its managed identity in Azure.
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

        // ChatClient represents the specific Azure OpenAI deployment used for incident analysis.
        services.AddSingleton(sp =>
        {
            var azureOpenAIClient = sp.GetRequiredService<AzureOpenAIClient>();
            var options = sp.GetRequiredService<IOptions<AzureAIOptions>>().Value;

            return azureOpenAIClient.GetChatClient(options.DeploymentName);
        });

        services.AddScoped<IIncidentAnalyzer, AzureIncidentAnalyzer>();

        #endregion

        #region Azure Embedding
        services
            .AddOptions<AzureEmbeddingOptions>()
            .Bind(configuration.GetSection(AzureEmbeddingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // EmbeddingClient targets the specific Azure OpenAI deployment used to generate vectors for Runbook chunks.
        services.AddSingleton(sp =>
        {
            var azureOpenAIClient = sp.GetRequiredService<AzureOpenAIClient>();
            var embeddingOptions = sp.GetRequiredService<IOptions<AzureEmbeddingOptions>>().Value;

            return azureOpenAIClient.GetEmbeddingClient(
                embeddingOptions.DeploymentName);
        });

        services.AddScoped<IEmbeddingGenerator, AzureEmbeddingGenerator>();
        #endregion

        return services;
    }

    /// <summary>
    /// Registers the deterministic incident analyzer used during local development.
    /// This allows the full asynchronous analysis workflow to run without requiring Azure OpenAI.
    /// </summary>
    public static IServiceCollection AddDevelopmentAIDependencies(this IServiceCollection services)
    {
        services.AddScoped<IIncidentAnalyzer, DevelopmentDummyIncidentAnalyzer>();
        services.AddScoped<IEmbeddingGenerator, DevelopmentDummyEmbeddingGenerator>();

        return services;
    }
}