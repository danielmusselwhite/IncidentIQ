namespace IncidentIQ.Application.Incidents.Analyse;

/// <summary>
/// Represents a likely cause of an Incident together with the model's confidence and the grounding evidence supporting the hypothesis.
/// </summary>
public sealed record LikelyCause(
    string Cause,
    double Confidence,
    IReadOnlyList<string> EvidenceReferences);