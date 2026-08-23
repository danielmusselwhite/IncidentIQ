using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.GetById;

/// <summary>
/// Represents a handler for the <see cref="GetIncidentByIdHandler"/>.
/// (What the application does with the request.)
/// </summary>
/// <param name="incidentRepository">The repository used to retrieve incidents by ID.</param>
public sealed class GetIncidentByIdHandler(IIncidentRepository incidentRepository)
{
    /// <summary>
    /// Handles the retrieval of an incident by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the incident to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The incident with the specified ID.</returns>
    /// <exception cref="IncidentNotFoundException">Thrown if no incident with the specified ID is found.</exception>
    public async Task<Incident> HandleAsync(string id, CancellationToken cancellationToken = default)
    {
        var incident = await incidentRepository.GetByIdAsync(id, cancellationToken);

        return incident ?? throw new IncidentNotFoundException(id);
    }
}