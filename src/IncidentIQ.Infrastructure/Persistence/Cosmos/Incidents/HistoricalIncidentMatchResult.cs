using IncidentIQ.Domain.Incidents;
using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Incidents;

/// <summary>
/// Represents the projected result returned by a historical Incident vector query.
/// </summary>
internal sealed class HistoricalIncidentMatchResult
{
    [JsonPropertyName("incidentId")]
    public string IncidentId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("symptoms")]
    public string? Symptoms { get; init; }

    [JsonPropertyName("service")]
    public string Service { get; init; } = string.Empty;

    [JsonPropertyName("environment")]
    public string Environment { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; }

    [JsonPropertyName("completedAtUtc")]
    public DateTimeOffset CompletedAtUtc { get; init; }

    [JsonPropertyName("distance")]
    public double Distance { get; init; }
}