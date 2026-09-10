using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Domain.Incidents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Incidents;

/// <summary>
/// Retrieves semantically similar completed historical Incidents from Cosmos DB
/// using vector search with optional metadata filtering.
/// </summary>
public sealed class CosmosHistoricalIncidentRetriever : IHistoricalIncidentRetriever
{
    private readonly Container _container;
    private readonly ILogger<CosmosHistoricalIncidentRetriever> _logger;

    private const float MinimumDistanceThreshold = 0.04f;

    public CosmosHistoricalIncidentRetriever(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options,
        ILogger<CosmosHistoricalIncidentRetriever> logger)
    {
        _container = cosmosClient.GetContainer(
            options.Value.DatabaseName,
            options.Value.HistoricalIncidentVectorsContainerName);

        _logger = logger;
    }

    public async Task<IReadOnlyList<HistoricalIncidentMatch>> RetrieveAsync(
        IReadOnlyList<float> queryEmbedding,
        string? service,
        string? environment,
        int topK,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);

        if (queryEmbedding.Count == 0)
            throw new ArgumentException("Query embedding cannot be empty.", nameof(queryEmbedding));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

        var stopwatch = Stopwatch.StartNew();
        var totalRequestCharge = 0d;

        var whereClause =
            "WHERE VectorDistance(c.embedding, @embedding) > @minimumDistanceThreshold";

        if (!string.IsNullOrWhiteSpace(service))
            whereClause += " AND c.service = @service";

        if (!string.IsNullOrWhiteSpace(environment))
            whereClause += " AND c.environment = @environment";

        var query = new QueryDefinition(
            $"""
            SELECT TOP @topK
                c.incidentId AS incidentId,
                c.title AS title,
                c.description AS description,
                c.symptoms AS symptoms,
                c.service AS service,
                c.environment AS environment,
                c.severity AS severity,
                c.completedAtUtc AS completedAtUtc,
                VectorDistance(c.embedding, @embedding) AS distance
            FROM c
            {whereClause}
            ORDER BY VectorDistance(c.embedding, @embedding)
            """)
            .WithParameter("@topK", topK)
            .WithParameter("@embedding", queryEmbedding.ToArray())
            .WithParameter("@minimumDistanceThreshold", MinimumDistanceThreshold);

        if (!string.IsNullOrWhiteSpace(service))
            query.WithParameter("@service", service);

        if (!string.IsNullOrWhiteSpace(environment))
            query.WithParameter("@environment", environment);

        using var iterator = _container.GetItemQueryIterator<HistoricalIncidentMatchResult>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = topK
            });

        var historicalIncidents = new List<HistoricalIncidentMatch>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);

            historicalIncidents.AddRange(response.Select(incident => new HistoricalIncidentMatch(
                IncidentId: Guid.Parse(incident.IncidentId),
                Title: incident.Title,
                Description: incident.Description,
                Symptoms: incident.Symptoms,
                Service: incident.Service,
                Environment: incident.Environment,
                Severity: Enum.TryParse<IncidentSeverity>(incident.Severity, out var severity) ? severity : throw new InvalidOperationException($"Invalid severity value: {incident.Severity}"),
                CompletedAtUtc: incident.CompletedAtUtc,
                Distance: incident.Distance)));

            totalRequestCharge += response.RequestCharge;
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Retrieved {ResultCount} historical Incidents using vector search in {DurationMs} ms. " +
            "Cosmos request charge: {RequestCharge} RU. TopK: {TopK}. Service: {Service}. " +
            "Environment: {Environment}. Minimum relevance threshold: {Threshold}.",
            historicalIncidents.Count,
            stopwatch.ElapsedMilliseconds,
            totalRequestCharge,
            topK,
            service ?? "all",
            environment ?? "all",
            MinimumDistanceThreshold);

        return historicalIncidents;
    }
}