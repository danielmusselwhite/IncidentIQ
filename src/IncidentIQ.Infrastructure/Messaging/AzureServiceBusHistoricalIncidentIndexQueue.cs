using System.Text.Json;
using Azure.Messaging.ServiceBus;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.HistoricalSearch.Index;
using Microsoft.Extensions.Options;

namespace IncidentIQ.Infrastructure.Messaging;

/// <summary>
/// Publishes historical Incident indexing commands to Azure Service Bus.
/// </summary>
public sealed class AzureServiceBusHistoricalIncidentIndexQueue: IHistoricalIncidentIndexQueue
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusHistoricalIncidentIndexQueue(ServiceBusClient serviceBusClient, IOptions<ServiceBusOptions> options)
    {
        var serviceBusOptions = options.Value;

        _sender = serviceBusClient.CreateSender(serviceBusOptions.IndexHistoricalIncidentQueueName);
    }

    public async Task EnqueueAsync(
        IndexHistoricalIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var message = new ServiceBusMessage(JsonSerializer.Serialize(command))
        {
            MessageId = command.CommandId.ToString(),
            CorrelationId = command.CorrelationId,
            ContentType = "application/json"
        };

        await _sender.SendMessageAsync(message, cancellationToken);
    }
}