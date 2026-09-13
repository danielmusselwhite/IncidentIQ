using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Domain.Incidents;
using IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Incidents;

/// <summary>
/// Stores a completed Incident, its generated analysis and the grounding evidence
/// used to produce that analysis atomically in the same Cosmos logical partition.
/// </summary>
internal sealed class CosmosIncidentAnalysisStore : IIncidentAnalysisStore
{
    private readonly Container _container;

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
    /// Atomically persists the completed Incident together with its generated
    /// analysis and the exact grounding evidence supplied during analysis.
    /// </summary>
    public async Task StoreCompletedAnalysisAsync(
        Incident incident,
        IncidentAnalysisResult analysis,
        IncidentAnalysisEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(evidence);

        var incidentDocument = IncidentDocument.FromDomain(incident);

        var analysisDocument = IncidentAnalysisDocument.FromApplication(
            analysis,
            incident,
            evidence);

        // Both documents use the Incident ID as /incidentId, allowing the
        // completed Incident and its analysis to be committed atomically.
        var partitionKey = new PartitionKey(incident.Id);

        using var response = await _container
            .CreateTransactionalBatch(partitionKey)
            .ReplaceItem(incidentDocument.Id, incidentDocument)
            .UpsertItem(analysisDocument)
            .ExecuteAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to persist completed Incident analysis. " +
                $"Status: {(int)response.StatusCode} ({response.StatusCode}). " +
                $"Error: {response.ErrorMessage}");
        }
    }
}