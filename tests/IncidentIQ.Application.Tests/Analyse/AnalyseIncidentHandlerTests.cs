using IncidentIQ.Application.Common.Abstractions;
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
        var incident = Incident.Create(
            "Payments API timeout",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "Database timeout errors");

        var command = new AnalyseIncidentCommand(
            Guid.NewGuid(),
            incident.Id,
            "test-correlation-id",
            DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = new AnalyseIncidentHandler(
            _repository.Object);

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal(
            IncidentStatus.Completed,
            incident.Status);

        Assert.NotNull(
            incident.ProcessingStartedAt);

        Assert.NotNull(
            incident.CompletedAt);

        // The incident is persisted once when processing starts
        // and again when processing completes.
        _repository.Verify(
            repository => repository.UpdateAsync(
                incident,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentDoesNotExist_ThrowsException()
    {
        // Arrange
        var command = new AnalyseIncidentCommand(
            Guid.NewGuid(),
            "missing-incident",
            "test-correlation-id",
            DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                command.IncidentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var handler = new AnalyseIncidentHandler(
            _repository.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command));

        _repository.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}