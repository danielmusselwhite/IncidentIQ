namespace IncidentIQ.Application.Runbooks.RetrieveChunks;

/// <summary>
/// Retrieves the Runbook chunks most semantically similar to a query embedding.
/// </summary>
public interface IRunbookChunkRetriever
{

    /// <summary>
    /// Retrieves the Runbook chunks most semantically similar to a query embedding.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding to compare against the Runbook chunks.</param>
    /// <param name="service">The optional service to filter the Runbook chunks.</param>
    /// <param name="topK">The maximum number of top matching chunks to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of RunbookChunkMatch objects representing the top matching chunks.</returns>
    Task<IReadOnlyList<RunbookChunkMatch>> RetrieveAsync(
        IReadOnlyList<float> queryEmbedding,
        string? service,
        int topK,
        CancellationToken cancellationToken = default);
}