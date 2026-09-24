using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.Operations;

public sealed class GetFailedIncidentsHandler(
    IIncidentRepository incidentRepository)
{
    public Task<IReadOnlyCollection<Incident>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        return incidentRepository.GetByStatusAsync(
            IncidentStatus.Failed,
            cancellationToken);
    }
}