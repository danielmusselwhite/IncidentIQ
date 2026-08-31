using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.Analyse;

public sealed class AnalyseIncidentHandlerTests
{
    private readonly Mock<IIncidentRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_WhenIncidentExists_MarksIncidentCompleted()
    {
        // Arrange
        var incident = CreateIncident();
        var command = CreateAnalyseIncidentCommand(incident.Id);

        _repository
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = new AnalyseIncidentHandler(_repository.Object);

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal(IncidentStatus.Completed, incident.Status);
        Assert.Equal(1, incident.AttemptCount);
        Assert.NotNull(incident.LastAttemptAt);
        Assert.NotNull(incident.ProcessingStartedAt);
        Assert.NotNull(incident.CompletedAt);

        // The incident is persisted once when processing starts and again when processing completes.
        _repository.Verify(
            repository => repository.UpdateAsync(incident, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentDoesNotExist_ThrowsException()
    {
        // Arrange
        var command = CreateAnalyseIncidentCommand("missing-incident");

        _repository
            .Setup(repository => repository.GetByIdAsync(command.IncidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var handler = new AnalyseIncidentHandler(_repository.Object);

        // Act + Assert
        await Assert.ThrowsAsync<IncidentNotFoundException>(() => handler.HandleAsync(command));

        _repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()),
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

        var handler = new AnalyseIncidentHandler(_repository.Object);

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal(IncidentStatus.Completed, incident.Status);
        Assert.Equal(1, incident.AttemptCount);

        _repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()),
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

        _repository
            .Setup(repository => repository.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = new AnalyseIncidentHandler(_repository.Object);

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal(IncidentStatus.Completed, incident.Status);
        Assert.Equal(2, incident.AttemptCount);
        Assert.NotNull(incident.LastAttemptAt);
        Assert.NotNull(incident.CompletedAt);

        _repository.Verify(
            repository => repository.UpdateAsync(incident, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
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

        var handler = new AnalyseIncidentHandler(_repository.Object);

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

        var handler = new AnalyseIncidentHandler(_repository.Object);

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

        var handler = new AnalyseIncidentHandler(_repository.Object);

        // Act
        await handler.MarkFailedAsync(command, "Duplicate failure");

        // Assert
        Assert.Equal(IncidentStatus.Failed, incident.Status);
        Assert.Equal("Initial failure", incident.FailureReason);

        _repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

    private static AnalyseIncidentCommand CreateAnalyseIncidentCommand(string incidentId)
    {
        return new AnalyseIncidentCommand(
            Guid.NewGuid(),
            incidentId,
            "test-correlation-id",
            DateTimeOffset.UtcNow);
    }
}