using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IncidentIQ.Worker;

/// <summary>
/// Watches the Runbooks Cosmos DB Change Feed and publishes indexing commands
/// whenever a Runbook is created or updated.
/// </summary>
public sealed class RunbookIndexChangeFeedWorker : BackgroundService
{
    private const string ProcessorName = "runbook-index-relay";

    private readonly ChangeFeedProcessor _changeFeedProcessor;
    private readonly IRunbookIndexQueue _runbookIndexQueue;
    private readonly ILogger<RunbookIndexChangeFeedWorker> _logger;

    /// <summary>
    /// Creates the Change Feed Processor used to detect Runbook changes.
    /// </summary>
    public RunbookIndexChangeFeedWorker(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options,
        IRunbookIndexQueue runbookIndexQueue,
        ILogger<RunbookIndexChangeFeedWorker> logger)
    {
        _runbookIndexQueue = runbookIndexQueue;
        _logger = logger;

        var cosmosOptions = options.Value;

        var runbooksContainer = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.RunbooksContainerName);

        var leaseContainer = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.ChangeFeedLeasesContainerName);

        _changeFeedProcessor = runbooksContainer
            .GetChangeFeedProcessorBuilder<JsonElement>(
                ProcessorName,
                HandleChangesAsync)
            .WithInstanceName(Environment.MachineName)
            .WithLeaseContainer(leaseContainer)

            // Starting from the beginning means existing Runbooks are also
            // indexed when this processor is introduced for the first time.
            .WithStartTime(DateTime.MinValue.ToUniversalTime())

            .WithPollInterval(TimeSpan.FromSeconds(2))
            .Build();
    }

    /// <summary>
    /// Starts the Runbook Change Feed Processor and keeps it alive until the
    /// Worker host is shutting down.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting Runbook indexing Change Feed Processor.");

        await _changeFeedProcessor.StartAsync();

        try
        {
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Expected during normal Worker shutdown.
        }
        finally
        {
            _logger.LogInformation(
                "Stopping Runbook indexing Change Feed Processor.");

            await _changeFeedProcessor.StopAsync();
        }
    }

    /// <summary>
    /// Converts changed Runbook documents into asynchronous indexing commands.
    /// </summary>
    private async Task HandleChangesAsync(
        ChangeFeedProcessorContext context,
        IReadOnlyCollection<JsonElement> changes,
        CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            var runbookId = GetRunbookId(change);
            var sourceUpdatedAtUtc = GetSourceUpdatedAtUtc(change);

            var commandId = Guid.NewGuid();

            var command = new IndexRunbookCommand(
                CommandId: commandId,
                RunbookId: runbookId,
                CorrelationId: commandId.ToString(),
                QueuedAtUtc: DateTime.UtcNow,
                SourceUpdatedAtUtc: sourceUpdatedAtUtc);

            await _runbookIndexQueue.EnqueueAsync(
                command,
                cancellationToken);

            _logger.LogInformation(
                "Published Runbook indexing command {CommandId} for Runbook {RunbookId} from Change Feed lease {LeaseToken}.",
                command.CommandId,
                command.RunbookId,
                context.LeaseToken);
        }
    }

    /// <summary>
    /// Reads and validates the Runbook ID from the Cosmos document.
    /// </summary>
    private static Guid GetRunbookId(JsonElement document)
    {
        if (!document.TryGetProperty("id", out var idProperty) ||
            !Guid.TryParse(idProperty.GetString(), out var runbookId))
        {
            throw new InvalidOperationException(
                "Changed Runbook document does not contain a valid id.");
        }

        return runbookId;
    }

    /// <summary>
    /// Reads the source Runbook revision timestamp used to identify the
    /// specific version that triggered indexing.
    /// </summary>
    private static DateTime GetSourceUpdatedAtUtc(JsonElement document)
    {
        if (!document.TryGetProperty("updatedAt", out var updatedAtProperty) ||
            !updatedAtProperty.TryGetDateTime(out var updatedAt))
        {
            throw new InvalidOperationException(
                "Changed Runbook document does not contain a valid updatedAt timestamp.");
        }

        return updatedAt.ToUniversalTime();
    }
}