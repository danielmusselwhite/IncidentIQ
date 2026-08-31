using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Retry;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.Analyse.Retry;

public sealed class RetryAnalyseIncidentHandlerTests
{
    private readonly Mock<IIncidentRepository> _incidentRepository = new();
    private readonly Mock<IIncidentSubmissionStore> _incidentSubmissionStore = new();

    [Fact]
    public async Task HandleAsync_WhenIncidentDoesNotExist_ShouldThrowIncidentNotFoundException()
    {
        // Arrange
        var incidentId = Guid.NewGuid().ToString();
        var command = new RetryAnalyseIncidentCommand(incidentId, "correlation-123");

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var handler = CreateHandler();

        // Act
        var action = () => handler.HandleAsync(command);

        // Assert
        await Assert.ThrowsAsync<IncidentNotFoundException>(action);

        _incidentSubmissionStore.Verify(
            store => store.RetryAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentIsNotFailed_ShouldThrowIncidentNotRetryableException()
    {
        // Arrange
        var incident = CreateIncident();

        var command = new RetryAnalyseIncidentCommand(
            incident.Id,
            "correlation-123");

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = CreateHandler();

        // Act
        var action = () => handler.HandleAsync(command);

        // Assert
        await Assert.ThrowsAsync<IncidentNotRetryableException>(action);

        _incidentSubmissionStore.Verify(
            store => store.RetryAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentIsFailed_ShouldResetIncidentAndPersistRetry()
    {
        // Arrange
        var incident = CreateFailedIncident();

        var command = new RetryAnalyseIncidentCommand(
            incident.Id,
            "correlation-123");

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentSubmissionStore
            .Setup(store => store.RetryAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()))
            .Returns((Incident retriedIncident, AnalyseIncidentCommand _, CancellationToken _) =>
                Task.FromResult(retriedIncident));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(IncidentStatus.Queued, result.Status);

        Assert.Null(result.FailureReason);
        Assert.Null(result.FailedAt);
        Assert.Null(result.ProcessingStartedAt);
        Assert.Null(result.CompletedAt);

        Assert.Equal(0, result.AttemptCount);
        Assert.Null(result.LastAttemptAt);

        _incidentSubmissionStore.Verify(
            store => store.RetryAsync(
                incident,
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentIsFailed_ShouldCreateNewAnalyseIncidentCommand()
    {
        // Arrange
        var incident = CreateFailedIncident();
        var correlationId = "correlation-123";

        var command = new RetryAnalyseIncidentCommand(
            incident.Id,
            correlationId);

        AnalyseIncidentCommand? capturedAnalyseCommand = null;

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentSubmissionStore
            .Setup(store => store.RetryAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<Incident, AnalyseIncidentCommand, CancellationToken>(
                (_, analyseCommand, _) => capturedAnalyseCommand = analyseCommand)
            .Returns((Incident retriedIncident, AnalyseIncidentCommand _, CancellationToken _) =>
                Task.FromResult(retriedIncident));

        var handler = CreateHandler();

        var before = DateTimeOffset.UtcNow;

        // Act
        await handler.HandleAsync(command);

        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(capturedAnalyseCommand);

        Assert.NotEqual(Guid.Empty, capturedAnalyseCommand.CommandId);
        Assert.Equal(incident.Id, capturedAnalyseCommand.IncidentId);
        Assert.Equal(correlationId, capturedAnalyseCommand.CorrelationId);

        Assert.InRange(
            capturedAnalyseCommand.QueuedAtUtc,
            before,
            after);
    }

    [Fact]
    public async Task HandleAsync_WhenRetrying_ShouldNotUpdateIncidentSeparately()
    {
        // Arrange
        var incident = CreateFailedIncident();

        var command = new RetryAnalyseIncidentCommand(
            incident.Id,
            "correlation-123");

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentSubmissionStore
            .Setup(store => store.RetryAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()))
            .Returns((Incident retriedIncident, AnalyseIncidentCommand _, CancellationToken _) =>
                Task.FromResult(retriedIncident));

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _incidentRepository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentSubmissionStore.Verify(
            store => store.RetryAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private RetryAnalyseIncidentHandler CreateHandler()
    {
        return new RetryAnalyseIncidentHandler(
            _incidentRepository.Object,
            _incidentSubmissionStore.Object);
    }

    private static Incident CreateIncident()
    {
        return Incident.Create(
            "Payment API unavailable",
            "Payment API is returning errors.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "HTTP 500 responses");
    }

    private static Incident CreateFailedIncident()
    {
        var incident = CreateIncident();

        incident.StartProcessingAttempt();
        incident.MarkFailed("Analysis failed.");

        return incident;
    }
}