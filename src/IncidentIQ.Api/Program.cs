using IncidentIQ.Infrastructure;
using IncidentIQ.Infrastructure.Persistence.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add custom services and dependencies from the Infrastructure project
builder.Services.AddInfrastructureDependencies(builder.Configuration);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    options.AddPolicy("ProductionCors", policy =>
    {
        policy
            .WithOrigins("https://incidentiq.com")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("DevelopmentCors");
}
else
{
    app.UseCors("ProductionCors");
}

// Initialize Cosmos DB if run in dev (no need to inmnitialize in prod as it already exists)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var initializer = scope.ServiceProvider.GetRequiredService<CosmosInitializer>();
    await initializer.InitializeAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();