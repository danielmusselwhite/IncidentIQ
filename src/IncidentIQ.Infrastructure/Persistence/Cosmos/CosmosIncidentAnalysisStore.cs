using IncidentIQ.Application.Analyse;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Incidents;
using IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Stores a completed incident and its generated analysis atomically in the same Cosmos logical partition.
/// </summary>
internal sealed class CosmosIncidentAnalysisStore : IIncidentAnalysisStore
{
    private readonly Container _container;

    public CosmosIncidentAnalysisStore(CosmosClient cosmosClient, IOptions<CosmosOptions> options)
    {
        var cosmosOptions = options.Value;
        _container = cosmosClient.GetContainer(cosmosOptions.DatabaseName, cosmosOptions.IncidentsContainerName);
    }

    /// <summary>
    /// Atomically persists the completed Incident document and its IncidentAnalysis document.
    /// </summary>
    public async Task StoreCompletedAnalysisAsync(Incident incident, IncidentAnalysisResult analysis, CancellationToken cancellationToken = default)
    {
        // Convert the domain incident and analysis result into their respective Cosmos DB documents.
        var incidentDocument = IncidentDocument.FromDomain(incident);
        var analysisDocument = IncidentAnalysisDocument.FromApplication(analysis, incident);

        // Both documents use incident.Id as /incidentId, so Cosmos can commit them in one transactional batch.
        var partitionKey = new PartitionKey(incident.Id);

        // in one transaction, replace the incident document and upsert the analysis document
        using var response = await _container
            .CreateTransactionalBatch(partitionKey)
            .ReplaceItem(incidentDocument.Id, incidentDocument)
            .UpsertItem(analysisDocument)
            .ExecuteAsync(cancellationToken);

        // Check if the transaction was successful; if not, throw an exception with details.
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to persist completed incident analysis. " +
                $"Status: {(int)response.StatusCode} ({response.StatusCode}). " +
                $"Error: {response.ErrorMessage}");
        }
    }
}
