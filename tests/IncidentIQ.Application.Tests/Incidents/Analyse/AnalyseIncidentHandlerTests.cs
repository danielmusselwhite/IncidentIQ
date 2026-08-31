using IncidentIQ.Application.Analyse;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.Analyse;

public sealed class AnalyseIncidentHandlerTests
{
    private readonly Mock<IIncidentRepository> _repository = new();
    private readonly Mock<IIncidentAnalyzer> _incidentAnalyzer = new();
    private readonly Mock<IIncidentAnalysisStore> _incidentAnalysisStore = new();

    [Fact]
    public async Task HandleAsync_WhenIncidentExists_MarksIncidentCompleted()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);
        var analysisResult = CreateAnalysisResult();

        _repository
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisInput>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        _incidentAnalysisStore
            .Setup(store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
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

        // Processing is persisted separately before the AI request.
        _repository.Verify(
            repository => repository.UpdateAsync(incident, It.IsAny<CancellationToken>()),
            Times.Once);

        // Completed + analysis are persisted together by the analysis store.
        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                incident,
                analysisResult,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentDoesNotExist_ThrowsException()
    {
        // Arrange
        var command = CreateAnalyseIncidentCommand("missing-incident");

        _repository
            .Setup(repository => repository.GetByIdAsync(command.IncidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var handler = CreateHandler();

        // Act + Assert
        await Assert.ThrowsAsync<IncidentNotFoundException>(() => handler.HandleAsync(command));

        _repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentAnalyzer.Verify(
            analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisInput>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
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
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal(IncidentStatus.Completed, incident.Status);
        Assert.Equal(1, incident.AttemptCount);

        _repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentAnalyzer.Verify(
            analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisInput>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
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
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisInput>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        _incidentAnalysisStore
            .Setup(store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
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
            repository => repository.UpdateAsync(incident, It.IsAny<CancellationToken>()),
            Times.Once);

        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                incident,
                analysisResult,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PassesIncidentDataToAnalyzer()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);
        var analysisResult = CreateAnalysisResult();

        IncidentAnalysisInput? capturedInput = null;

        _repository
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisInput>(),
                It.IsAny<CancellationToken>()))
            .Callback<IncidentAnalysisInput, CancellationToken>(
                (input, _) => capturedInput = input)
            .ReturnsAsync(analysisResult);

        _incidentAnalysisStore
            .Setup(store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.NotNull(capturedInput);
        Assert.Equal(incident.Title, capturedInput.Title);
        Assert.Equal(incident.Description, capturedInput.Description);
        Assert.Equal(incident.Service, capturedInput.Service);
        Assert.Equal(incident.Environment, capturedInput.Environment);
        Assert.Equal(incident.Severity, capturedInput.Severity);
        Assert.Equal(incident.Symptoms, capturedInput.Symptoms);
    }

    [Fact]
    public async Task HandleAsync_WhenAnalyzerThrows_DoesNotStoreCompletedAnalysis()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);

        _repository
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentAnalyzer
            .Setup(analyzer => analyzer.AnalyzeIncidentAsync(
                It.IsAny<IncidentAnalysisInput>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI unavailable"));

        var handler = CreateHandler();

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command));

        Assert.Equal(IncidentStatus.Processing, incident.Status);
        Assert.Equal(1, incident.AttemptCount);

        _repository.Verify(
            repository => repository.UpdateAsync(incident, It.IsAny<CancellationToken>()),
            Times.Once);

        _incidentAnalysisStore.Verify(
            store => store.StoreCompletedAnalysisAsync(
                It.IsAny<Incident>(),
                It.IsAny<IncidentAnalysisResult>(),
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
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        // Act
        await handler.MarkFailedAsync(command, "AI service unavailable");

        // Assert
        Assert.Equal(IncidentStatus.Failed, incident.Status);
        Assert.Equal("AI service unavailable", incident.FailureReason);
        Assert.NotNull(incident.FailedAt);

        _repository.Verify(
            repository => repository.UpdateAsync(incident, It.IsAny<CancellationToken>()),
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
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        // Act
        await handler.MarkFailedAsync(command, "Late failure");

        // Assert
        Assert.Equal(IncidentStatus.Completed, incident.Status);

        _repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()),
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
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        // Act
        await handler.MarkFailedAsync(command, "Duplicate failure");

        // Assert
        Assert.Equal(IncidentStatus.Failed, incident.Status);
        Assert.Equal("Initial failure", incident.FailureReason);

        _repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private AnalyseIncidentHandler CreateHandler()
    {
        return new AnalyseIncidentHandler(
            _repository.Object,
            _incidentAnalyzer.Object,
            _incidentAnalysisStore.Object);
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
                    0.9)
            ],
            RecommendedActions:
            [
                new RecommendedAction(
                    "Inspect database latency and active connections.")
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