using IncidentIQ.Application;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.HistoricalSearch.Index;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Infrastructure;
using IncidentIQ.Infrastructure.AzureAI;
using IncidentIQ.Infrastructure.Persistence.Cosmos;
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

// These handlers are Worker-specific because they are executed by the
// asynchronous background processing pipelines.
builder.Services.AddScoped<AnalyseIncidentHandler>();
builder.Services.AddScoped<IndexRunbookHandler>();
builder.Services.AddScoped<IndexHistoricalIncidentHandler>();

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

#region Historical Incident indexing pipeline

// Watches completed Incident changes and publishes historical indexing commands.
builder.Services.AddHostedService<HistoricalIncidentIndexChangeFeedWorker>();

// Consumes historical Incident indexing commands and creates vector documents.
builder.Services.AddHostedService<IndexHistoricalIncidentWorker>();

#endregion

var host = builder.Build();

// Initialize Cosmos DB when running locally before hosted Change Feed
// processors begin consuming containers.
// Production infrastructure is provisioned through Bicep.
if (builder.Environment.IsDevelopment())
{
    using var scope = host.Services.CreateScope();
    var cosmosInitializer = scope.ServiceProvider.GetRequiredService<CosmosInitializer>();

    await cosmosInitializer.InitializeAsync();
}

await host.RunAsync();