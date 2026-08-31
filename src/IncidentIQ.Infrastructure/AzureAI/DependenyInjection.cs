using Azure.AI.OpenAI;
using Azure.Identity;
using IncidentIQ.Application.Analyse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IncidentIQ.Infrastructure.AzureAI;

public static class DependencyInjection
{
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
        // Authentication is keyless: locally DefaultAzureCredential can use developer credentials,
        // while Azure Container Apps uses the Worker's managed identity.
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureAIOptions>>().Value;

            return new AzureOpenAIClient(
                new Uri(options.Endpoint),
                new DefaultAzureCredential());
        });

        // The deployment-specific ChatClient is also reusable and is what the
        // incident analyzer uses to make chat/structured-output requests.
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
}
