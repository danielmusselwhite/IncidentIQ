using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.Analyse.Grounding;

public sealed class IncidentAnalysisContextBuilderTests
{

    private readonly Mock<IEmbeddingGenerator> _embeddingGenerator = new();
    private readonly Mock<IHistoricalIncidentRetriever> _historicalIncidentRetriever = new();
    private readonly Mock<IRunbookChunkRetriever> _runbookChunkRetriever = new();

    [Fact]
    public async Task BuildAsync_RetrievesHistoricalIncidentsAndRunbookChunks()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        var incident = new IncidentAnalysisInput(
            Title: "Payment gateway returning 503",
            Description: "Checkout requests are failing.",
            Service: "Payments",
            Environment: "Production",
            Severity: IncidentSeverity.High,
            Symptoms: "HTTP 503 responses");

        var historicalMatches = new List<HistoricalIncidentMatch>
        {
            // use one valid match here
        };

        var runbookMatches = new List<RunbookChunkMatch>
        {
            // use one valid match here
        };

        _embeddingGenerator
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _historicalIncidentRetriever
            .Setup(x => x.RetrieveAsync(
                embedding,
                "Payments",
                "Production",
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalMatches);

        _runbookChunkRetriever
            .Setup(x => x.RetrieveAsync(
                embedding,
                "Payments",
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(runbookMatches);

        var builder = CreateBuilder();

        // Act
        var context = await builder.BuildAsync(incident);

        // Assert
        Assert.Same(incident, context.Incident);
        Assert.Equal(historicalMatches, context.HistoricalIncidents);
        Assert.Equal(runbookMatches, context.RunbookChunks);

        _embeddingGenerator.Verify(
            x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Build_CreatesExpectedSemanticQuery()
    {
        var incident = new IncidentAnalysisInput(
            Title: "Payment gateway failure",
            Description: "Payments cannot be processed.",
            Service: "Payments",
            Environment: "Production",
            Severity: IncidentSeverity.High,
            Symptoms: "503 responses");

        var result = IncidentRetrievalInputBuilder.Build(incident);

        Assert.Contains("Payment gateway failure", result.QueryText);
        Assert.Contains("Payments cannot be processed.", result.QueryText);
        Assert.Contains("503 responses", result.QueryText);

        Assert.Equal("Payments", result.Service);
        Assert.Equal("Production", result.Environment);
    }

    private IncidentAnalysisContextBuilder CreateBuilder()
    {
        return new IncidentAnalysisContextBuilder(
            _embeddingGenerator.Object,
            _historicalIncidentRetriever.Object,
            _runbookChunkRetriever.Object);
    }
}
