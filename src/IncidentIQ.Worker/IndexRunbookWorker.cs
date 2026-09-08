using Azure.Messaging.ServiceBus;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IncidentIQ.Worker;

/// <summary>
/// Background worker responsible for consuming <see cref="IndexRunbookCommand"/>
/// messages from the Service Bus Runbook indexing queue.
/// 
/// The worker acts as the transport boundary between Azure Service Bus and the
/// Application layer. Each message is processed inside its own dependency
/// injection scope before being explicitly completed or dead-lettered.
/// </summary>
public sealed class IndexRunbookWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IndexRunbookWorker> _logger;
    private readonly int _maxDeliveryCount;

    /// <summary>
    /// Creates the Service Bus processor used to consume Runbook indexing commands.
    /// </summary>
    public IndexRunbookWorker(
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        IOptions<ServiceBusOptions> options,
        ILogger<IndexRunbookWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var serviceBusOptions = options.Value;

        _maxDeliveryCount = serviceBusOptions.MaxDeliveryCount;

        _processor = serviceBusClient.CreateProcessor(
            serviceBusOptions.IndexRunbookQueueName,
            new ServiceBusProcessorOptions
            {
                // The message is only completed after the indexing workflow
                // finishes successfully.
                AutoCompleteMessages = false,

                // Keep processing sequential while the indexing pipeline is
                // being developed and verified.
                MaxConcurrentCalls = 1,

                // Embedding generation may involve several external requests,
                // so allow the SDK to renew the Service Bus message lock.
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    /// <summary>
    /// Starts the Service Bus processor and keeps the hosted service alive until
    /// the Worker host requests shutdown.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting IndexRunbook Service Bus processor.");

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
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping IndexRunbook Service Bus processor.");

        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Handles a single Service Bus message containing an
    /// <see cref="IndexRunbookCommand"/>.
    /// </summary>
    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        IndexRunbookCommand? command;

        try
        {
            command = JsonSerializer.Deserialize<IndexRunbookCommand>(
                args.Message.Body);
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception,
                "Unable to deserialize Runbook indexing message {MessageId}.",
                args.Message.MessageId);

            // Retrying malformed JSON will never make it valid.
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidMessage",
                "Message could not be deserialized as IndexRunbookCommand.",
                args.CancellationToken);

            return;
        }

        if (command is null)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidMessage",
                "IndexRunbookCommand was null.",
                args.CancellationToken);

            return;
        }

        using var loggingScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["CorrelationId"] = command.CorrelationId,
                ["RunbookId"] = command.RunbookId,
                ["CommandId"] = command.CommandId
            });

        _logger.LogInformation(
            "Received IndexRunbook command for Runbook {RunbookId}.",
            command.RunbookId);

        // BackgroundService instances are singletons. The indexing workflow
        // uses scoped dependencies such as repositories and Cosmos stores, so
        // create a fresh scope for every Service Bus message.
        await using var serviceScope = _scopeFactory.CreateAsyncScope();

        var indexRunbookHandler =
            serviceScope.ServiceProvider.GetRequiredService<IndexRunbookHandler>();

        try
        {
            await indexRunbookHandler.HandleAsync(
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
                "IndexRunbook attempt {DeliveryCount} failed for Runbook {RunbookId}. Message will be retried.",
                args.Message.DeliveryCount,
                command.RunbookId);

            throw;
        }

        await args.CompleteMessageAsync(
            args.Message,
            args.CancellationToken);

        _logger.LogInformation(
            "Completed IndexRunbook command {CommandId} for Runbook {RunbookId}.",
            command.CommandId,
            command.RunbookId);
    }

    /// <summary>
    /// Handles errors raised by the Service Bus processor infrastructure.
    /// </summary>
    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Runbook indexing Service Bus processor error. Source: {ErrorSource}, Entity: {EntityPath}.",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dead-letters a Runbook indexing command after all permitted delivery
    /// attempts have been exhausted.
    /// </summary>
    private async Task HandleFinalFailureAsync(
        ProcessMessageEventArgs args,
        IndexRunbookCommand command,
        Exception exception)
    {
        _logger.LogError(
            exception,
            "IndexRunbook command {CommandId} exhausted {DeliveryCount} delivery attempts for Runbook {RunbookId}.",
            command.CommandId,
            args.Message.DeliveryCount,
            command.RunbookId);

        await args.DeadLetterMessageAsync(
            args.Message,
            "RunbookIndexingFailed",
            $"Runbook indexing failed after {args.Message.DeliveryCount} delivery attempts.",
            args.CancellationToken);
    }
}