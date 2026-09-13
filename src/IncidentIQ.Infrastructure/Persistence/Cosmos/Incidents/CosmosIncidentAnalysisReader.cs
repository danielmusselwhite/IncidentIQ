using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Net;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Incidents;

/// <summary>
/// Reads persisted incident analysis results from Cosmos DB.
/// </summary>
internal sealed class CosmosIncidentAnalysisReader : IIncidentAnalysisReader
{
    private readonly Container _container;

    public CosmosIncidentAnalysisReader(CosmosClient cosmosClient, IOptions<CosmosOptions> options)
    {
        var cosmosOptions = options.Value;
        _container = cosmosClient.GetContainer(cosmosOptions.DatabaseName, cosmosOptions.IncidentsContainerName);
    }

    /// <summary>
    /// Retrieves an analysis using a Cosmos point read.
    /// </summary>
    /// <remarks>
    /// The analysis document ID is "analysis-{incidentId}", but the Incidents container
    /// is partitioned by /incidentId, so the partition key is the raw incident ID.
    /// Knowing both values allows a cheap point read instead of a Cosmos SQL query.
    /// </remarks>
    public async Task<GroundedIncidentAnalysis?> GetByIncidentIdAsync(string incidentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var analysisDocumentId = IncidentAnalysisDocument.CreateId(incidentId);

            var response = await _container.ReadItemAsync<IncidentAnalysisDocument>(
                analysisDocumentId, // the ID of the analysis document to read
                new PartitionKey(incidentId), // the partition key for the analysis document (based on the Incident that it belongs to)
                cancellationToken: cancellationToken);

            return response.Resource.ToApplication(); // Convert the persisted Cosmos document into the complete grounded Application read model, including the analysis and its evidence snapshot.
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
