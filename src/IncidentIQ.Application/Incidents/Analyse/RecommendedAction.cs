namespace IncidentIQ.Application.Incidents.Analyse;

/// <summary>
/// Represents an action recommended for an Incident together with the grounding evidence that supports the recommendation.
/// </summary>
public sealed record RecommendedAction(
    string Action,
    IReadOnlyList<string> EvidenceReferences);