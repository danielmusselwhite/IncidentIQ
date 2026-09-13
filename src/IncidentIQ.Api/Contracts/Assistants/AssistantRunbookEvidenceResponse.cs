namespace IncidentIQ.Api.Contracts.Assistants;

public sealed record AssistantRunbookEvidenceResponse(
    string ReferenceId,
    Guid RunbookId,
    int ChunkIndex,
    string Title,
    string Service,
    string Content);