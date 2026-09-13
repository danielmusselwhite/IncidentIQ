using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.HistoricalSearch.Index;

/// <summary>
/// Creates and persists the semantic-search representation of a completed Incident.
/// </summary>
public sealed class IndexHistoricalIncidentHandler
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IHistoricalIncidentVectorStore _historicalIncidentVectorStore;

    public IndexHistoricalIncidentHandler(
        IIncidentRepository incidentRepository,
        IEmbeddingGenerator embeddingGenerator,
        IHistoricalIncidentVectorStore historicalIncidentVectorStore)
    {
        _incidentRepository = incidentRepository;
        _embeddingGenerator = embeddingGenerator;
        _historicalIncidentVectorStore = historicalIncidentVectorStore;
    }

    /// <summary>
    /// Loads the completed Incident, generates its embedding and persists its
    /// derived vector representation for semantic retrieval.
    /// </summary>
    public async Task HandleAsync(
        IndexHistoricalIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await _incidentRepository.GetByIdAsync(
            command.IncidentId,
            cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException($"Incident '{command.IncidentId}' was not found.");
        }

        // Historical search should only contain successfully completed Incidents.
        if (incident.Status != IncidentStatus.Completed ||
            incident.CompletedAt is null)
        {
            return;
        }

        var embeddingText = BuildEmbeddingText(incident);

        var embedding = await _embeddingGenerator.GenerateAsync(
            embeddingText,
            cancellationToken);

        var incidentVector = new HistoricalIncidentVector(
            IncidentId: incident.Id,
            Title: incident.Title,
            Description: incident.Description,
            Symptoms: incident.Symptoms,
            Service: incident.Service,
            Environment: incident.Environment,
            Severity: incident.Severity.ToString(),
            CompletedAtUtc: incident.CompletedAt.Value,
            Embedding: embedding);

        await _historicalIncidentVectorStore.UpsertAsync(
            incidentVector,
            cancellationToken);
    }

    /// <summary>
    /// Builds the stable natural-language representation used to generate
    /// an Incident's semantic-search embedding.
    /// </summary>
    private static string BuildEmbeddingText(Incident incident)
    {
        return $"""
            Title: {incident.Title}

            Description:
            {incident.Description}

            Symptoms:
            {incident.Symptoms ?? "None provided"}
            """;
    }
}