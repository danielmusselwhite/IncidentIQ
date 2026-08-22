using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Common.Abstractions;

public interface IIncidentRepository
{
    Task<Incident> CreateAsync(Incident incident, CancellationToken cancellationToken = default);

    Task<Incident?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Incident>> GetAllAsync(CancellationToken cancellationToken = default);
}