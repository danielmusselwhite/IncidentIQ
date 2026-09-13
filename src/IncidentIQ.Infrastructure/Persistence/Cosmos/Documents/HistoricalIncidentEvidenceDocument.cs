using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

internal sealed class HistoricalIncidentEvidenceDocument
{
    public required string ReferenceId { get; init; }

    public required Guid IncidentId { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public string? Symptoms { get; init; }

    public required string Service { get; init; }

    public required string Environment { get; init; }

    public required IncidentSeverity Severity { get; init; }

    public required DateTimeOffset CompletedAtUtc { get; init; }

    public required double Distance { get; init; }

    internal static HistoricalIncidentEvidenceDocument FromApplication(
        HistoricalIncidentEvidence evidence)
    {
        return new HistoricalIncidentEvidenceDocument
        {
            ReferenceId = evidence.ReferenceId,
            IncidentId = evidence.IncidentId,
            Title = evidence.Title,
            Description = evidence.Description,
            Symptoms = evidence.Symptoms,
            Service = evidence.Service,
            Environment = evidence.Environment,
            Severity = evidence.Severity,
            CompletedAtUtc = evidence.CompletedAtUtc,
            Distance = evidence.Distance
        };
    }
}