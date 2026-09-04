using Azure.Messaging.ServiceBus;
using IncidentIQ.Application.Analyse;
using IncidentIQ.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IncidentIQ.Worker;

/// <summary>
/// Background worker responsible for consuming <see cref="AnalyseIncidentCommand"/> messages from the Service Bus analysis queue.
///
/// The worker acts as the transport boundary between Azure Service Bus and the application layer. It receives and validates messages, establishes logging context, creates a dependency injection scope for each message, invokes the analysis handler, and settles processed messages.
/// </summary>
public sealed class AnalyseIncidentWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalyseIncidentWorker> _logger;
    private readonly int _maxDeliveryCount;

    /// <summary>
    /// Creates the Service Bus processor used to consume incident analysis commands.
    /// </summary>
    /// <param name="serviceBusClient">Shared Service Bus client used to create the queue processor.</param>
    /// <param name="scopeFactory">Creates a dependency injection scope for each processed message.</param>
    /// <param name="options">Service Bus configuration, including queue and delivery settings.</param>
    /// <param name="logger">Logger used for Worker lifecycle and message-processing diagnostics.</param>
    public AnalyseIncidentWorker(
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        IOptions<ServiceBusOptions> options,
        ILogger<AnalyseIncidentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var serviceBusOptions = options.Value;
        _maxDeliveryCount = serviceBusOptions.MaxDeliveryCount;

        _processor = serviceBusClient.CreateProcessor(
            serviceBusOptions.AnalyseIncidentQueueName,
            new ServiceBusProcessorOptions
            {
                // Messages are explicitly completed only after successful processing.
                AutoCompleteMessages = false,

                // Keep processing sequential while the pipeline is being developed.
                MaxConcurrentCalls = 1,

                // Renew the lock during potentially long-running analysis.
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    /// <summary>
    /// Starts the Service Bus processor and keeps the background service alive until the host requests shutdown.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token triggered when the Worker host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting AnalyseIncident Service Bus processor.");

        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            // The Service Bus SDK processes messages through the registered callbacks, so the worker only needs to remain alive.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Cancellation is expected during normal Worker shutdown.
        }
    }

    /// <summary>
    /// Stops message consumption and disposes the Service Bus processor when the Worker host shuts down.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token controlling the shutdown operation.</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping AnalyseIncident Service Bus processor.");

        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Handles a single Service Bus message containing an <see cref="AnalyseIncidentCommand"/>.
    /// A new dependency injection scope is created for each message so scoped services are not retained for the lifetime of the background Worker.
    /// </summary>
    /// <param name="args">Service Bus processing context containing the message and settlement operations.</param>
    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        AnalyseIncidentCommand? command;

        try
        {
            // Convert the transport-level JSON message into the application command.
            command = JsonSerializer.Deserialize<AnalyseIncidentCommand>(args.Message.Body);
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Unable to deserialize Service Bus message {MessageId}.", args.Message.MessageId);

            // Invalid messages cannot succeed through retries, so dead-letter them immediately.
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidMessage",
                "Message could not be deserialized as AnalyseIncidentCommand.",
                args.CancellationToken);

            return;
        }

        if (command is null)
        {
            // A valid JSON payload that produces no command is permanently invalid.
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidMessage",
                "AnalyseIncidentCommand was null.",
                args.CancellationToken);

            return;
        }

        // Attach workflow identifiers so related API, Service Bus and Worker activity can be correlated.
        using var loggingScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = command.CorrelationId,
            ["IncidentId"] = command.IncidentId,
            ["CommandId"] = command.CommandId
        });

        _logger.LogInformation("Received AnalyseIncident command for incident {IncidentId}.", command.IncidentId);

        // BackgroundService is a singleton, while the analysis workflow uses scoped dependencies. 
        // Creating one scope per message gives each message its own handler, repositories and analyzer.
        await using var serviceScope = _scopeFactory.CreateAsyncScope();
        var analyseIncidentHandler = serviceScope.ServiceProvider.GetRequiredService<AnalyseIncidentHandler>();

        try
        {
            // Delegate the business workflow to the Application layer.
            await analyseIncidentHandler.HandleAsync(command, args.CancellationToken);
        }
        catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Final delivery failures are persisted and dead-lettered; earlier failures are left for Service Bus to retry.
            if (args.Message.DeliveryCount >= _maxDeliveryCount)
            {
                await HandleFinalFailureAsync(args, command, exception, analyseIncidentHandler);
                return;
            }

            _logger.LogWarning(
                exception,
                "AnalyseIncident attempt {DeliveryCount} failed for Incident {IncidentId}. Message will be retried.",
                args.Message.DeliveryCount,
                command.IncidentId);

            throw;
        }

        // Only settle the Service Bus message after processing succeeds.
        await args.CompleteMessageAsync(args.Message, args.CancellationToken);

        _logger.LogInformation(
            "Completed AnalyseIncident command {CommandId} for Incident {IncidentId}.",
            command.CommandId,
            command.IncidentId);
    }

    /// <summary>
    /// Handles errors raised by the Service Bus processor infrastructure, such as connection, authentication, or message-pump failures.
    /// </summary>
    /// <param name="args">Information describing the processor error and where it occurred.</param>
    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus processor error. Source: {ErrorSource}, Entity: {EntityPath}.",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the final failure of an AnalyseIncident command after all permitted delivery attempts have been exhausted.
    /// </summary>
    /// <param name="args">The message processing event arguments.</param>
    /// <param name="command">The AnalyseIncident command that failed.</param>
    /// <param name="exception">The exception that caused the final failure.</param>
    /// <param name="analyseIncidentHandler">The scoped handler being used for the current message.</param>
    private async Task HandleFinalFailureAsync(
        ProcessMessageEventArgs args,
        AnalyseIncidentCommand command,
        Exception exception,
        AnalyseIncidentHandler analyseIncidentHandler)
    {
        _logger.LogError(
            exception,
            "AnalyseIncident command {CommandId} exhausted {DeliveryCount} delivery attempts for Incident {IncidentId}.",
            command.CommandId,
            args.Message.DeliveryCount,
            command.IncidentId);

        // Persist the terminal application state before settling the Service Bus message.
        await analyseIncidentHandler.MarkFailedAsync(command, exception.Message, args.CancellationToken);

        // Keep the failed command in the DLQ for later inspection and administrative requeue.
        await args.DeadLetterMessageAsync(
            args.Message,
            "AnalysisFailed",
            $"Incident analysis failed after {args.Message.DeliveryCount} delivery attempts.",
            args.CancellationToken);
    }
}