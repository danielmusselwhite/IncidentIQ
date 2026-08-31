using IncidentIQ.Application;
using IncidentIQ.Infrastructure;
using IncidentIQ.Infrastructure.AzureAI;
using IncidentIQ.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Register infrastructure services.
builder.Services.AddInfrastructureDependencies(builder.Configuration);

// Register Azure AI services.
builder.Services.AddAzureAIDependencies(builder.Configuration);

// Register application services and handlers.
builder.Services.AddApplicationDependencies();

// Relays persisted Cosmos outbox entries into Service Bus.
builder.Services.AddHostedService<IncidentOutboxWorker>();

// Consumes AnalyseIncident commands from Service Bus.
builder.Services.AddHostedService<AnalyseIncidentWorker>();

var host = builder.Build();

await host.RunAsync();