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
}