using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.GetAll;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Incidents.GetAll;

public sealed class GetAllIncidentsHandlerTests
{
    private readonly Mock<IIncidentRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_WhenIncidentsExist_ReturnsIncidents()
    {
        IReadOnlyCollection<Incident> incidents =
        [
            Incident.Create(
                "Payments outage",
                "Payments are unavailable.",
                "Payments",
                "Production",
                IncidentSeverity.Critical,
                null),

            Incident.Create(
                "Orders latency",
                "Orders are responding slowly.",
                "Orders",
                "Production",
                IncidentSeverity.Medium,
                null)
        ];

        _repository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(incidents);

        var handler = new GetAllIncidentsHandler(_repository.Object);

        var result = await handler.HandleAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(incidents, result);

        _repository.Verify(
            repository => repository.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenNoIncidentsExist_ReturnsEmptyCollection()
    {
        _repository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Incident>());

        var handler = new GetAllIncidentsHandler(_repository.Object);

        var result = await handler.HandleAsync();

        Assert.Empty(result);

        _repository.Verify(
            repository => repository.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}