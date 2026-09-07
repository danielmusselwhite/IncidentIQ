using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Initializes the Cosmos DB database and containers if they do not already exist.
/// </summary>
public sealed class CosmosInitializer
{
    private const int RunbookEmbeddingDimensions = 1536;

    private readonly CosmosClient _client;
    private readonly CosmosOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosInitializer"/> class with the specified <see cref="CosmosClient"/> and <see cref="IOptions{CosmosOptions}"/>.
    /// </summary>
    /// <param name="client">The <see cref="CosmosClient"/> used to interact with Cosmos DB.</param>
    /// <param name="options">The <see cref="IOptions{CosmosOptions}"/> containing the Cosmos DB configuration.</param>
    public CosmosInitializer(CosmosClient client, IOptions<CosmosOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    /// <summary>
    /// Initializes the Cosmos DB database and containers if they do not already exist.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(
            _options.DatabaseName,
            cancellationToken: cancellationToken);

        await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            _options.IncidentsContainerName,
            "/incidentId",
            cancellationToken: cancellationToken);

        await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(
                _options.RunbooksContainerName,
                "/runbookId"),
            cancellationToken: cancellationToken);

        await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(
                _options.RunbookChunksContainerName,
                "/runbookId"),
            cancellationToken: cancellationToken);

        await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(
                _options.ChangeFeedLeasesContainerName,
                "/id"),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates the same RunbookChunks vector policy locally that is provisioned
    /// through Bicep in Azure.
    /// </summary>
    private ContainerProperties CreateRunbookChunksContainerProperties()
    {
        var embeddings = new Collection<Embedding>
        {
            new()
            {
                Path = "/embedding",
                DataType = VectorDataType.Float32,
                DistanceFunction = DistanceFunction.Cosine,
                Dimensions = RunbookEmbeddingDimensions
            }
        };

        var properties = new ContainerProperties(
            _options.RunbookChunksContainerName,
            "/runbookId")
        {
            VectorEmbeddingPolicy = new VectorEmbeddingPolicy(embeddings),
            IndexingPolicy = new IndexingPolicy()
        };

        properties.IndexingPolicy.IncludedPaths.Add(
            new IncludedPath
            {
                Path = "/*"
            });

        // The specialised vector index handles /embedding.
        properties.IndexingPolicy.ExcludedPaths.Add(
            new ExcludedPath
            {
                Path = "/embedding/*"
            });

        properties.IndexingPolicy.VectorIndexes.Add(
            new VectorIndexPath
            {
                Path = "/embedding",
                Type = VectorIndexType.QuantizedFlat
            });

        return properties;
    }
}