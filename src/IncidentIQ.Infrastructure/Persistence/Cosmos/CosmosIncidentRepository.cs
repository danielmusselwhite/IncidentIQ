using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Incidents;
using IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Net;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Represents a repository for managing incidents in a Cosmos DB database.
/// </summary>
internal sealed class CosmosIncidentRepository : IIncidentRepository
{
    private readonly Container _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosIncidentRepository"/> class with the specified <see cref="CosmosClient"/> and <see cref="IOptions{CosmosOptions}"/>.
    /// </summary>
    /// <param name="client">The <see cref="CosmosClient"/> used to interact with Cosmos DB.</param>
    /// <param name="options">The <see cref="IOptions{CosmosOptions}"/> containing the Cosmos DB configuration.</param>
    public CosmosIncidentRepository(CosmosClient client, IOptions<CosmosOptions> options)
    {
        var cosmosOptions = options.Value;

        _container = client.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.IncidentsContainerName);
    }

    public async Task<Incident> CreateAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        var document = IncidentDocument.FromDomain(incident);

        var response = await _container.CreateItemAsync(
            document,
            new PartitionKey(document.Id),
            cancellationToken: cancellationToken);

        return response.Resource.ToDomain();
    }

    public async Task<Incident?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<IncidentDocument>(
                id,
                new PartitionKey(id),
                cancellationToken: cancellationToken);

            return response.Resource.ToDomain();
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyCollection<Incident>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = _container.GetItemQueryIterator<IncidentDocument>(
            new QueryDefinition("SELECT * FROM c ORDER BY c.CreatedAt DESC"));

        var incidents = new List<Incident>();

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync(cancellationToken);
            incidents.AddRange(response.Select(document => document.ToDomain()));
        }

        return incidents;
    }

    public async Task<Incident> UpdateAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        var document = IncidentDocument.FromDomain(incident);

        var response = await _container.ReplaceItemAsync(
            document,
            document.Id,
            new PartitionKey(document.Id),
            cancellationToken: cancellationToken);

        return response.Resource.ToDomain();
    }
}