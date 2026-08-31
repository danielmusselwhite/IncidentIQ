using FluentValidation;
using IncidentIQ.Application.Analyse;
using IncidentIQ.Application.Analyse.Retry;
using IncidentIQ.Application.Incidents.Create;
using IncidentIQ.Application.Incidents.GetAll;
using IncidentIQ.Application.Incidents.GetById;
using IncidentIQ.Application.Runbooks.Create;
using IncidentIQ.Application.Runbooks.Delete;
using IncidentIQ.Application.Runbooks.GetAll;
using IncidentIQ.Application.Runbooks.GetById;
using IncidentIQ.Application.Runbooks.Update;
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

        services.AddScoped<CreateRunbookHandler>();
        services.AddScoped<GetRunbookByIdHandler>();
        services.AddScoped<GetAllRunbooksHandler>();
        services.AddScoped<UpdateRunbookHandler>();
        services.AddScoped<DeleteRunbookHandler>();

        services.AddTransient<AnalyseIncidentHandler>();
        services.AddTransient<RetryAnalyseIncidentHandler>();

        return services;
    }
}