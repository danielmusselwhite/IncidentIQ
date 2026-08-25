using IncidentIQ.Api.Contracts.Incidents;
using IncidentIQ.Api.Tests.Infrastructure;
using IncidentIQ.Domain.Incidents;
using System.Net;
using System.Net.Http.Json;
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
        _client = factory.CreateHttpsClient();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
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

        var incident = await response.Content
            .ReadFromJsonAsync<IncidentResponse>(JsonOptions);

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
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
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

        var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var root = json.RootElement;

        Assert.Equal(
            "Validation failed",
            root.GetProperty("title").GetString());

        Assert.True(
            root.GetProperty("errors")
                .TryGetProperty("Title", out _));
    }

    [Fact]
    public async Task GetById_WhenIncidentExists_ReturnsIncident()
    {
        var incident = CreateIncident();

        await _factory.IncidentRepository.CreateAsync(incident);

        var response = await _client.GetAsync(
            $"/api/incidents/{incident.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

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

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Incident not found",
            json.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetAll_WhenIncidentsExist_ReturnsIncidents()
    {
        await _factory.IncidentRepository.CreateAsync(CreateIncident());
        await _factory.IncidentRepository.CreateAsync(CreateIncident());

        var response = await _client.GetAsync("/api/incidents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var incidents = await response.Content
            .ReadFromJsonAsync<IncidentResponse[]>(JsonOptions);

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