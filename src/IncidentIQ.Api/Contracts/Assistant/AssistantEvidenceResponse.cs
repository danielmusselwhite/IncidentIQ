namespace IncidentIQ.Api.Contracts.Assistant;

public sealed record AssistantEvidenceResponse(
    IReadOnlyList<AssistantHistoricalIncidentEvidenceResponse> HistoricalIncidents,
    IReadOnlyList<AssistantRunbookEvidenceResponse> RunbookChunks);