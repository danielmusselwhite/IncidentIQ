using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Create;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.Create;

public sealed class CreateIncidentHandlerTests
{
    private readonly Mock<IIncidentRepository> _repository = new();
    private readonly CreateIncidentValidator _validator = new();

    [Fact]
    public async Task HandleAsync_WithValidCommand_CreatesIncident()
    {
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
            _validator);

        var result = await handler.HandleAsync(command);

        Assert.Equal(command.Title, result.Title);
        Assert.Equal(command.Description, result.Description);
        Assert.Equal(command.Service, result.Service);
        Assert.Equal(command.Environment, result.Environment);
        Assert.Equal(command.Severity, result.Severity);
        Assert.Equal(command.Symptoms, result.Symptoms);
        Assert.Equal(IncidentStatus.Queued, result.Status);

        _repository.Verify(
            repository => repository.CreateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCommand_ThrowsValidationException()
    {
        var command = new CreateIncidentCommand(
            "",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            null);

        var handler = new CreateIncidentHandler(
            _repository.Object,
            _validator);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(command));

        _repository.Verify(
            repository => repository.CreateAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}