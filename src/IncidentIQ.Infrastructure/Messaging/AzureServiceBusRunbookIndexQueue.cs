using Azure.Messaging.ServiceBus;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Runbooks.Index;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IncidentIQ.Infrastructure.Messaging;

/// <summary>
/// Publishes Runbook indexing commands to Azure Service Bus.
/// </summary>
internal sealed class AzureServiceBusRunbookIndexQueue
    : IRunbookIndexQueue, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusRunbookIndexQueue(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options)
    {
        _sender = client.CreateSender(
            options.Value.IndexRunbookQueueName);
    }

    public async Task EnqueueAsync(
        IndexRunbookCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var body = JsonSerializer.Serialize(command);

        var message = new ServiceBusMessage(body)
        {
            // Use the Runbook revision as the Service Bus message identity.
            // If the Change Feed delivers the same revision more than once,
            // Service Bus duplicate detection can suppress the duplicate.
            MessageId = $"{command.RunbookId}:{command.SourceUpdatedAtUtc.ToUniversalTime():O}",

            CorrelationId = command.CorrelationId,

            ContentType = "application/json",

            Subject = nameof(IndexRunbookCommand)
        };

        await _sender.SendMessageAsync(
            message,
            cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _sender.DisposeAsync();
    }
}