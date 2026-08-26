using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using System.Collections.Concurrent;

namespace IncidentIQ.Api.Tests.Fakes;

/// <summary>
/// Fake of the <IIncidentAnalysisQueue> for testing purposes. It stores commands in memory for inspection.
/// Aka a fake of the AzureServiceBus IncidentAnalysis Queue.
/// </summary>
public sealed class InMemoryIncidentAnalysisQueue
    : IIncidentAnalysisQueue
{
    private readonly ConcurrentQueue<AnalyseIncidentCommand> _commands = new();

    public IReadOnlyCollection<AnalyseIncidentCommand> Commands =>
        _commands.ToArray();

    public Task EnqueueAsync(
        AnalyseIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        _commands.Enqueue(command);

        return Task.CompletedTask;
    }

    public void Clear()
    {
        while (_commands.TryDequeue(out _))
        {
        }
    }
}