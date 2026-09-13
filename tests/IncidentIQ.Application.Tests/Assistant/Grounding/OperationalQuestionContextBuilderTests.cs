using IncidentIQ.Application.Assistant.Grounding;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Assistant.Grounding;

public sealed class OperationalQuestionContextBuilderTests
{
    [Fact]
    public async Task BuildAsync_RetrievesHistoricalIncidentsAndRunbookChunks()
    {
        // Arrange
        IReadOnlyList<float> embedding = [0.1f, 0.2f, 0.3f];

        var embeddingGenerator = new Mock<IEmbeddingGenerator>();
        var historicalIncidentRetriever = new Mock<IHistoricalIncidentRetriever>();
        var runbookChunkRetriever = new Mock<IRunbookChunkRetriever>();

        embeddingGenerator
            .Setup(generator => generator.GenerateAsync(
                "payment gateway is timing out",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var historicalIncident = new HistoricalIncidentMatch(
            IncidentId: Guid.NewGuid(),
            Title: "Previous payment gateway timeout",
            Description: "Payments failed because the external gateway timed out.",
            Symptoms: "HTTP 504 responses",
            Service: "Payments",
            Environment: "Production",
            Severity: IncidentSeverity.High,
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Distance: 0.8);

        historicalIncidentRetriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.Is<IReadOnlyList<float>>(value => value.SequenceEqual(embedding)),
                "Payments",
                "Production",
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([historicalIncident]);

        var runbookChunk = new RunbookChunkMatch(
            RunbookId: Guid.NewGuid(),
            ChunkIndex: 0,
            Title: "Payment Gateway Recovery",
            Service: "Payments",
            Content: "Check gateway availability and outbound connectivity.",
            Distance: 0.8);

        runbookChunkRetriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.Is<IReadOnlyList<float>>(value => value.SequenceEqual(embedding)),
                "Payments",
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([runbookChunk]);

        // This is the SUT: System Under Test.
        var sut = new OperationalQuestionContextBuilder(
            embeddingGenerator.Object,
            historicalIncidentRetriever.Object,
            runbookChunkRetriever.Object);

        // Act
        var result = await sut.BuildAsync(
            "  payment gateway is timing out  ",
            "Payments",
            "Production");

        // Assert
        // Assert
        Assert.Equal(
            "payment gateway is timing out",
            result.Question);

        Assert.Equal(
            "Payments",
            result.Service);

        Assert.Equal(
            "Production",
            result.Environment);

        var returnedHistoricalIncident =
            Assert.Single(result.HistoricalIncidents);

        Assert.Equal(
            historicalIncident,
            returnedHistoricalIncident);

        var returnedRunbookChunk =
            Assert.Single(result.RunbookChunks);

        Assert.Equal(
            runbookChunk,
            returnedRunbookChunk);

        embeddingGenerator.Verify(
            generator => generator.GenerateAsync(
                "payment gateway is timing out",
                It.IsAny<CancellationToken>()),
            Times.Once);

        historicalIncidentRetriever.Verify(
            retriever => retriever.RetrieveAsync(
                It.Is<IReadOnlyList<float>>(
                    value => value.SequenceEqual(embedding)),
                "Payments",
                "Production",
                3,
                It.IsAny<CancellationToken>()),
            Times.Once);

        runbookChunkRetriever.Verify(
            retriever => retriever.RetrieveAsync(
                It.Is<IReadOnlyList<float>>(
                    value => value.SequenceEqual(embedding)),
                "Payments",
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}