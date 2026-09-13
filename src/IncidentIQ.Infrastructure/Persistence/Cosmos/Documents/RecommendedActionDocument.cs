using IncidentIQ.Application.Incidents.Analyse;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Cosmos persistence representation of a recommended Incident action and the
/// grounding evidence references supporting that recommendation.
/// </summary>
internal sealed class RecommendedActionDocument
{
    public required string Action { get; init; }

    // Older analysis documents predate grounded evidence references.
    public IReadOnlyList<string> EvidenceReferences { get; init; } = [];

    internal static RecommendedActionDocument FromApplication(
        RecommendedAction recommendedAction)
    {
        return new RecommendedActionDocument
        {
            Action = recommendedAction.Action,
            EvidenceReferences = recommendedAction.EvidenceReferences
        };
    }

    internal RecommendedAction ToApplication()
    {
        return new RecommendedAction(
            Action,
            EvidenceReferences);
    }
}