using IncidentIQ.Application.Incidents.HistoricalSearch.Index;

namespace IncidentIQ.Application.Common.Abstractions;

/// <summary>
/// Publishes commands requesting semantic indexing of completed Incidents.
/// </summary>
public interface IHistoricalIncidentIndexQueue
{
    /// <summary>
    /// Enqueues a command to request semantic indexing of a completed Incident.
    /// </summary>
    /// <param name="command">The command requesting semantic indexing of a completed Incident.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task EnqueueAsync(
        IndexHistoricalIncidentCommand command,
        CancellationToken cancellationToken = default);
}