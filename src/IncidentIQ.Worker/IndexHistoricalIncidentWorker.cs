using Azure.Messaging.ServiceBus;
using IncidentIQ.Application.Incidents.HistoricalSearch.Index;
using IncidentIQ.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IncidentIQ.Worker;

/// <summary>
/// Background worker responsible for consuming
/// <see cref="IndexHistoricalIncidentCommand"/> messages from the Service Bus
/// historical Incident indexing queue.
///
/// The worker acts as the transport boundary between Azure Service Bus and the
/// Application layer. Each message is processed inside its own dependency
/// injection scope before being explicitly completed or dead-lettered.
/// </summary>
public sealed class IndexHistoricalIncidentWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IndexHistoricalIncidentWorker> _logger;
    private readonly int _maxDeliveryCount;

    /// <summary>
    /// Creates the Service Bus processor used to consume historical Incident
    /// indexing commands.
    /// </summary>
    public IndexHistoricalIncidentWorker(
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        IOptions<ServiceBusOptions> options,
        ILogger<IndexHistoricalIncidentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var serviceBusOptions = options.Value;

        _maxDeliveryCount = serviceBusOptions.MaxDeliveryCount;

        _processor = serviceBusClient.CreateProcessor(
            serviceBusOptions.IndexHistoricalIncidentQueueName,
            new ServiceBusProcessorOptions
            {
                // Only complete the message once historical indexing succeeds.
                AutoCompleteMessages = false,

                // Keep indexing sequential while the workflow is being
                // developed and verified.
                MaxConcurrentCalls = 1,

                // Embedding generation may involve an external Azure OpenAI
                // request, so allow the SDK to renew the message lock.
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    /// <summary>
    /// Starts the Service Bus processor and keeps the hosted service alive until
    /// the Worker host requests shutdown.
    /// </summary>
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting historical Incident indexing Service Bus processor.");

        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            // Service Bus invokes the registered message callback, so this
            // BackgroundService only needs to remain alive.
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Expected during normal Worker shutdown.
        }
    }

    /// <summary>
    /// Stops message processing and disposes the Service Bus processor.
    /// </summary>
    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping historical Incident indexing Service Bus processor.");

        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Handles a single Service Bus message containing an
    /// <see cref="IndexHistoricalIncidentCommand"/>.
    /// </summary>
    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args)
    {
        IndexHistoricalIncidentCommand? command;

        try
        {
            command = JsonSerializer.Deserialize<IndexHistoricalIncidentCommand>(
                args.Message.Body);
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception,
                "Unable to deserialize historical Incident indexing message {MessageId}.",
                args.Message.MessageId);

            // Retrying malformed JSON will never make it valid.
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidMessage",
                "Message could not be deserialized as IndexHistoricalIncidentCommand.",
                args.CancellationToken);

            return;
        }

        if (command is null)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidMessage",
                "IndexHistoricalIncidentCommand was null.",
                args.CancellationToken);

            return;
        }

        using var loggingScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["CorrelationId"] = command.CorrelationId,
                ["IncidentId"] = command.IncidentId,
                ["CommandId"] = command.CommandId
            });

        _logger.LogInformation(
            "Received historical Incident indexing command for Incident {IncidentId}.",
            command.IncidentId);

        // BackgroundService instances are singletons. The indexing workflow
        // uses scoped dependencies such as repositories and Cosmos stores, so
        // create a fresh scope for every Service Bus message.
        await using var serviceScope = _scopeFactory.CreateAsyncScope();

        var handler =
            serviceScope.ServiceProvider
                .GetRequiredService<IndexHistoricalIncidentHandler>();

        try
        {
            await handler.HandleAsync(
                command,
                args.CancellationToken);
        }
        catch (OperationCanceledException)
            when (args.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Earlier failures are allowed to propagate so Service Bus can
            // redeliver the message. On the final permitted delivery, retain
            // the failed command in the DLQ for inspection.
            if (args.Message.DeliveryCount >= _maxDeliveryCount)
            {
                await HandleFinalFailureAsync(
                    args,
                    command,
                    exception);

                return;
            }

            _logger.LogWarning(
                exception,
                "Historical Incident indexing attempt {DeliveryCount} failed for Incident {IncidentId}. Message will be retried.",
                args.Message.DeliveryCount,
                command.IncidentId);

            throw;
        }

        await args.CompleteMessageAsync(
            args.Message,
            args.CancellationToken);

        _logger.LogInformation(
            "Completed historical Incident indexing command {CommandId} for Incident {IncidentId}.",
            command.CommandId,
            command.IncidentId);
    }

    /// <summary>
    /// Handles errors raised by the Service Bus processor infrastructure.
    /// </summary>
    private Task ProcessErrorAsync(
        ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Historical Incident indexing Service Bus processor error. Source: {ErrorSource}, Entity: {EntityPath}.",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dead-letters a historical Incident indexing command after all permitted
    /// delivery attempts have been exhausted.
    /// </summary>
    private async Task HandleFinalFailureAsync(
        ProcessMessageEventArgs args,
        IndexHistoricalIncidentCommand command,
        Exception exception)
    {
        _logger.LogError(
            exception,
            "Historical Incident indexing command {CommandId} exhausted {DeliveryCount} delivery attempts for Incident {IncidentId}.",
            command.CommandId,
            args.Message.DeliveryCount,
            command.IncidentId);

        await args.DeadLetterMessageAsync(
            args.Message,
            "HistoricalIncidentIndexingFailed",
            $"Historical Incident indexing failed after {args.Message.DeliveryCount} delivery attempts.",
            args.CancellationToken);
    }
}