namespace IncidentIQ.Application.Incidents.Analyse;

/// <summary>
/// Represents the structured result produced by an incident analyser.
/// </summary>
public sealed record IncidentAnalysisResult(
    string Summary,
    IReadOnlyList<LikelyCause> LikelyCauses,
    IReadOnlyList<RecommendedAction> RecommendedActions,
    string Model,
    DateTimeOffset AnalysedAtUtc);
