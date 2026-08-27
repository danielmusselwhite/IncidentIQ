using IncidentIQ.Api.Contracts.Incidents;
using IncidentIQ.Api.Tests.Infrastructure;
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
}