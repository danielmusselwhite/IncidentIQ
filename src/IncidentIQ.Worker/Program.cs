using IncidentIQ.Application;
using IncidentIQ.Application.Analyse;
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
builder.Services.AddTransient<AnalyseIncidentHandler>(); // Cannot be in AddApplicationDependencies as it is specific to the worker and requires the AzureAIDependencies to be added that the API does not need.

// Relays persisted Cosmos outbox entries into Service Bus.
builder.Services.AddHostedService<IncidentOutboxWorker>();

// Consumes AnalyseIncident commands from Service Bus.
builder.Services.AddHostedService<AnalyseIncidentWorker>();

var host = builder.Build();

await host.RunAsync();