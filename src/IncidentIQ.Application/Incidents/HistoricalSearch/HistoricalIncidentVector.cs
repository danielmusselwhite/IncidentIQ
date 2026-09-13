namespace IncidentIQ.Application.Incidents.HistoricalSearch;

/// <summary>
/// Represents a completed Incident prepared for semantic similarity search.
/// </summary>
public sealed record HistoricalIncidentVector(
    string IncidentId,
    string Title,
    string Description,
    string? Symptoms,
    string Service,
    string Environment,
    string Severity,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<float> Embedding);