namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Represents a historical Incident returned during retrieval evaluation.
/// </summary>
public sealed record RetrievedHistoricalIncident(
    Guid IncidentId,
    int Rank,
    double Distance,
    bool IsExpected);