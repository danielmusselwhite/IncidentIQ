using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Stores the vectorised representation of Runbooks in Cosmos DB.
/// </summary>
internal sealed class CosmosRunbookChunkStore : IRunbookChunkStore
{
    // Cosmos transactional batches currently support at most 100 operations.
    private const int MaximumTransactionalBatchOperations = 100;

    private readonly Container _container;

    public CosmosRunbookChunkStore(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options)
    {
        var cosmosOptions = options.Value;

        _container = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.RunbookChunksContainerName);
    }

    /// <summary>
    /// Replaces the current indexed chunks for a Runbook.
    /// </summary>
    public async Task ReplaceForRunbookAsync(
        Guid runbookId,
        IReadOnlyCollection<RunbookChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        // Prevent an accidental call from mixing chunks belonging to different
        // logical partitions into one replacement operation.
        if (chunks.Any(chunk => chunk.RunbookId != runbookId))
        {
            throw new ArgumentException(
                "All Runbook chunks must belong to the Runbook being replaced.",
                nameof(chunks));
        }

        var partitionKeyValue = runbookId.ToString();
        var partitionKey = new PartitionKey(partitionKeyValue);

        var documents = chunks
            .Select(RunbookChunkDocument.FromApplication)
            .ToList();

        var currentIds = documents
            .Select(document => document.Id)
            .ToHashSet(StringComparer.Ordinal);

        // Find any documents from the previous index that no longer exist in
        // the newly generated set.
        var existingIds = await GetExistingDocumentIdsAsync(
            partitionKeyValue,
            cancellationToken);

        var staleIds = existingIds
            .Where(id => !currentIds.Contains(id))
            .ToList();

        var operationCount = staleIds.Count + documents.Count;

        if (operationCount > MaximumTransactionalBatchOperations)
        {
            throw new InvalidOperationException(
                $"Replacing Runbook chunks requires {operationCount} Cosmos transactional batch operations, " +
                $"which exceeds the supported maximum of {MaximumTransactionalBatchOperations}.");
        }

        if (operationCount == 0)
        {
            return;
        }

        // All documents share /runbookId, allowing the deletes and upserts to
        // commit atomically in one logical partition.
        var batch = _container.CreateTransactionalBatch(partitionKey);

        foreach (var staleId in staleIds)
        {
            batch.DeleteItem(staleId);
        }

        foreach (var document in documents)
        {
            batch.UpsertItem(document);
        }

        using var response = await batch.ExecuteAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to replace Runbook chunks in Cosmos DB. " +
                $"Status: {response.StatusCode}. Error: {response.ErrorMessage}");
        }
    }

    /// <summary>
    /// Retrieves only the IDs of existing chunks for a Runbook so stale
    /// documents can be removed during re-indexing.
    /// </summary>
    private async Task<IReadOnlyCollection<string>> GetExistingDocumentIdsAsync(
        string runbookId,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
                "SELECT VALUE c.id FROM c WHERE c.runbookId = @runbookId")
            .WithParameter("@runbookId", runbookId);

        var requestOptions = new QueryRequestOptions
        {
            // Restrict the query to the Runbook's logical partition.
            PartitionKey = new PartitionKey(runbookId)
        };

        using var iterator = _container.GetItemQueryIterator<string>(
            query,
            requestOptions: requestOptions);

        var ids = new List<string>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            ids.AddRange(response);
        }

        return ids;
    }
}