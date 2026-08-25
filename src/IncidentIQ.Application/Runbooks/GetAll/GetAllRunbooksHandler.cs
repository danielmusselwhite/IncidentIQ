using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Runbooks;

namespace IncidentIQ.Application.Runbooks.GetAll;

/// <summary>
/// Represents a handler for retrieving all runbooks.
/// </summary>
/// <param name="runbookRepository">The repository used to manage runbooks.</param>
public sealed class GetAllRunbooksHandler(
    IRunbookRepository runbookRepository)
{
    /// <summary>
    /// Handles the retrieval of all runbooks.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only collection of all <see cref="Runbook"/> instances.</returns>
    public Task<IReadOnlyCollection<Runbook>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        return runbookRepository.GetAllAsync(cancellationToken);
    }
}