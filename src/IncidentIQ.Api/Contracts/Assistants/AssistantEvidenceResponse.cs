namespace IncidentIQ.Api.Contracts.Assistants;

public sealed record AssistantEvidenceResponse(
    IReadOnlyList<AssistantHistoricalIncidentEvidenceResponse> HistoricalIncidents,
    IReadOnlyList<AssistantRunbookEvidenceResponse> RunbookChunks);