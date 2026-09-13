using IncidentIQ.Application.Incidents.Analyse.Grounding;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

internal sealed class RunbookChunkEvidenceDocument
{
    public required string ReferenceId { get; init; }

    public required Guid RunbookId { get; init; }

    public required int ChunkIndex { get; init; }

    public required string Title { get; init; }

    public required string Service { get; init; }

    public required string Content { get; init; }

    public required double Distance { get; init; }

    internal static RunbookChunkEvidenceDocument FromApplication(
        RunbookChunkEvidence evidence)
    {
        return new RunbookChunkEvidenceDocument
        {
            ReferenceId = evidence.ReferenceId,
            RunbookId = evidence.RunbookId,
            ChunkIndex = evidence.ChunkIndex,
            Title = evidence.Title,
            Service = evidence.Service,
            Content = evidence.Content,
            Distance = evidence.Distance
        };
    }
}