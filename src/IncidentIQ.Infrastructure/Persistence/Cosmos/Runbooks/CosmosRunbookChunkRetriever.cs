using IncidentIQ.Application.Runbooks.RetrieveChunks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Runbooks
{
    /// <summary>
    /// Retrieves semantically similar Runbook chunks from Cosmos DB using vector search.
    /// </summary>
    public class CosmosRunbookChunkRetriever : IRunbookChunkRetriever
    {

        private readonly Container _container;
        private readonly ILogger<CosmosRunbookChunkRetriever> _logger;

        public CosmosRunbookChunkRetriever(
            CosmosClient cosmosClient,
            IOptions<CosmosOptions> options,
            ILogger<CosmosRunbookChunkRetriever> logger)
        {
            _logger = logger;
            var cosmosOptions = options.Value;

            _container = cosmosClient.GetContainer(
                cosmosOptions.DatabaseName,
                cosmosOptions.RunbookChunksContainerName);
        }


        public async Task<IReadOnlyList<RunbookChunkMatch>> RetrieveAsync(IReadOnlyList<float> queryEmbedding, string? service, int topK, CancellationToken cancellationToken = default)
        {
            // Validation
            ArgumentNullException.ThrowIfNull(queryEmbedding);
            if (queryEmbedding.Count == 0) throw new ArgumentException("Query embedding cannot be empty.", nameof(queryEmbedding));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

            // Begin measuring
            var stopwatch = Stopwatch.StartNew();
            var totalRequestCharge = 0d;

            // Construct the SQL query to retrieve the top K Runbook chunks based on vector similarity
            var whereClause = !string.IsNullOrEmpty(service) ? "WHERE c.service = @service" : string.Empty;

            var query = new QueryDefinition(
                $"""
                SELECT TOP @topK
                    c.runbookId AS runbookId,
                    c.chunkIndex AS chunkIndex,
                    c.title AS title,
                    c.service AS service,
                    c.content AS content,
                    VectorDistance(c.embedding, @embedding) AS distance
                FROM c
                {whereClause}
                ORDER BY VectorDistance(c.embedding, @embedding)
                """)
                .WithParameter("@topK", topK)
                .WithParameter("@embedding", queryEmbedding.ToArray()); // safer as we want to ensure the Cosmos SDK receives the vector as an ordinary numeric array

            if (!string.IsNullOrWhiteSpace(service))
                query.WithParameter("@service", service);

            // Create an iterator to execute the query and retrieve the results as CosmosRunbookChunkMatchResult objects
            using var iterator = _container.GetItemQueryIterator<CosmosRunbookChunkMatchResult>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    MaxItemCount = topK
                }
            );

            //  Finally, read the results from the iterator and return them as a list of RunbookChunkMatch objects
            var chunks = new List<RunbookChunkMatch>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                chunks.AddRange(response.Select(chunk => new RunbookChunkMatch
                (
                    Guid.Parse(chunk.RunbookId),
                    chunk.ChunkIndex,
                    chunk.Title,
                    chunk.Service,
                    chunk.Content,
                    chunk.Distance
                )));
                totalRequestCharge += response.RequestCharge; // track how many request units were used
            }

            // end monitoring
            stopwatch.Stop();
            // Log the request
            _logger.LogInformation(
                "Retrieved {ResultCount} Runbook chunks using vector search in {DurationMs} ms. " +
                "Cosmos request charge: {RequestCharge} RU. TopK: {TopK}. Service: {Service}.",
                chunks.Count,
                stopwatch.ElapsedMilliseconds, // latency
                totalRequestCharge, // cosmos request units consumed
                topK,
                service ?? "all");

            return chunks;

        }
    }
}
