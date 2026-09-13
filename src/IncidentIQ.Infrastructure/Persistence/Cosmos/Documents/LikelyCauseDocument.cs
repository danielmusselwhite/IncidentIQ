using IncidentIQ.Application.Incidents.Analyse;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Cosmos persistence representation of a likely Incident cause and the
/// grounding evidence references returned for that hypothesis.
/// </summary>
internal sealed class LikelyCauseDocument
{
    public required string Cause { get; init; }

    public required double Confidence { get; init; }

    // Older analysis documents predate grounded evidence references.
    public IReadOnlyList<string> EvidenceReferences { get; init; } = [];

    internal static LikelyCauseDocument FromApplication(LikelyCause likelyCause)
    {
        return new LikelyCauseDocument
        {
            Cause = likelyCause.Cause,
            Confidence = likelyCause.Confidence,
            EvidenceReferences = likelyCause.EvidenceReferences
        };
    }

    internal LikelyCause ToApplication()
    {
        return new LikelyCause(
            Cause,
            Confidence,
            EvidenceReferences);
    }
}