using IncidentIQ.Application;
using IncidentIQ.Infrastructure;
using IncidentIQ.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Register infrastructure services.
builder.Services.AddInfrastructureDependencies(builder.Configuration);

// Register application services and handlers.
builder.Services.AddApplicationDependencies();

// Register the background worker responsible for consuming AnalyseIncident commands from Service Bus.
builder.Services.AddHostedService<AnalyseIncidentWorker>();

var host = builder.Build();

await host.RunAsync();