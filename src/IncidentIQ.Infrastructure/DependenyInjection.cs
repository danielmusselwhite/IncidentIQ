using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<CosmosOptions>>()
                .Value;

            return new CosmosClient(
                options.Endpoint,
                options.Key,
                new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway
                });
        });

        services.AddSingleton<CosmosInitializer>();
        services.AddScoped<IIncidentRepository, CosmosIncidentRepository>();

        return services;
    }
}
