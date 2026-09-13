using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.HistoricalSearch;
using IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Net;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Incidents;

/// <summary>
/// Persists the derived vector representation of completed historical Incidents
/// used for semantic similarity search.
/// </summary>
public sealed class CosmosHistoricalIncidentVectorStore
    : IHistoricalIncidentVectorStore
{
    private readonly Container _container;

    public CosmosHistoricalIncidentVectorStore(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options)
    {
        var cosmosOptions = options.Value;

        _container = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.HistoricalIncidentVectorsContainerName);
    }

    public async Task UpsertAsync(
        HistoricalIncidentVector incidentVector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incidentVector);

        var document =
            HistoricalIncidentVectorDocument.FromApplication(incidentVector);

        await _container.UpsertItemAsync(
            document,
            new PartitionKey(incidentVector.IncidentId),
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        string incidentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(incidentId);

        try
        {
            await _container.DeleteItemAsync<HistoricalIncidentVectorDocument>(
                incidentId,
                new PartitionKey(incidentId),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            // The derived vector may already be absent. Deletion is idempotent.
        }
    }
}