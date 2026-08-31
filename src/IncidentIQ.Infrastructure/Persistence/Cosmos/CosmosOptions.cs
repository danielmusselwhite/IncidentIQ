namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Configuration options for connecting to a Cosmos DB instance.
/// Set in the application configuration (e.g., appsettings.json or environment variables).
/// In prod, the environment variables are set via the Bicep templates used for infrastructure deployment.
/// </summary>
public sealed class CosmosOptions
{
    /// <summary>
    /// The section name in the application configuration for Cosmos DB options.
    /// </summary>
    public const string SectionName = "Cosmos";

    public required string Endpoint { get; init; }

    /// <summary>
    /// The primary key for the Cosmos DB account.
    /// Optional, as it is needed when running in dev with the Cosmos emulator, 
    /// But not needed when running in Azure with managed identity.
    /// </summary>
    public string? Key { get; init; }

    public required string DatabaseName { get; init; }

    #region Containers
    public required string IncidentsContainerName { get; init; }
    public required string RunbooksContainerName { get; init; }
    public required string ChangeFeedLeasesContainerName { get; init; }
    #endregion
}