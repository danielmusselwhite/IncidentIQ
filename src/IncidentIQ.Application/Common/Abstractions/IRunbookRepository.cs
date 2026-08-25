using IncidentIQ.Domain.Runbooks;

namespace IncidentIQ.Application.Common.Abstractions;

/// <summary>
/// Represents a repository for managing runbooks in the IncidentIQ application.
/// </summary>
public interface IRunbookRepository
{
    /// <summary>
    /// Creates a new runbook in the repository.
    /// </summary>
    /// <param name="runbook">The runbook to create.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous create operation.</returns>
    Task CreateAsync(
        Runbook runbook,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a runbook by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the runbook.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous get operation. The task result contains the runbook if found; otherwise, null.</returns>
    Task<Runbook?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all runbooks in the repository.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous get operation. The task result contains a read-only collection of runbooks.</returns>
    Task<IReadOnlyCollection<Runbook>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing runbook in the repository.
    /// </summary>
    /// <param name="runbook">The runbook to update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task UpdateAsync(
        Runbook runbook,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a runbook from the repository by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the runbook to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}