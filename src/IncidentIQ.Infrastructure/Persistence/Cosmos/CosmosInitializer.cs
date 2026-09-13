using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Initializes the Cosmos DB database and containers if they do not already exist.
/// </summary>
public sealed class CosmosInitializer
{
    private const int EmbeddingDimensions = 1536;

    private readonly CosmosClient _client;
    private readonly CosmosOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosInitializer"/>.
    /// </summary>
    public CosmosInitializer(
        CosmosClient client,
        IOptions<CosmosOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    /// <summary>
    /// Initializes the Cosmos DB database and required application containers.
    /// </summary>
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(
            _options.DatabaseName,
            cancellationToken: cancellationToken);

        var database = databaseResponse.Database;

        await database.CreateContainerIfNotExistsAsync(
            _options.IncidentsContainerName,
            "/incidentId",
            cancellationToken: cancellationToken);

        await database.CreateContainerIfNotExistsAsync(
            CreateVectorContainerProperties(
                _options.HistoricalIncidentVectorsContainerName,
                "/incidentId"),
            cancellationToken: cancellationToken);

        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(
                _options.RunbooksContainerName,
                "/id"),
            cancellationToken: cancellationToken);

        await database.CreateContainerIfNotExistsAsync(
            CreateVectorContainerProperties(
                _options.RunbookChunksContainerName,
                "/runbookId"),
            cancellationToken: cancellationToken);

        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(
                _options.ChangeFeedLeasesContainerName,
                "/id"),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates Cosmos container properties for documents containing semantic
    /// embeddings used by vector search.
    /// </summary>
    private static ContainerProperties CreateVectorContainerProperties(
        string containerName,
        string partitionKeyPath)
    {
        var embeddings = new Collection<Embedding>
        {
            new()
            {
                Path = "/embedding",
                DataType = VectorDataType.Float32,
                DistanceFunction = DistanceFunction.Cosine,
                Dimensions = EmbeddingDimensions
            }
        };

        var properties = new ContainerProperties(
            containerName,
            partitionKeyPath)
        {
            VectorEmbeddingPolicy = new VectorEmbeddingPolicy(embeddings),
            IndexingPolicy = new IndexingPolicy
            {
                VectorIndexes = new Collection<VectorIndexPath>
                {
                    new()
                    {
                        Path = "/embedding",
                        Type = VectorIndexType.DiskANN // quantizedFlat in prod/live but the initializer is only used in local dev and only DiskANN is supported for local emulated cosmos
                    }
                }
            }
        };

        // Keep normal metadata indexed for filtering while excluding the large
        // embedding array from the ordinary Cosmos index.
        properties.IndexingPolicy.IncludedPaths.Add(
            new IncludedPath
            {
                Path = "/*"
            });

        properties.IndexingPolicy.ExcludedPaths.Add(
            new ExcludedPath
            {
                Path = "/embedding/*"
            });

        return properties;
    }
}