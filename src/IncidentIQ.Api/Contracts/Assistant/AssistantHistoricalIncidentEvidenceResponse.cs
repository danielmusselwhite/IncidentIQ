namespace IncidentIQ.Api.Contracts.Assistant;

public sealed record AssistantHistoricalIncidentEvidenceResponse(
    string ReferenceId,
    Guid IncidentId,
    string Title,
    string Description,
    string? Symptoms,
    string Service,
    string Environment,
    string Severity,
    DateTimeOffset CompletedAtUtc);