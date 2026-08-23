using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.GetAll;

/// <summary>
/// Represents a handler for the <see cref="GetAllIncidentsHandler"/>.
/// (What the application does with the request.)
/// </summary>
/// <param name="incidentRepository">The repository used to retrieve incidents from.</param>
public sealed class GetAllIncidentsHandler(IIncidentRepository incidentRepository)
{
    /// <summary>
    /// Handles the retrieval of all incidents
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>All incidents.</returns>
    public async Task<IReadOnlyCollection<Incident>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await incidentRepository.GetAllAsync(cancellationToken);
    }
}