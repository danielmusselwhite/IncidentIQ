namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Builds the semantic retrieval input used to find evidence relevant
/// to the Incident being analysed.
/// </summary>
public static class IncidentRetrievalInputBuilder
{
    /// <summary>
    /// Builds the semantic retrieval input used to find evidence relevant to the Incident being analysed.
    /// </summary>
    /// <param name="incident">The incident analysis input.</param>
    /// <returns>The semantic retrieval input.</returns>
    public static IncidentRetrievalInput Build(IncidentAnalysisInput incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        // !IMPORTANT - this matches the shape used when embedding historical incidents, giving a consistent semantic representation for vector comparison in the retrieval process.
        var queryText =
            $"""
            Title: {incident.Title}

            Description:
            {incident.Description}

            Symptoms:
            {incident.Symptoms ?? "None provided"}
            """;

        return new IncidentRetrievalInput(
            QueryText: queryText,
            Service: incident.Service,
            Environment: incident.Environment);
    }
}