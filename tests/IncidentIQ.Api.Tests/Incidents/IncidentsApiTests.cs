using IncidentIQ.Api.Contracts.Incidents;
using IncidentIQ.Api.Tests.Infrastructure;
using IncidentIQ.Application.Analyse;
using IncidentIQ.Domain.Incidents;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IncidentIQ.Api.Tests.Incidents;

public sealed class IncidentsApiTests : IClassFixture<IncidentIqApiFactory>
{
    private readonly IncidentIqApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public IncidentsApiTests(IncidentIqApiFactory factory)
    {
        _factory = factory;

        _factory.IncidentRepository.Clear();
        _factory.IncidentSubmissionStore.Clear();
        _factory.IncidentAnalysisReader.Clear();

        _client = factory.CreateHttpsClient();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAndPersistsAnalysisRequest()
    {
        var request = new CreateIncidentRequest(
            "Payments API timeout",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "Database timeout errors");

        var response = await _client.PostAsJsonAsync("/api/incidents", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var incident = await response.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        Assert.NotNull(incident);
        Assert.NotEmpty(incident.Id);
        Assert.Equal(request.Title, incident.Title);
        Assert.Equal(request.Description, incident.Description);
        Assert.Equal(request.Service, incident.Service);
        Assert.Equal(request.Environment, incident.Environment);
        Assert.Equal(request.Severity, incident.Severity);
        Assert.Equal(IncidentStatus.Queued, incident.Status);

        Assert.NotNull(response.Headers.Location);
        Assert.Contains(incident.Id, response.Headers.Location.ToString());

        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var correlationId = response.Headers.GetValues("X-Correlation-ID").Single();

        Assert.False(string.IsNullOrWhiteSpace(correlationId));

        // Creating an incident should atomically persist its analysis request alongside it.
        var analyseCommand = Assert.Single(_factory.IncidentSubmissionStore.Commands);

        Assert.Equal(incident.Id, analyseCommand.IncidentId);
        Assert.Equal(correlationId, analyseCommand.CorrelationId);
        Assert.NotEqual(Guid.Empty, analyseCommand.CommandId);
        Assert.Equal(incident.CreatedAt, analyseCommand.QueuedAtUtc);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequestAndDoesNotPersistAnalysisRequest()
    {
        var request = new CreateIncidentRequest(
            "",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            null);

        var response = await _client.PostAsJsonAsync("/api/incidents", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("Validation failed", root.GetProperty("title").GetString());
        Assert.True(root.GetProperty("errors").TryGetProperty("Title", out _));

        Assert.Empty(_factory.IncidentSubmissionStore.Commands);
        Assert.Empty(await _factory.IncidentRepository.GetAllAsync());
    }

    [Fact]
    public async Task GetById_WhenIncidentExists_ReturnsIncident()
    {
        var incident = CreateIncident();

        await _factory.IncidentRepository.CreateAsync(incident);

        var response = await _client.GetAsync($"/api/incidents/{incident.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(incident.Id, result.Id);
        Assert.Equal(incident.Title, result.Title);
    }

    [Fact]
    public async Task GetById_WhenIncidentDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/incidents/missing-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("Incident not found", json.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetAll_WhenIncidentsExist_ReturnsIncidents()
    {
        await _factory.IncidentRepository.CreateAsync(CreateIncident());
        await _factory.IncidentRepository.CreateAsync(CreateIncident());

        var response = await _client.GetAsync("/api/incidents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var incidents = await response.Content.ReadFromJsonAsync<IncidentResponse[]>(JsonOptions);

        Assert.NotNull(incidents);
        Assert.Equal(2, incidents.Length);
    }

    #region Analysis Tests

    [Fact]
    public async Task GetAnalysis_WhenAnalysisExists_ReturnsAnalysis()
    {
        // Arrange
        var incident = CreateIncident();
        var analysis = CreateAnalysis();

        _factory.IncidentAnalysisReader.Set(incident.Id, analysis);

        // Act
        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/analysis");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<IncidentAnalysisResponse>(JsonOptions);

        Assert.NotNull(result);

        Assert.Equal(analysis.Summary, result.Summary);
        Assert.Equal(analysis.Model, result.Model);
        Assert.Equal(analysis.AnalysedAtUtc, result.AnalysedAtUtc);

        var likelyCause = Assert.Single(result.LikelyCauses);

        Assert.Equal("Database connection pool exhaustion.", likelyCause.Cause);
        Assert.Equal(0.85, likelyCause.Confidence);

        var recommendedAction = Assert.Single(result.RecommendedActions);

        Assert.Equal(
            "Review database connection pool metrics and recent database failures.",
            recommendedAction.Action);
    }

    [Fact]
    public async Task GetAnalysis_WhenAnalysisDoesNotExist_ReturnsNotFoundProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/incidents/missing-id/analysis");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(json.RootElement.TryGetProperty("title", out var title));
        Assert.False(string.IsNullOrWhiteSpace(title.GetString()));
    }

    #endregion

    #region Retry Tests

    [Fact]
    public async Task Retry_WhenIncidentIsFailed_ReturnsAcceptedAndPersistsNewAnalysisRequest()
    {
        // Arrange
        var incident = CreateFailedIncident();

        await _factory.IncidentRepository.CreateAsync(incident);

        // Act
        var response = await _client.PostAsync($"/api/incidents/{incident.Id}/retry", null);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var retriedIncidentResponse = await response.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);
        var retriedIncident = await _factory.IncidentRepository.GetByIdAsync(incident.Id);

        Assert.NotNull(retriedIncident);
        Assert.Equal(incident.Id, retriedIncident.Id);
        Assert.Equal(IncidentStatus.Queued, retriedIncident.Status);
        Assert.Equal(0, retriedIncident.AttemptCount);
        Assert.Null(retriedIncident.LastAttemptAt);
        Assert.Null(retriedIncident.ProcessingStartedAt);
        Assert.Null(retriedIncident.CompletedAt);
        Assert.Null(retriedIncident.FailureReason);
        Assert.Null(retriedIncident.FailedAt);

        Assert.NotNull(retriedIncidentResponse);
        Assert.Equal(incident.Id, retriedIncidentResponse.Id);
        Assert.Equal(IncidentStatus.Queued, retriedIncidentResponse.Status);

        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var correlationId = response.Headers.GetValues("X-Correlation-ID").Single();

        Assert.False(string.IsNullOrWhiteSpace(correlationId));

        var analyseCommand = Assert.Single(_factory.IncidentSubmissionStore.Commands);

        Assert.Equal(incident.Id, analyseCommand.IncidentId);
        Assert.Equal(correlationId, analyseCommand.CorrelationId);
        Assert.NotEqual(Guid.Empty, analyseCommand.CommandId);

        Assert.NotNull(response.Headers.Location);
        Assert.Contains(incident.Id, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Retry_WhenIncidentDoesNotExist_ReturnsNotFoundAndDoesNotPersistAnalysisRequest()
    {
        // Act
        var response = await _client.PostAsync("/api/incidents/missing-id/retry", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("Incident not found", json.RootElement.GetProperty("title").GetString());

        Assert.Empty(_factory.IncidentSubmissionStore.Commands);
    }

    [Fact]
    public async Task Retry_WhenIncidentIsNotFailed_ReturnsConflictAndDoesNotPersistAnalysisRequest()
    {
        // Arrange
        var incident = CreateIncident();

        await _factory.IncidentRepository.CreateAsync(incident);

        // Act
        var response = await _client.PostAsync($"/api/incidents/{incident.Id}/retry", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("Incident not retryable", json.RootElement.GetProperty("title").GetString());

        Assert.Empty(_factory.IncidentSubmissionStore.Commands);
    }

    #endregion

    private static Incident CreateIncident()
    {
        return Incident.Create(
            "Payments API timeout",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "Database timeout errors");
    }

    private static Incident CreateFailedIncident()
    {
        var incident = CreateIncident();

        incident.StartProcessingAttempt();
        incident.MarkFailed("Analysis failed.");

        return incident;
    }

    private static IncidentAnalysisResult CreateAnalysis()
    {
        return new IncidentAnalysisResult(
            "The Payments API is experiencing elevated checkout latency.",
            [
                new LikelyCause(
                    "Database connection pool exhaustion.",
                    0.85)
            ],
            [
                new RecommendedAction(
                    "Review database connection pool metrics and recent database failures.")
            ],
            "test-model",
            new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero));
    }
}