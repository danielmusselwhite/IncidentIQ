using IncidentIQ.Application.Assistant.Generate;
using IncidentIQ.Application.Assistant.Grounding;

namespace IncidentIQ.Application.Assistant.Ask;

/// <summary>
/// Orchestrates retrieval and grounded generation for an operational question.
/// </summary>
public sealed class AskOperationalQuestionHandler
{
    private readonly OperationalQuestionContextBuilder _contextBuilder;
    private readonly IOperationalAssistant _operationalAssistant;

    public AskOperationalQuestionHandler(
        OperationalQuestionContextBuilder contextBuilder,
        IOperationalAssistant operationalAssistant)
    {
        _contextBuilder = contextBuilder;
        _operationalAssistant = operationalAssistant;
    }

    /// <summary>
    /// Handles the AskOperationalQuestionQuery by building the context, generating an answer, and validating evidence references.
    /// </summary>
    /// <param name="query">The query containing the question, service, and environment.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The result of the operational assistant, including the answer and retrieved evidence.</returns>
    public async Task<OperationalAssistantResult> HandleAsync(
        AskOperationalQuestionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Build the context for the operational question, including the top-K related historical incidents and runbook chunks.
        var context = await _contextBuilder.BuildAsync(
            query.Question,
            query.Service,
            query.Environment,
            cancellationToken);

        // Generate an answer to the operational question using the built context.
        var answer = await _operationalAssistant.AnswerAsync(
            context,
            cancellationToken);

        // Extract the evidence references from the answer sections.
        var evidenceReferences = answer.Sections
            .SelectMany(section =>
                section.EvidenceReferences);

        // Validate that all evidence references in the answer are present in the retrieved historical incidents and runbook chunks.
        OperationalEvidenceReferenceValidator.Validate(
            evidenceReferences,
            context);

        // Return the final result, including the answer and the retrieved evidence.
        return new OperationalAssistantResult(
            Answer: answer,
            HistoricalIncidents: context.HistoricalIncidents,
            RunbookChunks: context.RunbookChunks);
    }
}