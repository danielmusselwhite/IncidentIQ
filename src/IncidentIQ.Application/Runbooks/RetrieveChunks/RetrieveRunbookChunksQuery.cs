namespace IncidentIQ.Application.Runbooks.RetrieveChunks;

/// <summary>
/// Represents a request to find Runbook chunks relevant to natural-language text.
/// </summary>
public sealed record RetrieveRunbookChunksQuery(
    string Query,
    string? Service = null,
    int TopK = 5);