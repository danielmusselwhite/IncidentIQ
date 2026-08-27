using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Initializes the Cosmos DB database and containers if they do not already exist.
/// </summary>
public sealed class CosmosInitializer
{
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
            "/id",
            cancellationToken: cancellationToken);

        await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(
                _options.RunbooksContainerName,
                "/id"),
            cancellationToken: cancellationToken);
    }
}