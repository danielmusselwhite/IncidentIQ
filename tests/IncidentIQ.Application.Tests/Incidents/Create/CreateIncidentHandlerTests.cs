using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Create;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.Create;

public sealed class CreateIncidentHandlerTests
{
    private readonly Mock<IIncidentSubmissionStore> _store = new();
    private readonly CreateIncidentValidator _validator = new();

    [Fact]
    public async Task HandleAsync_WithValidCommand_CreatesIncidentAndPersistsAnalysisRequest()
    {
        // Arrange
        const string correlationId = "test-correlation-id";

        var command = new CreateIncidentCommand(
            "Payments API timeout",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "Database timeout errors");

        _store
            .Setup(store => store.CreateAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident incident, AnalyseIncidentCommand _, CancellationToken _) => incident);

        var handler = new CreateIncidentHandler(_store.Object, _validator);

        // Act
        var result = await handler.HandleAsync(command, correlationId);

        // Assert
        Assert.Equal(command.Title, result.Title);
        Assert.Equal(command.Description, result.Description);
        Assert.Equal(command.Service, result.Service);
        Assert.Equal(command.Environment, result.Environment);
        Assert.Equal(command.Severity, result.Severity);
        Assert.Equal(command.Symptoms, result.Symptoms);
        Assert.Equal(IncidentStatus.Queued, result.Status);

        _store.Verify(
            store => store.CreateAsync(
                It.Is<Incident>(incident => incident.Id == result.Id),
                It.Is<AnalyseIncidentCommand>(analyseCommand =>
                    analyseCommand.IncidentId == result.Id &&
                    analyseCommand.CorrelationId == correlationId &&
                    analyseCommand.CommandId != Guid.Empty &&
                    analyseCommand.QueuedAtUtc == result.CreatedAt),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCommand_ThrowsValidationException()
    {
        // Arrange
        const string correlationId = "test-correlation-id";

        var command = new CreateIncidentCommand(
            "",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            null);

        var handler = new CreateIncidentHandler(_store.Object, _validator);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, correlationId));

        // Invalid commands should never persist the Incident or its analysis request.
        _store.Verify(
            store => store.CreateAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}