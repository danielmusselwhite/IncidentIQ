using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Retrieve;

namespace IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;

/// <summary>
/// Generates an embedding for a semantic search query and retrieves matching
/// historical Incidents from the configured vector store.
/// </summary>
public sealed class RetrieveHistoricalIncidentsHandler
{
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IHistoricalIncidentRetriever _historicalIncidentRetriever;
    private readonly IValidator<RetrieveHistoricalIncidentsQuery> _validator;

    public RetrieveHistoricalIncidentsHandler(
        IEmbeddingGenerator embeddingGenerator,
        IHistoricalIncidentRetriever historicalIncidentRetriever,
        IValidator<RetrieveHistoricalIncidentsQuery> validator)
    {
        _embeddingGenerator = embeddingGenerator;
        _historicalIncidentRetriever = historicalIncidentRetriever;
        _validator = validator;
    }

    /// <summary>
    /// Generates an embedding for the provided query and retrieves matching historical Incidents
    /// </summary>
    /// <param name="query">The query to retrieve historical incidents for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of historical incident matches.</returns>
    public async Task<IReadOnlyList<HistoricalIncidentMatch>> HandleAsync(
        RetrieveHistoricalIncidentsQuery query,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var embedding = await _embeddingGenerator.GenerateAsync(query.Query.Trim(), cancellationToken);

        return await _historicalIncidentRetriever.RetrieveAsync(
            embedding,
            query.Service,
            query.Environment,
            query.TopK,
            cancellationToken);
    }
}