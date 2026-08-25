using System.Net;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Runbooks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Represents a repository for managing runbooks in Cosmos DB.
/// </summary>
internal sealed class CosmosRunbookRepository : IRunbookRepository
{
    private readonly Container _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosRunbookRepository"/> class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    /// <param name="options">The Cosmos DB options.</param>
    public CosmosRunbookRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options)
    {
        var cosmosOptions = options.Value;

        _container = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.RunbooksContainerName);
    }

    public async Task CreateAsync(
        Runbook runbook,
        CancellationToken cancellationToken = default)
    {
        var document = RunbookDocument.FromDomain(runbook);

        await _container.CreateItemAsync(
            document,
            new PartitionKey(document.Id),
            cancellationToken: cancellationToken);
    }

    public async Task<Runbook?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var stringId = id.ToString();

        try
        {
            var response = await _container.ReadItemAsync<RunbookDocument>(
                stringId,
                new PartitionKey(stringId),
                cancellationToken: cancellationToken);

            return response.Resource.ToDomain();
        }
        catch (CosmosException exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyCollection<Runbook>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c ORDER BY c.updatedAt DESC");

        using var iterator =
            _container.GetItemQueryIterator<RunbookDocument>(query);

        var runbooks = new List<Runbook>();

        while (iterator.HasMoreResults)
        {
            var response =
                await iterator.ReadNextAsync(cancellationToken);

            runbooks.AddRange(
                response.Select(document => document.ToDomain()));
        }

        return runbooks;
    }

    public async Task UpdateAsync(
        Runbook runbook,
        CancellationToken cancellationToken = default)
    {
        var document = RunbookDocument.FromDomain(runbook);

        await _container.ReplaceItemAsync(
            document,
            document.Id,
            new PartitionKey(document.Id),
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var stringId = id.ToString();

        await _container.DeleteItemAsync<RunbookDocument>(
            stringId,
            new PartitionKey(stringId),
            cancellationToken: cancellationToken);
    }
}