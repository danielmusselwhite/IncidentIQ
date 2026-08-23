using FluentValidation;
using IncidentIQ.Application.Incidents.Create;
using IncidentIQ.Application.Incidents.GetAll;
using IncidentIQ.Application.Incidents.GetById;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentIQ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateIncidentValidator>();

        services.AddScoped<CreateIncidentHandler>();
        services.AddScoped<GetAllIncidentsHandler>();
        services.AddScoped<GetIncidentByIdHandler>();

        return services;
    }
}