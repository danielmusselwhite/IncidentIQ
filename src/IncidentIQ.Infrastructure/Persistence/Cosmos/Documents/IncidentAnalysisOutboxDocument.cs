using IncidentIQ.Application.Incidents.Analyse;
using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Represents a pending AnalyseIncident command persisted atomically alongside its Incident.
/// </summary>
public sealed class IncidentAnalysisOutboxDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("incidentId")]
    public required string IncidentId { get; init; }

    [JsonPropertyName("documentType")]
    public string DocumentType { get; init; } = "AnalyseIncidentOutbox";

    [JsonPropertyName("commandId")]
    public required Guid CommandId { get; init; }

    [JsonPropertyName("correlationId")]
    public required string CorrelationId { get; init; }

    [JsonPropertyName("queuedAtUtc")]
    public required DateTimeOffset QueuedAtUtc { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("traceParent")]
    public string? TraceParent { get; init; }

    [JsonPropertyName("traceState")]
    public string? TraceState { get; init; }

    public static IncidentAnalysisOutboxDocument FromCommand(AnalyseIncidentCommand command)
    {
        return new IncidentAnalysisOutboxDocument
        {
            Id = $"outbox-{command.CommandId}",
            IncidentId = command.IncidentId,
            CommandId = command.CommandId,
            CorrelationId = command.CorrelationId,
            QueuedAtUtc = command.QueuedAtUtc,
            CreatedAt = DateTimeOffset.UtcNow,
            TraceParent = command.TraceParent,
            TraceState = command.TraceState
        };
    }

    /// <summary>
    /// Converts this outbox document back to an <see cref="AnalyseIncidentCommand"/>.
    /// </summary>
    public AnalyseIncidentCommand ToCommand()
    {
        return new AnalyseIncidentCommand(CommandId, IncidentId, CorrelationId, QueuedAtUtc, TraceParent, TraceState);
    }
}