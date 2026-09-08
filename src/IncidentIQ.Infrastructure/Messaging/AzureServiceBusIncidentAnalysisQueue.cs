using Azure.Messaging.ServiceBus;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IncidentIQ.Infrastructure.Messaging;

/// <summary>
/// Publishes incident analysis commands to Azure Service Bus.
/// </summary>
internal sealed class AzureServiceBusIncidentAnalysisQueue
    : IIncidentAnalysisQueue, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusIncidentAnalysisQueue(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options)
    {
        _sender = client.CreateSender(
            options.Value.AnalyseIncidentQueueName);
    }

    public async Task EnqueueAsync(
        AnalyseIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(command);

        var message = new ServiceBusMessage(body)
        {
            // Duplicate detection on the queue uses MessageId.
            MessageId = command.CommandId.ToString(),

            CorrelationId = command.CorrelationId,

            ContentType = "application/json",

            Subject = nameof(AnalyseIncidentCommand)
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