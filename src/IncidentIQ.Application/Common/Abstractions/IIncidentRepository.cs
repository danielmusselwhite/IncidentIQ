using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Common.Abstractions;

/// <summary>
/// Defines the contract for incident repository operations.
/// </summary>
public interface IIncidentRepository
{
    /// <summary>
    /// Creates a new incident in the repository.
    /// </summary>
    /// <param name="incident">The incident to create.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The created <see cref="Incident"/>.</returns>
    Task<Incident> CreateAsync(Incident incident, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an incident by its ID from the repository.
    /// </summary>
    /// <param name="id">The ID of the incident to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The <see cref="Incident"/> with the specified ID, or <c>null</c> if not found.</returns>
    Task<Incident?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all incidents from the repository
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A read-only collection of <see cref="Incident"/>.</returns>
    Task<IReadOnlyCollection<Incident>> GetAllAsync(CancellationToken cancellationToken = default);
}