using Azure.Messaging.ServiceBus;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IncidentIQ.Worker;

/// <summary>
/// Background worker responsible for consuming <see cref="AnalyseIncidentCommand"/>
/// messages from the Service Bus analysis queue.
/// 
/// The worker acts as the transport boundary between Azure Service Bus and the
/// application layer. It is responsible for receiving and validating messages,
/// establishing logging context, invoking the analysis handler, and completing
/// successfully processed messages.
/// </summary>
public sealed class AnalyseIncidentWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<AnalyseIncidentWorker> _logger;
    private readonly AnalyseIncidentHandler _analyseIncidentHandler;

    /// <summary>
    /// Creates the Service Bus processor used to consume incident analysis commands.
    /// </summary>
    /// <param name="serviceBusClient">
    /// Shared Service Bus client used to create the queue processor.
    /// </param>
    /// <param name="analyseIncidentHandler">
    /// Application handler containing the actual incident analysis workflow.
    /// </param>
    /// <param name="options">
    /// Service Bus configuration, including the analysis queue name.
    /// </param>
    /// <param name="logger">
    /// Logger used for worker lifecycle and message-processing diagnostics.
    /// </param>
    public AnalyseIncidentWorker(
        ServiceBusClient serviceBusClient,
        AnalyseIncidentHandler analyseIncidentHandler,
        IOptions<ServiceBusOptions> options,
        ILogger<AnalyseIncidentWorker> logger)
    {
        _logger = logger;
        _analyseIncidentHandler = analyseIncidentHandler;

        _processor = serviceBusClient.CreateProcessor(
            options.Value.AnalyseIncidentQueueName,
            new ServiceBusProcessorOptions
            {
                // Messages are completed explicitly only after the application
                // handler finishes successfully. If processing fails before then,
                // Service Bus can retry the message according to the queue policy.
                AutoCompleteMessages = false,

                // Process one analysis command at a time while the pipeline is
                // being developed. This can be increased later once processing
                // behaviour and resource usage are well understood.
                MaxConcurrentCalls = 1,

                // Automatically renew the message lock during longer-running work.
                // This prevents Service Bus from making the same message available
                // to another consumer while this worker is still processing it.
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
            });

        // Register the callbacks used by the Service Bus processor.
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    /// <summary>
    /// Starts the Service Bus processor and keeps the background service alive
    /// until the host requests shutdown.
    /// </summary>
    /// <param name="stoppingToken">
    /// Cancellation token triggered when the Worker host is shutting down.
    /// </param>
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting AnalyseIncident Service Bus processor.");

        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            // The Service Bus SDK performs message processing through the
            // registered callbacks, so the background service only needs to
            // remain alive until cancellation is requested.
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Cancellation is expected during a normal Worker shutdown.
        }
    }

    /// <summary>
    /// Stops message consumption and disposes the Service Bus processor when
    /// the Worker host shuts down.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancellation token controlling the shutdown operation.
    /// </param>
    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping AnalyseIncident Service Bus processor.");

        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Handles a single Service Bus message containing an
    /// <see cref="AnalyseIncidentCommand"/>.
    /// </summary>
    /// <param name="args">
    /// Service Bus processing context containing the message and settlement operations.
    /// </param>
    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args)
    {
        AnalyseIncidentCommand? command;

        try
        {
            // Convert the transport-level JSON message into the application
            // command understood by the analysis handler.
            command = JsonSerializer.Deserialize<AnalyseIncidentCommand>(
                args.Message.Body);
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception,
                "Unable to deserialize Service Bus message {MessageId}.",
                args.Message.MessageId);

            // Invalid messages cannot succeed through retries, so move them
            // directly to the dead-letter queue rather than repeatedly
            // attempting to process them.
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidMessage",
                "Message could not be deserialized as AnalyseIncidentCommand.");

            return;
        }

        if (command is null)
        {
            // A valid JSON payload that produces no command is also considered
            // permanently invalid and should not be retried.
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidMessage",
                "AnalyseIncidentCommand was null.");

            return;
        }

        // Attach workflow identifiers to all log messages produced while this
        // command is processed. This allows API, Service Bus and Worker activity
        // for the same incident to be correlated during troubleshooting.
        using var scope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["CorrelationId"] = command.CorrelationId,
                ["IncidentId"] = command.IncidentId,
                ["CommandId"] = command.CommandId
            });

        _logger.LogInformation(
            "Received AnalyseIncident command for incident {IncidentId}.",
            command.IncidentId);

        // Delegate the business workflow to the Application layer. The Worker
        // should remain focused on transport concerns rather than containing
        // incident-processing logic itself.
        await _analyseIncidentHandler.HandleAsync(
            command,
            args.CancellationToken);

        // Only settle the Service Bus message after processing succeeds.
        // If the handler throws, the message remains uncompleted and can be
        // retried by Service Bus.
        await args.CompleteMessageAsync(
            args.Message,
            args.CancellationToken);

        _logger.LogInformation(
            "Completed AnalyseIncident command {CommandId}.",
            command.CommandId);
    }

    /// <summary>
    /// Handles errors raised by the Service Bus processor infrastructure,
    /// such as connection, authentication or message-pump failures.
    /// </summary>
    /// <param name="args">
    /// Information describing the processor error and where it occurred.
    /// </param>
    private Task ProcessErrorAsync(
        ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus processor error. Source: {ErrorSource}, Entity: {EntityPath}.",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }
}