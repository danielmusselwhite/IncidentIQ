using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.HistoricalSearch;
using IncidentIQ.Application.Incidents.HistoricalSearch.Index;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.HistoricalSearch.Index;

/// <summary>
/// Tests the application workflow responsible for converting completed Incidents
/// into vector representations that can later be used for semantic retrieval.
/// </summary>
public sealed class IndexHistoricalIncidentHandlerTests
{
    private readonly Mock<IIncidentRepository> _incidentRepository = new();
    private readonly Mock<IEmbeddingGenerator> _embeddingGenerator = new();
    private readonly Mock<IHistoricalIncidentVectorStore> _historicalIncidentVectorStore = new();

    /// <summary>
    /// Verifies that a completed Incident is converted into embedding text,
    /// embedded, and persisted as a historical Incident vector.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenIncidentIsCompleted_GeneratesAndStoresVector()
    {
        // Arrange
        var incident = CreateCompletedIncident();

        var embedding = new float[]
        {
            0.1f,
            0.2f,
            0.3f
        };

        _incidentRepository
            .Setup(x => x.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _embeddingGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var handler = CreateHandler();

        var command = CreateCommand(incident.Id);

        // Act
        await handler.HandleAsync(command);

        // Assert
        _embeddingGenerator.Verify(
            x => x.GenerateAsync(
                It.Is<string>(text =>
                    text.Contains(incident.Title) &&
                    text.Contains(incident.Description) &&
                    text.Contains(incident.Symptoms!)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _historicalIncidentVectorStore.Verify(
            x => x.UpsertAsync(
                It.Is<HistoricalIncidentVector>(vector =>
                    vector.IncidentId == incident.Id &&
                    vector.Title == incident.Title &&
                    vector.Description == incident.Description &&
                    vector.Symptoms == incident.Symptoms &&
                    vector.Service == incident.Service &&
                    vector.Environment == incident.Environment &&
                    vector.Severity == incident.Severity.ToString() &&
                    vector.CompletedAtUtc == incident.CompletedAt &&
                    vector.Embedding.SequenceEqual(embedding)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that Incidents which have not completed successfully are not
    /// embedded or added to the historical semantic-search index.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenIncidentIsNotCompleted_DoesNotGenerateOrStoreVector()
    {
        // Arrange
        var incident = Incident.Create(
            title: "Payment failures",
            description: "Customers cannot complete payments.",
            service: "Payments",
            environment: "Production",
            severity: IncidentSeverity.High,
            symptoms: "Payment gateway requests are timing out.");

        _incidentRepository
            .Setup(x => x.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        var command = CreateCommand(incident.Id);

        // Act
        await handler.HandleAsync(command);

        // Assert
        _embeddingGenerator.Verify(
            x => x.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _historicalIncidentVectorStore.Verify(
            x => x.UpsertAsync(
                It.IsAny<HistoricalIncidentVector>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that indexing fails when the requested Incident cannot be found.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenIncidentDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var incidentId = Guid.NewGuid().ToString();

        _incidentRepository
            .Setup(x => x.GetByIdAsync(
                incidentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var handler = CreateHandler();

        var command = CreateCommand(incidentId);

        // Act
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(command));

        // Assert
        Assert.Contains(incidentId, exception.Message);

        _embeddingGenerator.Verify(
            x => x.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _historicalIncidentVectorStore.Verify(
            x => x.UpsertAsync(
                It.IsAny<HistoricalIncidentVector>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that an Incident can still be indexed when no optional symptoms
    /// were supplied.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenSymptomsAreNull_UsesFallbackEmbeddingText()
    {
        // Arrange
        var incident = CreateCompletedIncident(symptoms: null);

        var embedding = new float[]
        {
            0.1f,
            0.2f,
            0.3f
        };

        _incidentRepository
            .Setup(x => x.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _embeddingGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var handler = CreateHandler();

        var command = CreateCommand(incident.Id);

        // Act
        await handler.HandleAsync(command);

        // Assert
        _embeddingGenerator.Verify(
            x => x.GenerateAsync(
                It.Is<string>(text =>
                    text.Contains(incident.Title) &&
                    text.Contains(incident.Description) &&
                    text.Contains("Symptoms:") &&
                    text.Contains("None provided")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _historicalIncidentVectorStore.Verify(
            x => x.UpsertAsync(
                It.Is<HistoricalIncidentVector>(vector =>
                    vector.IncidentId == incident.Id &&
                    vector.Symptoms == null &&
                    vector.Embedding.SequenceEqual(embedding)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Creates the handler using the mocked dependencies shared by each test.
    /// </summary>
    private IndexHistoricalIncidentHandler CreateHandler()
    {
        return new IndexHistoricalIncidentHandler(
            _incidentRepository.Object,
            _embeddingGenerator.Object,
            _historicalIncidentVectorStore.Object);
    }

    /// <summary>
    /// Creates an Incident and moves it through the valid
    /// Queued → Processing → Completed lifecycle.
    /// </summary>
    private static Incident CreateCompletedIncident(
        string? symptoms = "Payment gateway requests are timing out.")
    {
        var incident = Incident.Create(
            title: "Payment failures",
            description: "Customers cannot complete payments.",
            service: "Payments",
            environment: "Production",
            severity: IncidentSeverity.High,
            symptoms: symptoms);

        incident.StartProcessingAttempt();
        incident.MarkCompleted();

        return incident;
    }

    /// <summary>
    /// Creates an indexing command for the supplied Incident.
    /// </summary>
    private static IndexHistoricalIncidentCommand CreateCommand(string incidentId)
    {
        return new IndexHistoricalIncidentCommand(
            CommandId: Guid.NewGuid(),
            IncidentId: incidentId,
            CorrelationId: Guid.NewGuid().ToString(),
            QueuedAtUtc: DateTimeOffset.UtcNow);
    }
}