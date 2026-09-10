namespace IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;

/// <summary>
/// Represents a request to retrieve historical Incidents that are semantically
/// similar to the supplied natural-language query.
/// </summary>
public sealed record RetrieveHistoricalIncidentsQuery(
    string Query,
    string? Service = null,
    string? Environment = null,
    int TopK = 5);