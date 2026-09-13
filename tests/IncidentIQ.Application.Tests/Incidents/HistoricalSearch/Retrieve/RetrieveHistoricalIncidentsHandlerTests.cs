using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.HistoricalSearch.Retrieve;

public sealed class RetrieveHistoricalIncidentsHandlerTests
{
    private readonly Mock<IEmbeddingGenerator> _embeddingGenerator = new();
    private readonly Mock<IHistoricalIncidentRetriever> _historicalIncidentRetriever = new();

    [Fact]
    public async Task HandleAsync_WithValidQuery_GeneratesEmbeddingAndReturnsMatches()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        var incident1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var incident2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var expectedMatches = new List<HistoricalIncidentMatch>
        {
            CreateMatch(incident1Id, "Payment gateway unavailable", 0.82),
            CreateMatch(incident2Id, "Checkout payment failures", 0.74)
        };

        var query = new RetrieveHistoricalIncidentsQuery(
            Query: "Payment gateway returning 503 errors",
            Service: "Payments",
            Environment: "Production",
            TopK: 5);

        _embeddingGenerator
            .Setup(x => x.GenerateAsync(query.Query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _historicalIncidentRetriever
            .Setup(x => x.RetrieveAsync(
                embedding,
                query.Service,
                query.Environment,
                query.TopK,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMatches);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.Equal(expectedMatches, result);

        _embeddingGenerator.Verify(
            x => x.GenerateAsync(query.Query, It.IsAny<CancellationToken>()),
            Times.Once);

        _historicalIncidentRetriever.Verify(
            x => x.RetrieveAsync(
                embedding,
                "Payments",
                "Production",
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TrimsQueryBeforeGeneratingEmbedding()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f };

        var query = new RetrieveHistoricalIncidentsQuery(
            Query: "  Payment gateway timeout  ");

        _embeddingGenerator
            .Setup(x => x.GenerateAsync("Payment gateway timeout", It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _historicalIncidentRetriever
            .Setup(x => x.RetrieveAsync(
                embedding,
                null,
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(query);

        // Assert
        _embeddingGenerator.Verify(
            x => x.GenerateAsync("Payment gateway timeout", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ForwardsFiltersAndTopKToRetriever()
    {
        // Arrange
        var embedding = new float[] { 0.4f, 0.5f };

        var query = new RetrieveHistoricalIncidentsQuery(
            Query: "Checkout failures",
            Service: "Payments",
            Environment: "Staging",
            TopK: 10);

        _embeddingGenerator
            .Setup(x => x.GenerateAsync(query.Query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _historicalIncidentRetriever
            .Setup(x => x.RetrieveAsync(
                embedding,
                query.Service,
                query.Environment,
                query.TopK,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(query);

        // Assert
        _historicalIncidentRetriever.Verify(
            x => x.RetrieveAsync(
                embedding,
                "Payments",
                "Staging",
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyQuery_ThrowsValidationException()
    {
        // Arrange
        var query = new RetrieveHistoricalIncidentsQuery("");

        var handler = CreateHandler();

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(query));

        _embeddingGenerator.Verify(
            x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _historicalIncidentRetriever.Verify(
            x => x.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task HandleAsync_WithInvalidTopK_ThrowsValidationException(int topK)
    {
        // Arrange
        var query = new RetrieveHistoricalIncidentsQuery(
            Query: "Payment failures",
            TopK: topK);

        var handler = CreateHandler();

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(query));

        _embeddingGenerator.Verify(
            x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _historicalIncidentRetriever.Verify(
            x => x.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private RetrieveHistoricalIncidentsHandler CreateHandler()
    {
        return new RetrieveHistoricalIncidentsHandler(
            _embeddingGenerator.Object,
            _historicalIncidentRetriever.Object,
            new RetrieveHistoricalIncidentsValidator());
    }

    private static HistoricalIncidentMatch CreateMatch(Guid incidentId, string title, double distance)
    {
        return new HistoricalIncidentMatch(
            IncidentId: incidentId,
            Title: title,
            Description: "Customers are unable to complete payments.",
            Symptoms: "HTTP 503 responses from the payment gateway.",
            Service: "Payments",
            Environment: "Production",
            Severity: IncidentSeverity.High,
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Distance: distance);
    }
}