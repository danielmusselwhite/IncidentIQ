using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Incidents.GetById;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.GetById;

public sealed class GetIncidentByIdHandlerTests
{
    private readonly Mock<IIncidentRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_WhenIncidentExists_ReturnsIncident()
    {
        var incident = Incident.Create(
            "Payments API timeout",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "Database timeout errors");

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var handler = new GetIncidentByIdHandler(_repository.Object);

        var result = await handler.HandleAsync(incident.Id);

        Assert.Same(incident, result);

        _repository.Verify(
            repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenIncidentDoesNotExist_ThrowsIncidentNotFoundException()
    {
        const string incidentId = "missing-id";

        _repository
            .Setup(repository => repository.GetByIdAsync(
                incidentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var handler = new GetIncidentByIdHandler(_repository.Object);

        await Assert.ThrowsAsync<IncidentNotFoundException>(
            () => handler.HandleAsync(incidentId));

        _repository.Verify(
            repository => repository.GetByIdAsync(
                incidentId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}