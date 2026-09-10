using IncidentIQ.Application.Incidents.HistoricalSearch;
using IncidentIQ.Domain.Incidents;
using Newtonsoft.Json;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Represents the Cosmos DB document used to persist the vector representation of a completed historical Incident.
/// </summary>
internal sealed class HistoricalIncidentVectorDocument
{
    [JsonProperty("id")]
    public string Id { get; init; } = string.Empty;

    [JsonProperty("incidentId")]
    public string IncidentId { get; init; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; init; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; init; } = string.Empty;

    [JsonProperty("symptoms")]
    public string? Symptoms { get; init; }

    [JsonProperty("service")]
    public string Service { get; init; } = string.Empty;

    [JsonProperty("environment")]
    public string Environment { get; init; } = string.Empty;

    [JsonProperty("severity")]
    public IncidentSeverity Severity { get; init; }

    [JsonProperty("completedAtUtc")]
    public DateTimeOffset CompletedAtUtc { get; init; }

    [JsonProperty("embedding")]
    public IReadOnlyList<float> Embedding { get; init; } = [];

    /// <summary>
    /// Creates the Cosmos persistence representation of a historical Incident vector.
    /// </summary>
    public static HistoricalIncidentVectorDocument FromApplication(HistoricalIncidentVector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        return new HistoricalIncidentVectorDocument
        {
            // One deterministic vector document exists for each Incident.
            Id = vector.IncidentId,
            IncidentId = vector.IncidentId,
            Title = vector.Title,
            Description = vector.Description,
            Symptoms = vector.Symptoms,
            Service = vector.Service,
            Environment = vector.Environment,
            Severity = Enum.TryParse(vector.Severity, out IncidentSeverity severity) ? severity : IncidentSeverity.Low,
            CompletedAtUtc = vector.CompletedAtUtc,
            Embedding = vector.Embedding
        };
    }
}