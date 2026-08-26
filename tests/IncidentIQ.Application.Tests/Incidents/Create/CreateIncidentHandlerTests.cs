using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Create;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.Create;

public sealed class CreateIncidentHandlerTests
{
    private readonly Mock<IIncidentRepository> _repository = new();
    private readonly Mock<IIncidentAnalysisQueue> _analysisQueue = new();
    private readonly CreateIncidentValidator _validator = new();

    [Fact]
    public async Task HandleAsync_WithValidCommand_CreatesIncidentAndQueuesAnalysis()
    {
        const string correlationId = "test-correlation-id";

        var command = new CreateIncidentCommand(
            "Payments API timeout",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "Database timeout errors");

        _repository
            .Setup(repository => repository.CreateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident incident, CancellationToken _) => incident);

        var handler = new CreateIncidentHandler(
            _repository.Object,
            _analysisQueue.Object,
            _validator);

        var result = await handler.HandleAsync(
            command,
            correlationId);

        Assert.Equal(command.Title, result.Title);
        Assert.Equal(command.Description, result.Description);
        Assert.Equal(command.Service, result.Service);
        Assert.Equal(command.Environment, result.Environment);
        Assert.Equal(command.Severity, result.Severity);
        Assert.Equal(command.Symptoms, result.Symptoms);
        Assert.Equal(IncidentStatus.Queued, result.Status);

        // The incident should be persisted before analysis is requested.
        _repository.Verify(
            repository => repository.CreateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Creating an incident should queue one analysis command
        // containing the newly created incident ID and correlation ID.
        _analysisQueue.Verify(
            queue => queue.EnqueueAsync(
                It.Is<AnalyseIncidentCommand>(analyseCommand =>
                    analyseCommand.IncidentId == result.Id &&
                    analyseCommand.CorrelationId == correlationId &&
                    analyseCommand.CommandId != Guid.Empty),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCommand_ThrowsValidationException()
    {
        const string correlationId = "test-correlation-id";

        var command = new CreateIncidentCommand(
            "",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            null);

        var handler = new CreateIncidentHandler(
            _repository.Object,
            _analysisQueue.Object,
            _validator);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(
                command,
                correlationId));

        // Invalid commands should not be persisted.
        _repository.Verify(
            repository => repository.CreateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Invalid commands should never result in an analysis request.
        _analysisQueue.Verify(
            queue => queue.EnqueueAsync(
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}