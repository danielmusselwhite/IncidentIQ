using Azure.Monitor.OpenTelemetry.AspNetCore;
using IncidentIQ.Api.ExceptionHandling;
using IncidentIQ.Application;
using IncidentIQ.Infrastructure;
using IncidentIQ.Infrastructure.AzureAI;
using IncidentIQ.Infrastructure.Persistence.Cosmos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// API
// -----------------------------------------------------------------------------

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// -----------------------------------------------------------------------------
// Authentication & authorization
// -----------------------------------------------------------------------------

// IncidentIQ is a protected Microsoft Entra web API.
//
// Microsoft.Identity.Web validates incoming bearer tokens including their
// signature, issuer, audience and lifetime against the configured Entra tenant.
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// -----------------------------------------------------------------------------
// Application Insights telemetry
// -----------------------------------------------------------------------------

var applicationInsightsConnectionString =
    builder.Configuration[
        "APPLICATIONINSIGHTS_CONNECTION_STRING"];

if (!string.IsNullOrWhiteSpace(
        applicationInsightsConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString =
                applicationInsightsConnectionString;
        });
}

// -----------------------------------------------------------------------------
// Swagger
// -----------------------------------------------------------------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -----------------------------------------------------------------------------
// Exception handling
// -----------------------------------------------------------------------------

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// -----------------------------------------------------------------------------
// Application / Infrastructure
// -----------------------------------------------------------------------------

builder.Services.AddInfrastructureDependencies(
    builder.Configuration);

builder.Services.AddApplicationDependencies();

// The API uses embeddings for semantic retrieval and the Operational Assistant
// for grounded conversational questions.
//
// Development and Testing use deterministic AI implementations so local and
// automated tests do not require Azure OpenAI.
if (builder.Environment.IsDevelopment() ||
    builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDevelopmentAIDependencies();
}
else
{
    builder.Services.AddAzureAIDependencies(
        builder.Configuration);
}

// -----------------------------------------------------------------------------
// CORS
// -----------------------------------------------------------------------------

var frontendOrigin =
    builder.Configuration["Frontend:Origin"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "DevelopmentCors",
        policy =>
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
        {
            throw new InvalidOperationException(
                "Frontend:Origin must be configured in production.");
        }

        options.AddPolicy(
            "ProductionCors",
            policy =>
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

// -----------------------------------------------------------------------------
// HTTP pipeline
// -----------------------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("DevelopmentCors");
}
else if (app.Environment.IsEnvironment("Testing"))
{
    // No specific CORS policy for the testing environment.
}
else
{
    app.UseHttpsRedirection();
    app.UseCors("ProductionCors");
}

// Initialize Cosmos DB when running locally.
// Production infrastructure is provisioned through Bicep.
if (app.Environment.IsDevelopment())
{
    using var scope =
        app.Services.CreateScope();

    var initializer =
        scope.ServiceProvider
            .GetRequiredService<CosmosInitializer>();

    await initializer.InitializeAsync();
}

// Authentication must run before authorization so ASP.NET can construct the
// ClaimsPrincipal used by authorization policies and endpoint requirements.
app.UseAuthentication();
app.UseAuthorization();

// All controller endpoints require:
//
// 1. an authenticated Microsoft Entra user;
// 2. an access token containing the delegated access_as_user scope.
app.MapControllers()
    .RequireAuthorization()
    .RequireScope("access_as_user");

// Health checks deliberately remain anonymous so Azure Container Apps and
// external health probes do not need to acquire an Entra access token.
app.MapHealthChecks("/api/health");

app.Run();

// Required to allow WebApplicationFactory<Program> to boot the API
// application in memory during integration tests.
public partial class Program
{
}