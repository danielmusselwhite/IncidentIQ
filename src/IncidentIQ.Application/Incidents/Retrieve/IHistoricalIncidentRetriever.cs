using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;

namespace IncidentIQ.Application.Incidents.Retrieve;

/// <summary>
/// Retrieves semantically similar historical Incidents from the vector store.
/// </summary>
public interface IHistoricalIncidentRetriever
{
    /// <summary>
    /// Retrieves the most semantically similar historical Incidents to the provided query embedding.
    /// </summary>
    /// <param name="queryEmbedding">The embedding vector representing the query.</param>
    /// <param name="service">The service to filter historical Incidents by.</param>
    /// <param name="environment">The environment to filter historical Incidents by.</param>
    /// <param name="topK">The maximum number of historical Incidents to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of historical Incidents that are most semantically similar to the query embedding.</returns>
    Task<IReadOnlyList<HistoricalIncidentMatch>> RetrieveAsync(
        IReadOnlyList<float> queryEmbedding,
        string? service,
        string? environment,
        int topK,
        CancellationToken cancellationToken = default);
}