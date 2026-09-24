using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.Operations;

public sealed class GetOperationsSummaryHandler(IIncidentRepository incidentRepository)
{
    public async Task<OperationsSummary> HandleAsync(CancellationToken cancellationToken = default)
    {
        var incidents = await incidentRepository.GetAllAsync(cancellationToken);

        return new OperationsSummary(
            Queued: incidents.Count(x => x.Status == IncidentStatus.Queued),
            Processing: incidents.Count(x => x.Status == IncidentStatus.Processing),
            Completed: incidents.Count(x => x.Status == IncidentStatus.Completed),
            Failed: incidents.Count(x => x.Status == IncidentStatus.Failed),
            Total: incidents.Count);
    }
}

public sealed record OperationsSummary(
    int Queued,
    int Processing,
    int Completed,
    int Failed,
    int Total);