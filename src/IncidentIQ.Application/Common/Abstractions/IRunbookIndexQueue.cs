using IncidentIQ.Application.Runbooks.Index;

namespace IncidentIQ.Application.Common.Abstractions;

/// <summary>
/// Enqueues a request for a Runbook to be indexed asynchronously.
///
/// The Application/Worker code depends on this abstraction rather than directly
/// depending on Azure Service Bus.
/// </summary>
public interface IRunbookIndexQueue
{
    /// <summary>
    /// Enqueues a request for a Runbook to be indexed asynchronously.
    /// </summary>
    /// <param name="command">The command containing the details of the Runbook to be indexed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task EnqueueAsync(
        IndexRunbookCommand command,
        CancellationToken cancellationToken = default);
}