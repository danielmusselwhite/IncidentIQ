using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Evaluation.Retrieval;

public sealed class InMemoryHistoricalIncidentRetriever(
    EvaluationCorpusIndex index)
    : IHistoricalIncidentRetriever
{
    public Task<IReadOnlyList<HistoricalIncidentMatch>> RetrieveAsync(
        IReadOnlyList<float> queryEmbedding,
        string? service,
        string? environment,
        int topK,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

        cancellationToken.ThrowIfCancellationRequested();

        var matches =
            index.HistoricalIncidents
                .Where(incident =>
                    string.IsNullOrWhiteSpace(service) ||
                    string.Equals(
                        incident.Service,
                        service,
                        StringComparison.OrdinalIgnoreCase))
                .Where(incident =>
                    string.IsNullOrWhiteSpace(environment) ||
                    string.Equals(
                        incident.Environment,
                        environment,
                        StringComparison.OrdinalIgnoreCase))
                .Select(incident =>
                    new HistoricalIncidentMatch(
                        IncidentId:
                            Guid.Parse(incident.IncidentId),
                        Title:
                            incident.Title,
                        Description:
                            incident.Description,
                        Symptoms:
                            incident.Symptoms,
                        Service:
                            incident.Service,
                        Environment:
                            incident.Environment,
                        Severity:
                            Enum.Parse<IncidentSeverity>(
                                incident.Severity),
                        CompletedAtUtc:
                            incident.CompletedAtUtc,
                        Distance:
                            CosineDistance.Calculate(
                                queryEmbedding,
                                incident.Embedding)))
                .OrderBy(match => match.Distance)
                .Take(topK)
                .ToList();

        return Task.FromResult<
            IReadOnlyList<HistoricalIncidentMatch>>(
                matches);
    }
}