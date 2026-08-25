namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

public sealed class CosmosOptions
{
    public const string SectionName = "Cosmos";

    public required string Endpoint { get; init; }

    /// <summary>
    /// The primary key for the Cosmos DB account.
    /// Optional, as it is needed when running in dev with the Cosmos emulator, 
    /// But not needed when running in Azure with managed identity.
    /// </summary>
    public required string? Key { get; init; }

    public required string DatabaseName { get; init; }

    #region Containers
    public required string IncidentsContainerName { get; init; }
    public required string RunbooksContainerName { get; init; }
    #endregion
}