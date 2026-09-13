using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.HistoricalSearch.Index;
using IncidentIQ.Domain.Incidents;
using IncidentIQ.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IncidentIQ.Worker;

/// <summary>
/// Watches the Incidents Cosmos DB Change Feed and publishes indexing commands
/// when Incidents become eligible for historical semantic retrieval.
/// </summary>
public sealed class HistoricalIncidentIndexChangeFeedWorker : BackgroundService
{
    private const string ProcessorName = "historical-incident-index-relay";
    private const string IncidentDocumentType = "Incident";

    private readonly ChangeFeedProcessor _changeFeedProcessor;
    private readonly IHistoricalIncidentIndexQueue _historicalIncidentIndexQueue;
    private readonly ILogger<HistoricalIncidentIndexChangeFeedWorker> _logger;

    /// <summary>
    /// Creates the Change Feed Processor used to detect completed Incidents.
    /// </summary>
    public HistoricalIncidentIndexChangeFeedWorker(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options,
        IHistoricalIncidentIndexQueue historicalIncidentIndexQueue,
        ILogger<HistoricalIncidentIndexChangeFeedWorker> logger)
    {
        _historicalIncidentIndexQueue = historicalIncidentIndexQueue;
        _logger = logger;

        var cosmosOptions = options.Value;

        // Historical vectors are derived from the source Incident documents,
        // so the processor watches the Incidents container.
        var incidentsContainer = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.IncidentsContainerName);

        var leaseContainer = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.ChangeFeedLeasesContainerName);

        _changeFeedProcessor = incidentsContainer
            .GetChangeFeedProcessorBuilder<JsonElement>(
                ProcessorName,
                HandleChangesAsync)
            .WithInstanceName(Environment.MachineName)
            .WithLeaseContainer(leaseContainer)

            // Existing completed Incidents are eligible for indexing when this
            // processor is introduced for the first time.
            .WithStartTime(DateTime.MinValue.ToUniversalTime())

            .WithPollInterval(TimeSpan.FromSeconds(2))
            .Build();
    }

    /// <summary>
    /// Starts the historical Incident indexing Change Feed Processor and keeps
    /// it alive until the Worker host shuts down.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting historical Incident indexing Change Feed Processor.");

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
                "Stopping historical Incident indexing Change Feed Processor.");

            await _changeFeedProcessor.StopAsync();
        }
    }

    /// <summary>
    /// Publishes indexing commands for completed Incident documents observed
    /// in the Incidents Change Feed.
    /// </summary>
    private async Task HandleChangesAsync(
        ChangeFeedProcessorContext context,
        IReadOnlyCollection<JsonElement> changes,
        CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            // The Incidents container also contains outbox documents, and
            // Incidents pass through several states before becoming searchable.
            if (!IsCompletedIncidentDocument(change))
            {
                continue;
            }

            var incidentId = GetIncidentId(change);
            var commandId = Guid.NewGuid();

            var command = new IndexHistoricalIncidentCommand(
                CommandId: commandId,
                IncidentId: incidentId,
                CorrelationId: commandId.ToString(),
                QueuedAtUtc: DateTimeOffset.UtcNow);

            await _historicalIncidentIndexQueue.EnqueueAsync(
                command,
                cancellationToken);

            _logger.LogInformation(
                "Published historical Incident indexing command {CommandId} for Incident {IncidentId} from Change Feed lease {LeaseToken}.",
                command.CommandId,
                command.IncidentId,
                context.LeaseToken);
        }
    }

    /// <summary>
    /// Determines whether a changed Cosmos document represents a completed Incident.
    /// </summary>
    private static bool IsCompletedIncidentDocument(JsonElement document)
    {
        return document.TryGetProperty("documentType", out var documentType)
            && documentType.GetString() == IncidentDocumentType
            && document.TryGetProperty("status", out var status)
            && status.GetString() == nameof(IncidentStatus.Completed);
    }

    /// <summary>
    /// Reads the Incident ID from an Incident Cosmos document.
    /// </summary>
    private static string GetIncidentId(JsonElement document)
    {
        if (!document.TryGetProperty("incidentId", out var incidentIdProperty) ||
            string.IsNullOrWhiteSpace(incidentIdProperty.GetString()))
        {
            throw new InvalidOperationException(
                "Incident document does not contain a valid incidentId.");
        }

        return incidentIdProperty.GetString()!;
    }
}