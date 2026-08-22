namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

public sealed class CosmosOptions
{
    public const string SectionName = "Cosmos";

    public required string Endpoint { get; init; }

    public required string Key { get; init; }

    public required string DatabaseName { get; init; }

    public required string IncidentsContainerName { get; init; }
}