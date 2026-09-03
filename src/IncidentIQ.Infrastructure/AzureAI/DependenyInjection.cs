using Azure.AI.OpenAI;
using Azure.Identity;
using IncidentIQ.Application.Analyse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services
            .AddOptions<AzureAIOptions>()
            .Bind(configuration.GetSection(AzureAIOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        #region Azure AI

        // AzureOpenAIClient is thread-safe and can be reused across requests.
        // DefaultAzureCredential allows the Worker to authenticate using its managed identity in Azure.
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureAIOptions>>().Value;

            return new AzureOpenAIClient(
                new Uri(options.Endpoint),
                new DefaultAzureCredential());
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

        return services;
    }

    /// <summary>
    /// Registers the deterministic incident analyzer used during local development.
    /// This allows the full asynchronous analysis workflow to run without requiring Azure OpenAI.
    /// </summary>
    public static IServiceCollection AddDevelopmentAIDependencies(this IServiceCollection services)
    {
        services.AddScoped<IIncidentAnalyzer, DevelopmentDummyIncidentAnalyzer>();

        return services;
    }
}