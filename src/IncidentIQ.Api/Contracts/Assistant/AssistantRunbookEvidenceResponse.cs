namespace IncidentIQ.Api.Contracts.Assistant;

public sealed record AssistantRunbookEvidenceResponse(
    string ReferenceId,
    Guid RunbookId,
    int ChunkIndex,
    string Title,
    string Service,
    string Content);