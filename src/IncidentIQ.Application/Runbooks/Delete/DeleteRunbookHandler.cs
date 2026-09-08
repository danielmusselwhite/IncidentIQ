using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;

namespace IncidentIQ.Application.Runbooks.Delete;

/// <summary>
/// Represents a handler for deleting a runbook.
/// </summary>
/// <param name="runbookRepository">The repository used to manage runbooks.</param>
public sealed class DeleteRunbookHandler(
    IRunbookRepository runbookRepository,
    IRunbookChunkStore runbookChunkStore)
{
    /// <summary>
    /// Handles the deletion of a runbook.
    /// </summary>
    /// <param name="id">The unique identifier of the runbook to be deleted.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var runbook = await runbookRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (runbook is null)
        {
            throw new RunbookNotFoundException(id);
        }

        // Remove the derived vector-search representation first. If this fails, the source Runbook remains and can be retried safely.
        await runbookChunkStore.ReplaceForRunbookAsync(
            id,
            [], // empty vectorised chunks, meaning it will effectively remove all existing chunks then replace them with nothing.
            cancellationToken);

        // Delete the runbook itself after successfully removing its derived representation.
        await runbookRepository.DeleteAsync(
            id,
            cancellationToken);
    }
}