using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Infrastructure.Persistence.Cosmos;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<CosmosOptions>()
            .Bind(configuration.GetSection(CosmosOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            serializerOptions.Converters.Add(new JsonStringEnumConverter());

            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                UseSystemTextJsonSerializerWithOptions = serializerOptions
            };

            // Local Cosmos Emulator uses its development account key.
            if (!string.IsNullOrWhiteSpace(options.Key))
            {
                return new CosmosClient(
                    options.Endpoint,
                    options.Key,
                    clientOptions);
            }

            // Azure uses Microsoft Entra authentication.
            var credential = new DefaultAzureCredential();

            return new CosmosClient(
                options.Endpoint,
                credential,
                clientOptions);
        });

        services.AddSingleton<CosmosInitializer>();
        services.AddScoped<IIncidentRepository, CosmosIncidentRepository>();
        services.AddScoped<IRunbookRepository, CosmosRunbookRepository>();

        return services;
    }
}
