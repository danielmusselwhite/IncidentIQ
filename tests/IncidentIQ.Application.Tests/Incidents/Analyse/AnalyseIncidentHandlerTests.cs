using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.Analyse;

public sealed class AnalyseIncidentHandlerTests
{
    private readonly Mock<IIncidentRepository> _repository = new();
    private readonly Mock<IIncidentAnalyzer> _incidentAnalyzer = new();
    private readonly Mock<IIncidentAnalysisStore> _incidentAnalysisStore = new();

    private readonly Mock<IEmbeddingGenerator> _embeddingGenerator = new();
    private readonly Mock<IHistoricalIncidentRetriever> _historicalIncidentRetriever = new();
    private readonly Mock<IRunbookChunkRetriever> _runbookChunkRetriever = new();

    public AnalyseIncidentHandlerTests()
    {
        // Provide harmless default grounding behaviour for tests that are focused
        // on Incident lifecycle behaviour rather than retrieval itself.
        _embeddingGenerator
            .Setup(generator => generator.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        _historicalIncidentRetriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _runbookChunkRetriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentExists_MarksIncidentCompleted()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);
        var analysisResult = CreateAnalysisResult();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        _incidentAnalysisStore
            .Setup(store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal(IncidentStatus.Completed, incident.Status);
        Assert.Equal(1, incident.AttemptCount);
        Assert.NotNull(incident.LastAttemptAt);
        Assert.NotNull(incident.ProcessingStartedAt);
        Assert.NotNull(incident.CompletedAt);

        // Processing is persisted separately before grounding and AI analysis.
        _repository.Verify(
            repository => repository.UpdateAsync(
                incident,
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Completed Incident and analysis are persisted together.
        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                incident,
                analysisResult,
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentDoesNotExist_ThrowsException()
    {
        // Arrange
        var command = CreateAnalyseIncidentCommand("missing-incident");

        _repository
            .Setup(repository => repository.GetByIdAsync(
                command.IncidentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var handler = CreateHandler();

        // Act + Assert
        await Assert.ThrowsAsync<IncidentNotFoundException>(
            () => handler.HandleAsync(command));

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _embeddingGenerator.Verify(
            generator => generator.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentAnalyzer.Verify(
            analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentIsAlreadyCompleted_DoesNothing()
    {
        // Arrange
        var incident = CreateIncident();
        incident.StartProcessingAttempt();
        incident.MarkCompleted();

        var command = CreateAnalyseIncidentCommand(incident.Id);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal(IncidentStatus.Completed, incident.Status);
        Assert.Equal(1, incident.AttemptCount);

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _embeddingGenerator.Verify(
            generator => generator.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentAnalyzer.Verify(
            analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentIsAlreadyProcessing_RetriesProcessing()
    {
        // Arrange
        var incident = CreateIncident();
        incident.StartProcessingAttempt();

        Assert.Equal(1, incident.AttemptCount);

        var command = CreateAnalyseIncidentCommand(incident.Id);
        var analysisResult = CreateAnalysisResult();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        _incidentAnalysisStore
            .Setup(store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal(IncidentStatus.Completed, incident.Status);
        Assert.Equal(2, incident.AttemptCount);
        Assert.NotNull(incident.LastAttemptAt);
        Assert.NotNull(incident.CompletedAt);

        _repository.Verify(
            repository => repository.UpdateAsync(
                incident,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                incident,
                analysisResult,
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PassesGroundedContextToAnalyzer()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);
        var analysisResult = CreateAnalysisResult();

        IncidentAnalysisContext? capturedContext = null;

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<IncidentAnalysisContext, CancellationToken>(
                (context, _) => capturedContext = context)
            .ReturnsAsync(analysisResult);

        _incidentAnalysisStore
            .Setup(store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.NotNull(capturedContext);

        var capturedInput = capturedContext.Incident;

        Assert.Equal(incident.Title, capturedInput.Title);
        Assert.Equal(incident.Description, capturedInput.Description);
        Assert.Equal(incident.Service, capturedInput.Service);
        Assert.Equal(incident.Environment, capturedInput.Environment);
        Assert.Equal(incident.Severity, capturedInput.Severity);
        Assert.Equal(incident.Symptoms, capturedInput.Symptoms);

        Assert.Empty(capturedContext.HistoricalIncidents);
        Assert.Empty(capturedContext.RunbookChunks);
    }

    [Fact]
    public async Task HandleAsync_PassesRetrievedEvidenceToAnalyzer()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);
        var analysisResult = CreateAnalysisResult();

        var historicalMatches = new List<HistoricalIncidentMatch>
        {
            new(
                IncidentId: Guid.NewGuid(),
                Title: "Previous Payments timeout",
                Description: "Payments previously failed due to an upstream timeout.",
                Symptoms: "HTTP timeout responses",
                Service: "Payments",
                Environment: "Production",
                Severity: IncidentSeverity.High,
                CompletedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
                Distance: 0.8)
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _historicalIncidentRetriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                incident.Service,
                incident.Environment,
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalMatches);

        IncidentAnalysisContext? capturedContext = null;

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<IncidentAnalysisContext, CancellationToken>(
                (context, _) => capturedContext = context)
            .ReturnsAsync(analysisResult);

        _incidentAnalysisStore
            .Setup(store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.NotNull(capturedContext);

        Assert.Single(capturedContext.HistoricalIncidents);

        Assert.Equal(
            historicalMatches[0].IncidentId,
            capturedContext.HistoricalIncidents[0].IncidentId);

        Assert.Empty(capturedContext.RunbookChunks);
    }

    [Fact]
    public async Task HandleAsync_GeneratesEmbeddingOnceForGroundedRetrieval()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);
        var analysisResult = CreateAnalysisResult();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        _incidentAnalysisStore
            .Setup(store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _embeddingGenerator.Verify(
            generator => generator.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenAnalyzerThrows_DoesNotStoreCompletedAnalysis()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI unavailable"));

        var handler = CreateHandler();

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command));

        Assert.Equal(IncidentStatus.Processing, incident.Status);
        Assert.Equal(1, incident.AttemptCount);

        _repository.Verify(
            repository => repository.UpdateAsync(
                incident,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenGroundingFails_DoesNotCallAnalyzerOrStoreCompletedAnalysis()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _embeddingGenerator
            .Setup(generator => generator.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Embedding unavailable"));

        var handler = CreateHandler();

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command));

        Assert.Equal(IncidentStatus.Processing, incident.Status);
        Assert.Equal(1, incident.AttemptCount);

        _incidentAnalyzer.Verify(
            analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<IncidentAnalysisEvidence>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkFailedAsync_WhenIncidentIsProcessing_MarksIncidentFailed()
    {
        // Arrange
        var incident = CreateIncident();
        incident.StartProcessingAttempt();

        var command = CreateAnalyseIncidentCommand(incident.Id);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        // Act
        await handler.MarkFailedAsync(command, "AI service unavailable");

        // Assert
        Assert.Equal(IncidentStatus.Failed, incident.Status);
        Assert.Equal("AI service unavailable", incident.FailureReason);
        Assert.NotNull(incident.FailedAt);

        _repository.Verify(
            repository => repository.UpdateAsync(
                incident,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkFailedAsync_WhenIncidentIsCompleted_DoesNothing()
    {
        // Arrange
        var incident = CreateIncident();
        incident.StartProcessingAttempt();
        incident.MarkCompleted();

        var command = CreateAnalyseIncidentCommand(incident.Id);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        // Act
        await handler.MarkFailedAsync(command, "Late failure");

        // Assert
        Assert.Equal(IncidentStatus.Completed, incident.Status);

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkFailedAsync_WhenIncidentIsAlreadyFailed_DoesNothing()
    {
        // Arrange
        var incident = CreateIncident();
        incident.StartProcessingAttempt();
        incident.MarkFailed("Initial failure");

        var command = CreateAnalyseIncidentCommand(incident.Id);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        // Act
        await handler.MarkFailedAsync(command, "Duplicate failure");

        // Assert
        Assert.Equal(IncidentStatus.Failed, incident.Status);
        Assert.Equal("Initial failure", incident.FailureReason);

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private AnalyseIncidentHandler CreateHandler()
    {
        var contextBuilder = new IncidentAnalysisContextBuilder(
            _embeddingGenerator.Object,
            _historicalIncidentRetriever.Object,
            _runbookChunkRetriever.Object);

        return new AnalyseIncidentHandler(
            _repository.Object,
            _incidentAnalyzer.Object,
            _incidentAnalysisStore.Object,
            contextBuilder);
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

    private static IncidentAnalysisResult CreateAnalysisResult()
    {
        return new IncidentAnalysisResult(
            Summary: "The Payments API is experiencing database-related timeouts.",
            LikelyCauses:
            [
                new LikelyCause(
                    "Database connection or query timeout",
                    0.9,
                    []),
            ],
            RecommendedActions:
            [
                new RecommendedAction(
                    "Inspect database latency and active connections.",
                    [])
            ],
            Model: "test-model",
            AnalysedAtUtc: DateTimeOffset.UtcNow);
    }

    private static AnalyseIncidentCommand CreateAnalyseIncidentCommand(string incidentId)
    {
        return new AnalyseIncidentCommand(
            Guid.NewGuid(),
            incidentId,
            "test-correlation-id",
            DateTimeOffset.UtcNow);
    }
}