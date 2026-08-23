using FluentValidation;
using IncidentIQ.Application.Incidents.Create;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentIQ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateIncidentValidator>();

        services.AddScoped<CreateIncidentHandler>();

        return services;
    }
}