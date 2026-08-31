using IncidentIQ.Application.Analyse;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Incidents;
using IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Stores the completed Incident state and generated analysis atomically in the same Cosmos logical partition.
/// </summary>
internal sealed class CosmosIncidentAnalysisStore : IIncidentAnalysisStore
{
    private readonly Container _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosIncidentAnalysisStore"/> class with the specified <see cref="CosmosClient"/> and <see cref="IOptions{CosmosOptions}"/>.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos client used to interact with the Cosmos DB service.</param>
    /// <param name="options">The options used to configure the Cosmos DB connection.</param>
    public CosmosIncidentAnalysisStore(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options)
    {
        var cosmosOptions = options.Value;

        _container = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.IncidentsContainerName);
    }
    
    /// <summary>
    /// Stores the completed incident and its analysis atomically in the Cosmos DB container.
    /// </summary>
    /// <param name="incident">The incident to store.</param>
    /// <param name="analysis">The analysis result to store.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    public async Task StoreCompletedAnalysisAsync(
        Incident incident,
        IncidentAnalysisResult analysis,
        CancellationToken cancellationToken = default)
    {
        // Convert the domain incident and analysis result into their respective Cosmos DB documents.
        var incidentDocument = IncidentDocument.FromDomain(incident);
        var analysisDocument = IncidentAnalysisDocument.FromDomain(analysis, incident);

        // Both documents use incident.Id as /incidentId, so Cosmos can commit both operations atomically.
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
