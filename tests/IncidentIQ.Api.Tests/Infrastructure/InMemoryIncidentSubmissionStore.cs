using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Domain.Incidents;
using System.Collections.Concurrent;

namespace IncidentIQ.Api.Tests.Infrastructure;

public sealed class InMemoryIncidentSubmissionStore : IIncidentSubmissionStore
{
    private readonly InMemoryIncidentRepository _incidentRepository;
    private readonly ConcurrentQueue<AnalyseIncidentCommand> _commands = new();

    public IReadOnlyCollection<AnalyseIncidentCommand> Commands => _commands.ToArray();

    public InMemoryIncidentSubmissionStore(InMemoryIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Incident> CreateAsync(
        Incident incident,
        AnalyseIncidentCommand analyseIncidentCommand,
        CancellationToken cancellationToken = default)
    {
        var createdIncident = await _incidentRepository.CreateAsync(incident, cancellationToken);
        _commands.Enqueue(analyseIncidentCommand);

        return createdIncident;
    }

    public async Task<Incident> RetryAsync(
        Incident incident,
        AnalyseIncidentCommand analyseIncidentCommand,
        CancellationToken cancellationToken = default)
    {
        var createdIncident = await _incidentRepository.UpdateAsync(incident, cancellationToken);
        _commands.Enqueue(analyseIncidentCommand);

        return createdIncident;
    }

    public void Clear()
    {
        while (_commands.TryDequeue(out _))
        {
        }
    }
}