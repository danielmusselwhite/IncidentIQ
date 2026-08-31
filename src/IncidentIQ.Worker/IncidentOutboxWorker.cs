using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Infrastructure.Persistence.Cosmos;
using IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IncidentIQ.Worker;

/// <summary>
/// Relays persisted incident-analysis outbox messages from the Cosmos DB Change Feed to Azure Service Bus.
/// </summary>
public sealed class IncidentOutboxWorker : BackgroundService
{
    private const string ProcessorName = "incident-analysis-outbox-relay";
    private const string OutboxDocumentType = "AnalyseIncidentOutbox";

    private readonly ChangeFeedProcessor _changeFeedProcessor;
    private readonly IIncidentAnalysisQueue _incidentAnalysisQueue;
    private readonly ILogger<IncidentOutboxWorker> _logger;

    /// <summary>
    /// Initializes the Cosmos Change Feed Processor used to relay analysis commands.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos client.</param>
    /// <param name="options">Cosmos configuration containing the monitored and lease containers.</param>
    /// <param name="incidentAnalysisQueue">Queue used to publish AnalyseIncident commands.</param>
    /// <param name="logger">Logger used for relay diagnostics.</param>
    public IncidentOutboxWorker(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options,
        IIncidentAnalysisQueue incidentAnalysisQueue,
        ILogger<IncidentOutboxWorker> logger)
    {
        _incidentAnalysisQueue = incidentAnalysisQueue;
        _logger = logger;

        var cosmosOptions = options.Value;

        // get the cosmos containers for incidents and leases (leasing being the mechanism to track change feed progress)
        var incidentsContainer = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.IncidentsContainerName);
        var leaseContainer = cosmosClient.GetContainer(
            cosmosOptions.DatabaseName,
            cosmosOptions.ChangeFeedLeasesContainerName);

        // !IMPORTANT - The Change Feed Processor must be built with the lease container to track progress correctly.
        // ! this is in charge of relaying changes from the incidents container to the Service Bus queue.
        _changeFeedProcessor = incidentsContainer
            .GetChangeFeedProcessorBuilder<JsonElement>(ProcessorName, HandleChangesAsync)
            .WithInstanceName(Environment.MachineName)
            .WithLeaseContainer(leaseContainer) // !IMPORTANT - The lease container is used to track the progress of the Change Feed Processor across multiple instances. In outbox pattern this allows us to determine which changes have already been processed and which are new.
            .WithStartTime(DateTime.MinValue.ToUniversalTime())
            .WithPollInterval(TimeSpan.FromSeconds(2))
            .Build();
    }

    /// <summary>
    /// Starts the Change Feed Processor and keeps it running until the Worker shuts down.
    /// It works by continuously monitoring the Incidents container for changes and invoking the HandleChangesAsync method for each batch of changes.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token triggered when the Worker is stopping.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Incident outbox Change Feed Processor.");

        // start the Change Feed Processor to begin listening for changes in the Incidents container
        await _changeFeedProcessor.StartAsync();

        // keep the Worker running indefinitely until a cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Cancellation is expected during normal Worker shutdown.
        }
        finally
        {
            _logger.LogInformation("Stopping Incident outbox Change Feed Processor.");
            await _changeFeedProcessor.StopAsync();
        }
    }

    /// <summary>
    /// Processes changes observed in the Incidents container and publishes persisted outbox commands to Service Bus.
    /// </summary>
    /// <param name="context">The Change Feed Processor context.</param>
    /// <param name="changes">The changed Cosmos documents.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task HandleChangesAsync(
        ChangeFeedProcessorContext context,
        IReadOnlyCollection<JsonElement> changes,
        CancellationToken cancellationToken)
    {
        // iterate through each change in the batch and process it if it is an IncidentAnalysisOutboxDocument from the outbox
        foreach (var change in changes)
        {
            // skip any documents that are not IncidentAnalysisOutboxDocuments
            if (!IsIncidentAnalysisOutboxDocument(change))
            {
                continue;
            }

            // deserialize the outbox document from the raw JSON text
            var outboxDocument = JsonSerializer.Deserialize<IncidentAnalysisOutboxDocument>(
                change.GetRawText());
            if (outboxDocument is null) throw new InvalidOperationException("Incident analysis outbox document could not be deserialized.");

            // convert to an AnalyseIncidentCommand and enqueue it (on azure service bus) for processing
            var command = outboxDocument.ToCommand();
            await _incidentAnalysisQueue.EnqueueAsync(command, cancellationToken);

            _logger.LogInformation(
                "Published outbox command {CommandId} for Incident {IncidentId} from Change Feed lease {LeaseToken}.",
                command.CommandId,
                command.IncidentId,
                context.LeaseToken);
        }
    }

    private static bool IsIncidentAnalysisOutboxDocument(JsonElement document)
    {
        return document.TryGetProperty("documentType", out var documentType)
            && documentType.GetString() == OutboxDocumentType;
    }
}