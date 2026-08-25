using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Domain.Runbooks;

namespace IncidentIQ.Application.Runbooks.GetById;

/// <summary>
/// Represents a handler for retrieving a runbook by its unique identifier.
/// </summary>
/// <param name="runbookRepository">The repository used to manage runbooks.s</param>
public sealed class GetRunbookByIdHandler(
    IRunbookRepository runbookRepository)
{
    /// <summary>
    /// Handles the retrieval of a runbook by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the runbook to be retrieved.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The retrieved <see cref="Runbook"/>.</returns>
    /// <exception cref="RunbookNotFoundException">Thrown when a runbook with the specified ID does not exist.</exception>
    public async Task<Runbook> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var runbook = await runbookRepository.GetByIdAsync(
            id,
            cancellationToken);

        return runbook
            ?? throw new RunbookNotFoundException(id);
    }
}