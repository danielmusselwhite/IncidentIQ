using IncidentIQ.Application.Runbooks.Index;

namespace IncidentIQ.Application.Common.Abstractions;


/// <summary>
/// Persists the current vectorised chunks derived from a Runbook.
///
/// Implementations must replace the previously indexed chunks for the Runbook
/// rather than continually appending new versions.
/// </summary>
public interface IRunbookChunkStore
{
    /// <summary>
    /// Persists the current vectorised chunks derived from a Runbook.
    /// </summary>
    /// <param name="runbookId">The unique identifier of the Runbook.</param>
    /// <param name="chunks">The collection of vectorised chunks to persist.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ReplaceForRunbookAsync(
        Guid runbookId,
        IReadOnlyCollection<RunbookChunk> chunks,
        CancellationToken cancellationToken = default);
}