namespace IncidentIQ.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public required string FullyQualifiedNamespace { get; init; }

    public required string AnalyseIncidentQueueName { get; init; }

    /// <summary>
    /// Connection string is optional as the Service Bus client can also be authenticated using Azure AD credentials.
    /// So in production it is done via API Managed Identity, but in development it is done via connection string. 
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// The maximum number of times a message can be delivered before it is dead-lettered.
    /// Must align with both the 'Bicep' configuration and the Service Bus entity settings.
    /// And (if working locally) the local Service Bus emulator settings.
    /// </summary>
    public int MaxDeliveryCount { get; init; } = 5;
}