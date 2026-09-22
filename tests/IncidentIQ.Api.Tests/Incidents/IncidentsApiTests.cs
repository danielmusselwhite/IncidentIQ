using IncidentIQ.Api.Authorization;
using IncidentIQ.Api.Contracts.Incidents;
using IncidentIQ.Api.Tests.Infrastructure;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
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

        var correlationId = response.Headers
            .GetValues("X-Correlation-ID")
            .Single();

        Assert.False(string.IsNullOrWhiteSpace(correlationId));

        // Creating an Incident should atomically persist its analysis request alongside it.
        var analyseCommand = Assert.Single(
            _factory.IncidentSubmissionStore.Commands);

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

        var response = await _client.PostAsJsonAsync(
            "/api/incidents",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var root = json.RootElement;

        Assert.Equal(
            "Validation failed",
            root.GetProperty("title").GetString());

        Assert.True(
            root.GetProperty("errors")
                .TryGetProperty("Title", out _));

        Assert.Empty(
            _factory.IncidentSubmissionStore.Commands);

        Assert.Empty(
            await _factory.IncidentRepository.GetAllAsync());
    }

    [Fact]
    public async Task GetById_WhenIncidentExists_ReturnsIncident()
    {
        var incident = CreateIncident();

        await _factory.IncidentRepository.CreateAsync(incident);

        var response = await _client.GetAsync(
            $"/api/incidents/{incident.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(incident.Id, result.Id);
        Assert.Equal(incident.Title, result.Title);
    }

    [Fact]
    public async Task GetById_WhenIncidentDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            "/api/incidents/missing-id");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Incident not found",
            json.RootElement
                .GetProperty("title")
                .GetString());
    }

    [Fact]
    public async Task GetAll_WhenIncidentsExist_ReturnsIncidents()
    {
        await _factory.IncidentRepository.CreateAsync(
            CreateIncident());

        await _factory.IncidentRepository.CreateAsync(
            CreateIncident());

        var response = await _client.GetAsync(
            "/api/incidents");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var incidents = await response.Content
            .ReadFromJsonAsync<IncidentResponse[]>(JsonOptions);

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
        var evidence = CreateEvidence();

        _factory.IncidentAnalysisReader.Set(
            incident.Id,
            new GroundedIncidentAnalysis(
                Analysis: analysis,
                Evidence: evidence));

        // Act
        var response = await _client.GetAsync(
            $"/api/incidents/{incident.Id}/analysis");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<IncidentAnalysisResponse>(
                JsonOptions);

        Assert.NotNull(result);

        Assert.Equal(
            analysis.Summary,
            result.Summary);

        Assert.Equal(
            analysis.Model,
            result.Model);

        Assert.Equal(
            analysis.AnalysedAtUtc,
            result.AnalysedAtUtc);

        var likelyCause = Assert.Single(
            result.LikelyCauses);

        Assert.Equal(
            "Database connection pool exhaustion.",
            likelyCause.Cause);

        Assert.Equal(
            0.85,
            likelyCause.Confidence);

        Assert.Equal(
            new[] { "HI-1", "RB-1" },
            likelyCause.EvidenceReferences);

        var recommendedAction = Assert.Single(
            result.RecommendedActions);

        Assert.Equal(
            "Review database connection pool metrics and recent database failures.",
            recommendedAction.Action);

        Assert.Equal(
            new[] { "RB-1" },
            recommendedAction.EvidenceReferences);

        var historicalIncident = Assert.Single(
            result.Evidence.HistoricalIncidents);

        Assert.Equal(
            "HI-1",
            historicalIncident.ReferenceId);

        Assert.Equal(
            evidence.HistoricalIncidents[0].IncidentId,
            historicalIncident.IncidentId);

        Assert.Equal(
            "Previous Payments database timeout",
            historicalIncident.Title);

        Assert.Equal(
            "Payments",
            historicalIncident.Service);

        Assert.Equal(
            "Production",
            historicalIncident.Environment);

        Assert.Equal(
            IncidentSeverity.High.ToString(),
            historicalIncident.Severity);

        Assert.Equal(
            0.82,
            historicalIncident.Distance);

        var runbookChunk = Assert.Single(
            result.Evidence.RunbookChunks);

        Assert.Equal(
            "RB-1",
            runbookChunk.ReferenceId);

        Assert.Equal(
            evidence.RunbookChunks[0].RunbookId,
            runbookChunk.RunbookId);

        Assert.Equal(
            "Payments Database Recovery",
            runbookChunk.Title);

        Assert.Equal(
            2,
            runbookChunk.ChunkIndex);

        Assert.Equal(
            "Payments",
            runbookChunk.Service);

        Assert.Equal(
            "Inspect database connection pool usage and database latency before recycling affected instances.",
            runbookChunk.Content);

        Assert.Equal(
            0.76,
            runbookChunk.Distance);
    }

    [Fact]
    public async Task GetAnalysis_WhenAnalysisHasNoEvidence_ReturnsEmptyEvidence()
    {
        // Arrange
        var incident = CreateIncident();

        var analysis = new IncidentAnalysisResult(
            Summary: "The Payments API is experiencing elevated checkout latency.",
            LikelyCauses:
            [
                new LikelyCause(
                    "Database connection pool exhaustion.",
                    0.85,
                    [])
            ],
            RecommendedActions:
            [
                new RecommendedAction(
                    "Review database connection pool metrics and recent database failures.",
                    [])
            ],
            Model: "test-model",
            AnalysedAtUtc: new DateTimeOffset(
                2026,
                9,
                4,
                12,
                0,
                0,
                TimeSpan.Zero));

        var evidence = new IncidentAnalysisEvidence(
            HistoricalIncidents: [],
            RunbookChunks: []);

        _factory.IncidentAnalysisReader.Set(
            incident.Id,
            new GroundedIncidentAnalysis(
                Analysis: analysis,
                Evidence: evidence));

        // Act
        var response = await _client.GetAsync(
            $"/api/incidents/{incident.Id}/analysis");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<IncidentAnalysisResponse>(
                JsonOptions);

        Assert.NotNull(result);

        Assert.Empty(
            result.LikelyCauses[0].EvidenceReferences);

        Assert.Empty(
            result.RecommendedActions[0].EvidenceReferences);

        Assert.Empty(
            result.Evidence.HistoricalIncidents);

        Assert.Empty(
            result.Evidence.RunbookChunks);
    }

    [Fact]
    public async Task GetAnalysis_WhenAnalysisDoesNotExist_ReturnsNotFoundProblemDetails()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/incidents/missing-id/analysis");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.True(
            json.RootElement.TryGetProperty(
                "title",
                out var title));

        Assert.False(
            string.IsNullOrWhiteSpace(
                title.GetString()));
    }

    #endregion

    #region Retry Tests
    [Fact]
    public async Task Retry_WhenEngineer_ReturnsForbiddenAndDoesNotRetryIncident()
    {
        // Arrange
        var incident =
            CreateFailedIncident();

        await _factory.IncidentRepository.CreateAsync(
            incident);

        // _client represents an Engineer by default.
        // Act
        var response =
            await _client.PostAsync(
                $"/api/incidents/{incident.Id}/retry",
                null);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var persistedIncident =
            await _factory.IncidentRepository
                .GetByIdAsync(
                    incident.Id);

        Assert.NotNull(
            persistedIncident);

        Assert.Equal(
            IncidentStatus.Failed,
            persistedIncident.Status);

        Assert.Empty(
            _factory.IncidentSubmissionStore.Commands);
    }

    [Fact]
    public async Task Retry_WhenIncidentIsFailed_ReturnsAcceptedAndPersistsNewAnalysisRequest()
    {
        // Arrange
        var incident = CreateFailedIncident();

        await _factory.IncidentRepository.CreateAsync(
            incident);

        using var adminClient =
            _factory.CreateHttpsClient(
                IncidentIqRoles.Administrator);

        // Act
        var response = await adminClient.PostAsync(
            $"/api/incidents/{incident.Id}/retry",
            null);

        // Assert
        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        var retriedIncidentResponse = await response.Content
            .ReadFromJsonAsync<IncidentResponse>(
                JsonOptions);

        var retriedIncident =
            await _factory.IncidentRepository
                .GetByIdAsync(incident.Id);

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
        Assert.Equal(
            incident.Id,
            retriedIncidentResponse.Id);

        Assert.Equal(
            IncidentStatus.Queued,
            retriedIncidentResponse.Status);

        Assert.True(
            response.Headers.Contains(
                "X-Correlation-ID"));

        var correlationId = response.Headers
            .GetValues("X-Correlation-ID")
            .Single();

        Assert.False(
            string.IsNullOrWhiteSpace(
                correlationId));

        var analyseCommand = Assert.Single(
            _factory.IncidentSubmissionStore.Commands);

        Assert.Equal(
            incident.Id,
            analyseCommand.IncidentId);

        Assert.Equal(
            correlationId,
            analyseCommand.CorrelationId);

        Assert.NotEqual(
            Guid.Empty,
            analyseCommand.CommandId);

        Assert.NotNull(
            response.Headers.Location);

        Assert.Contains(
            incident.Id,
            response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Retry_WhenIncidentDoesNotExist_ReturnsNotFoundAndDoesNotPersistAnalysisRequest()
    {
        using var adminClient =
            _factory.CreateHttpsClient(
                IncidentIqRoles.Administrator);

        // Act
        var response = await adminClient.PostAsync(
            "/api/incidents/missing-id/retry",
            null);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Incident not found",
            json.RootElement
                .GetProperty("title")
                .GetString());

        Assert.Empty(
            _factory.IncidentSubmissionStore.Commands);
    }

    [Fact]
    public async Task Retry_WhenIncidentIsNotFailed_ReturnsConflictAndDoesNotPersistAnalysisRequest()
    {
        // Arrange
        var incident = CreateIncident();

        await _factory.IncidentRepository.CreateAsync(
            incident);

        using var adminClient =
        _factory.CreateHttpsClient(
            IncidentIqRoles.Administrator);

        // Act
        var response = await adminClient.PostAsync(
            $"/api/incidents/{incident.Id}/retry",
            null);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Incident not retryable",
            json.RootElement
                .GetProperty("title")
                .GetString());

        Assert.Empty(
            _factory.IncidentSubmissionStore.Commands);
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
        incident.MarkFailed(
            "Analysis failed.");

        return incident;
    }

    private static IncidentAnalysisResult CreateAnalysis()
    {
        return new IncidentAnalysisResult(
            Summary:
                "The Payments API is experiencing elevated checkout latency.",
            LikelyCauses:
            [
                new LikelyCause(
                    "Database connection pool exhaustion.",
                    0.85,
                    ["HI-1", "RB-1"])
            ],
            RecommendedActions:
            [
                new RecommendedAction(
                    "Review database connection pool metrics and recent database failures.",
                    ["RB-1"])
            ],
            Model:
                "test-model",
            AnalysedAtUtc:
                new DateTimeOffset(
                    2026,
                    9,
                    4,
                    12,
                    0,
                    0,
                    TimeSpan.Zero));
    }

    private static IncidentAnalysisEvidence CreateEvidence()
    {
        return new IncidentAnalysisEvidence(
            HistoricalIncidents:
            [
                new HistoricalIncidentEvidence(
                    ReferenceId: "HI-1",
                    IncidentId: Guid.Parse(
                        "11111111-1111-1111-1111-111111111111"),
                    Title:
                        "Previous Payments database timeout",
                    Description:
                        "Checkout requests previously failed because database connections were exhausted.",
                    Symptoms:
                        "Database timeout errors and elevated checkout latency",
                    Service:
                        "Payments",
                    Environment:
                        "Production",
                    Severity:
                        IncidentSeverity.High,
                    CompletedAtUtc:
                        new DateTimeOffset(
                            2026,
                            9,
                            1,
                            10,
                            0,
                            0,
                            TimeSpan.Zero),
                    Distance:
                        0.82)
            ],
            RunbookChunks:
            [
                new RunbookChunkEvidence(
                    ReferenceId: "RB-1",
                    RunbookId: Guid.Parse(
                        "22222222-2222-2222-2222-222222222222"),
                    ChunkIndex: 2,
                    Title:
                        "Payments Database Recovery",
                    Service:
                        "Payments",
                    Content:
                        "Inspect database connection pool usage and database latency before recycling affected instances.",
                    Distance:
                        0.76)
            ]);
    }
}