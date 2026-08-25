using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;

namespace IncidentIQ.Application.Runbooks.Delete;

/// <summary>
/// Represents a handler for deleting a runbook.
/// </summary>
/// <param name="runbookRepository">The repository used to manage runbooks.</param>
public sealed class DeleteRunbookHandler(
    IRunbookRepository runbookRepository)
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

        await runbookRepository.DeleteAsync(
            id,
            cancellationToken);
    }
}