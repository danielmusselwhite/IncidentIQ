using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Domain.Incidents;
using IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

internal sealed class CosmosIncidentSubmissionStore : IIncidentSubmissionStore
{
    private readonly Container _container;

    public CosmosIncidentSubmissionStore(CosmosClient client, IOptions<CosmosOptions> options)
    {
        var cosmosOptions = options.Value;
        _container = client.GetContainer(cosmosOptions.DatabaseName, cosmosOptions.IncidentsContainerName);
    }

    /// <summary>
    /// Persists a newly submitted incident along with the associated analysis command in a transactional manner.
    /// Done via outbox pattern (so we ensure atomicity between the incident and the analysis command).
    /// </summary>
    public async Task<Incident> CreateAsync(
        Incident incident,
        AnalyseIncidentCommand analyseIncidentCommand,
        CancellationToken cancellationToken = default)
    {
        // Convert the domain incident and analysis command into their respective Cosmos DB documents.
        var incidentDocument = IncidentDocument.FromDomain(incident);
        var outboxDocument = IncidentAnalysisOutboxDocument.FromCommand(analyseIncidentCommand);

        // Execute the transactional batch to persist both documents atomically.
        using var response = await _container
            .CreateTransactionalBatch(new PartitionKey(incident.Id)) // use the incident ID as the partition key for the transactional batch
            .CreateItem(incidentDocument)
            .CreateItem(outboxDocument)
            .ExecuteAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Failed to persist Incident and analysis outbox message. Cosmos returned {response.StatusCode}.");

        // Retrieve the persisted incident document from the transactional batch response (the first result)
        var incidentResult = response.GetOperationResultAtIndex<IncidentDocument>(0);

        // Convert back to the domain incident object.
        return incidentResult.Resource.ToDomain();
    }
}