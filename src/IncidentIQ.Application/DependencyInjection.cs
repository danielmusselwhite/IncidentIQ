using FluentValidation;
using IncidentIQ.Application.Assistant.Ask;
using IncidentIQ.Application.Assistant.Grounding;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Application.Incidents.Analyse.Retry;
using IncidentIQ.Application.Incidents.Create;
using IncidentIQ.Application.Incidents.GetAll;
using IncidentIQ.Application.Incidents.GetAnalysisById;
using IncidentIQ.Application.Incidents.GetById;
using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Incidents.Operations;
using IncidentIQ.Application.Runbooks.Create;
using IncidentIQ.Application.Runbooks.Delete;
using IncidentIQ.Application.Runbooks.GetAll;
using IncidentIQ.Application.Runbooks.GetById;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
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
        services.AddScoped<GetIncidentAnalysisByIdHandler>();

        services.AddScoped<CreateRunbookHandler>();
        services.AddScoped<GetRunbookByIdHandler>();
        services.AddScoped<GetAllRunbooksHandler>();
        services.AddScoped<UpdateRunbookHandler>();
        services.AddScoped<DeleteRunbookHandler>();

        services.AddTransient<RetryAnalyseIncidentHandler>();

        services.AddSingleton<RunbookChunker>();
        services.AddScoped<RetrieveRunbookChunksHandler>();
        services.AddScoped<RetrieveHistoricalIncidentsHandler>();

        services.AddScoped<IncidentAnalysisContextBuilder>();

        services.AddScoped<OperationalQuestionContextBuilder>();
        services.AddScoped<AskOperationalQuestionHandler>();

        services.AddScoped<GetFailedIncidentsHandler>();
        services.AddScoped<GetOperationsSummaryHandler>();

        return services;
    }
}