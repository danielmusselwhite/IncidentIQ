using Azure.Messaging.ServiceBus;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Analyse;
using System.Text.Json;

namespace IncidentIQ.Infrastructure.Messaging;

/// <summary>
/// Queues Incident analysis commands using Azure Service Bus.
/// </summary>
internal sealed class AzureServiceBusIncidentAnalysisQueue(
    ServiceBusSender sender)
    : IIncidentAnalysisQueue
{
    public async Task EnqueueAsync(
        AnalyseIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(command);

        var message = new ServiceBusMessage(body)
        {
            // Duplicate detection on the queue uses MessageId.
            MessageId = command.CommandId.ToString(),

            // Allows messages for the same workflow to be correlated.
            CorrelationId = command.CorrelationId,

            ContentType = "application/json",

            Subject = nameof(AnalyseIncidentCommand)
        };

        await sender.SendMessageAsync(
            message,
            cancellationToken);
    }
}