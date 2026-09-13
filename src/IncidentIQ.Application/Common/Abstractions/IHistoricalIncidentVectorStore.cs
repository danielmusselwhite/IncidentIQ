using IncidentIQ.Application.Incidents.HistoricalSearch;

namespace IncidentIQ.Application.Common.Abstractions;

/// <summary>
/// Persists the vector representation used to semantically search historical Incidents.
/// </summary>
public interface IHistoricalIncidentVectorStore
{
    /// <summary>
    /// Persists the vector representation of a completed Incident for semantic similarity search.
    /// </summary>
    /// <param name="incidentVector">The vector representation of the completed Incident.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task UpsertAsync(
        HistoricalIncidentVector incidentVector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the vector representation of a completed Incident from the store.
    /// </summary>
    /// <param name="incidentId">The ID of the completed Incident.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DeleteAsync(
        string incidentId,
        CancellationToken cancellationToken = default);
}