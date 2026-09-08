using IncidentIQ.Application;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Infrastructure;
using IncidentIQ.Infrastructure.AzureAI;
using IncidentIQ.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Register infrastructure services.
builder.Services.AddInfrastructureDependencies(builder.Configuration);

// Register AI services.
// Development uses deterministic implementations while deployed environments
// use Azure OpenAI for incident analysis and embedding generation.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevelopmentAIDependencies();
}
else
{
    builder.Services.AddAzureAIDependencies(builder.Configuration);
}

// Register application services and Worker-specific handlers.
builder.Services.AddApplicationDependencies();

// These handlers are Worker-specific because they depend on AI services that
// are only required by the asynchronous processing pipelines.
builder.Services.AddScoped<AnalyseIncidentHandler>();
builder.Services.AddScoped<IndexRunbookHandler>();

#region Incident analysis pipeline

// Relays persisted Cosmos outbox entries into Service Bus.
builder.Services.AddHostedService<IncidentOutboxWorker>();

// Consumes AnalyseIncident commands from Service Bus.
builder.Services.AddHostedService<AnalyseIncidentWorker>();

#endregion

#region Runbook indexing pipeline

// Watches Runbook create/update events and publishes IndexRunbook commands.
builder.Services.AddHostedService<RunbookIndexChangeFeedWorker>();

// Consumes IndexRunbook commands from Service Bus and performs the indexing.
builder.Services.AddHostedService<IndexRunbookWorker>();

#endregion

var host = builder.Build();

await host.RunAsync();