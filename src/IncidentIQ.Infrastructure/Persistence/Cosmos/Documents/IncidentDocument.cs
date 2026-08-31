using IncidentIQ.Domain.Incidents;
using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Represents a document for storing incident data in Cosmos DB, as Cosmos uses "Documents" to store data.
/// </summary>
internal sealed class IncidentDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("incidentId")]
    public required string IncidentId { get; init; } // required to allow outbox pattern (to ensure eventual consistency) as Cosmos TransactionalBatch can only atomically operate on items sharing the same logical partition key, so need to make a separate shared IncidentId for both, as otherwise they'd have different unique automatically assigned Id's

    [JsonPropertyName("documentType")]
    public string DocumentType { get; init; } = "Incident";

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string Service { get; init; }

    public required string Environment { get; init; }

    public required IncidentSeverity Severity { get; init; }

    public string? Symptoms { get; init; }

    public required IncidentStatus Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? ProcessingStartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    /// <summary>
    /// Creates an <see cref="IncidentDocument"/> from the specified domain <see cref="Incident"/>.
    /// </summary>
    /// <param name="incident">The domain incident to convert.</param>
    /// <returns>The corresponding <see cref="IncidentDocument"/>.</returns>
    public static IncidentDocument FromDomain(Incident incident)
    {
        return new IncidentDocument
        {
            Id = incident.Id,
            IncidentId = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            Service = incident.Service,
            Environment = incident.Environment,
            Severity = incident.Severity,
            Symptoms = incident.Symptoms,
            Status = incident.Status,
            CreatedAt = incident.CreatedAt,
            UpdatedAt = incident.UpdatedAt,
            ProcessingStartedAt = incident.ProcessingStartedAt,
            CompletedAt = incident.CompletedAt,
            FailureReason = incident.FailureReason,
            FailedAt = incident.FailedAt,
            AttemptCount = incident.AttemptCount,
            LastAttemptAt = incident.LastAttemptAt
        };
    }

    /// <summary>
    /// Converts this <see cref="IncidentDocument"/> back to a domain <see cref="Incident"/>.
    /// </summary>
    /// <returns>The corresponding domain <see cref="Incident"/>.</returns>
    public Incident ToDomain()
    {
        return Incident.Restore(
            Id,
            Title,
            Description,
            Service,
            Environment,
            Severity,
            Symptoms,
            Status,
            CreatedAt,
            UpdatedAt,
            ProcessingStartedAt,
            CompletedAt,
            FailureReason,
            FailedAt,
            AttemptCount,
            LastAttemptAt);
    }
}