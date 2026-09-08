using IncidentIQ.Application;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Infrastructure;
using IncidentIQ.Infrastructure.AzureAI;
using IncidentIQ.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Register infrastructure services.
builder.Services.AddInfrastructureDependencies(builder.Configuration);

// Register Azure AI services - use dummy AI output locally and real Azure OpenAI in deployed environments.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevelopmentAIDependencies();
}
else
{
    builder.Services.AddAzureAIDependencies(builder.Configuration);
}


// Register application services and handlers.
builder.Services.AddApplicationDependencies();
builder.Services.AddScoped<AnalyseIncidentHandler>(); // Cannot be in AddApplicationDependencies as it is specific to the worker and requires the AzureAIDependencies to be added that the API does not need.
builder.Services.AddScoped<IndexRunbookHandler>(); // Cannot be in AddApplicationDependencies as it is specific to the worker and requires the AzureAIDependencies to be added that the API does not need.

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

// Finally, consumes IndexRunbook commands from Service Bus and performs the indexing.
var host = builder.Build();

await host.RunAsync();