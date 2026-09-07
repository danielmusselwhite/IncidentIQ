namespace IncidentIQ.Application.Runbooks.Index;

public sealed record RunbookChunk(
    Guid RunbookId,
    int ChunkIndex,
    string Content,
    string Title,
    string Service,
    DateTime SourceUpdatedAtUtc,
    IReadOnlyList<float> Embedding);