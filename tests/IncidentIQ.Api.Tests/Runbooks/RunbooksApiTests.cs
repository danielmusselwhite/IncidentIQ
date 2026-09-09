using IncidentIQ.Api.Contracts.Runbooks;
using IncidentIQ.Api.Tests.Fakes;
using IncidentIQ.Api.Tests.Infrastructure;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using System.Net;

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




    [Fact]
    public async Task Search_WhenQueryIsValid_ReturnsMatchingChunks()
    {
        // Arrange
        var runbookId = Guid.NewGuid();

        var retriever = factory.RunbookChunkRetriever;

        retriever.Clear();

        retriever.SetResults(
            new RunbookChunkMatch(
                runbookId,
                0,
                "Payment Gateway Recovery",
                "Payments",
                "Check connectivity to the external payment gateway.",
                0.12));

        // Act
        var response = await _client.GetAsync(
            "/api/runbooks/search?query=payment%20gateway%20timeout&service=Payments&topK=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matches = await response.Content
            .ReadFromJsonAsync<RunbookChunkMatchResponse[]>();

        Assert.NotNull(matches);
        Assert.Single(matches);

        var match = matches[0];

        Assert.Equal(runbookId, match.RunbookId);
        Assert.Equal(0, match.ChunkIndex);
        Assert.Equal("Payment Gateway Recovery", match.Title);
        Assert.Equal("Payments", match.Service);
        Assert.Equal("Check connectivity to the external payment gateway.", match.Content);
        Assert.Equal(0.12, match.Distance);
    }

    [Fact]
    public async Task Search_WhenNoChunksMatch_ReturnsEmptyArray()
    {
        // Arrange
        var retriever = factory.RunbookChunkRetriever;

        retriever.Clear();

        // Act
        var response = await _client.GetAsync(
            "/api/runbooks/search?query=unknown%20failure");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matches = await response.Content
            .ReadFromJsonAsync<RunbookChunkMatchResponse[]>();

        Assert.NotNull(matches);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task Search_WhenQueryIsEmpty_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/runbooks/search?query=&topK=5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task Search_WhenTopKIsOutsideAllowedRange_ReturnsBadRequest(int topK)
    {
        // Act
        var response = await _client.GetAsync(
            $"/api/runbooks/search?query=payment%20timeout&topK={topK}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}