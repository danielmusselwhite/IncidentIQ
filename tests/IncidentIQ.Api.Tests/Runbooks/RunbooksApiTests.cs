using IncidentIQ.Api.Contracts.Runbooks;
using IncidentIQ.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace IncidentIQ.Api.Tests.Runbooks;

public sealed class RunbooksApiTests(
    IncidentIqApiFactory factory)
    : IClassFixture<IncidentIqApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_WithValidRequest_ShouldReturnCreated()
    {
        var request = new CreateRunbookRequest(
            "API Timeout Recovery",
            "Timeout investigation.",
            "Orders API",
            "Check Application Insights.");

        var response = await _client.PostAsJsonAsync(
            "/api/runbooks",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var runbook =
            await response.Content.ReadFromJsonAsync<RunbookResponse>();

        Assert.NotNull(runbook);
        Assert.Equal(request.Title, runbook.Title);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ShouldReturnBadRequest()
    {
        var request = new CreateRunbookRequest(
            "",
            "Description",
            "Service",
            "Content");

        var response = await _client.PostAsJsonAsync(
            "/api/runbooks",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenRunbookExists_ShouldReturnOk()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/runbooks",
            new CreateRunbookRequest(
                "Runbook",
                "Description",
                "Service",
                "Content"));

        var created =
            await createResponse.Content.ReadFromJsonAsync<RunbookResponse>();

        var response =
            await _client.GetAsync($"/api/runbooks/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenRunbookExists_ShouldReturnUpdatedRunbook()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/runbooks",
            new CreateRunbookRequest(
                "Old",
                "Old Description",
                "Old Service",
                "Old Content"));

        var created =
            await createResponse.Content.ReadFromJsonAsync<RunbookResponse>();

        var response = await _client.PutAsJsonAsync(
            $"/api/runbooks/{created!.Id}",
            new UpdateRunbookRequest(
                "Updated",
                "Updated Description",
                "Updated Service",
                "Updated Content"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated =
            await response.Content.ReadFromJsonAsync<RunbookResponse>();

        Assert.Equal("Updated", updated!.Title);
    }

    [Fact]
    public async Task Delete_WhenRunbookExists_ShouldReturnNoContent()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/runbooks",
            new CreateRunbookRequest(
                "Runbook",
                "Description",
                "Service",
                "Content"));

        var created =
            await createResponse.Content.ReadFromJsonAsync<RunbookResponse>();

        var response =
            await _client.DeleteAsync($"/api/runbooks/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse =
            await _client.GetAsync($"/api/runbooks/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}