using Azure.Monitor.OpenTelemetry.AspNetCore;
using IncidentIQ.Api.ExceptionHandling;
using IncidentIQ.Application;
using IncidentIQ.Infrastructure;
using IncidentIQ.Infrastructure.Persistence.Cosmos;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// application insights telemetry
var applicationInsightsConnectionString =
    builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = applicationInsightsConnectionString;
        });
}

// swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// exception handling middleware
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Add custom services and dependencies from the Infrastructure project
builder.Services.AddInfrastructureDependencies(builder.Configuration);
builder.Services.AddApplicationDependencies();

// CORS
var frontendOrigin = builder.Configuration["Frontend:Origin"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    if (!builder.Environment.IsDevelopment() &&
        !builder.Environment.IsEnvironment("Testing"))
    {
        if (string.IsNullOrWhiteSpace(frontendOrigin))
            throw new InvalidOperationException("Frontend:Origin must be configured in production.");

        options.AddPolicy("ProductionCors", policy =>
        {
            policy
                .WithOrigins(frontendOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    }
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("DevelopmentCors");
}
else if (app.Environment.IsEnvironment("Testing"))
{
    // No specific CORS policy for testing environment.
}
else
{
    app.UseHttpsRedirection();
    app.UseCors("ProductionCors");
}

// Initialize Cosmos DB if running in development.
// Production infrastructure is provisioned through Bicep.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var initializer = scope.ServiceProvider.GetRequiredService<CosmosInitializer>();
    await initializer.InitializeAsync();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();

// Add a partial Program class to allow for integration testing 
// Required to allow WebApplicationFactory<Program> to boot the application in integration tests in memory
public partial class Program
{
}