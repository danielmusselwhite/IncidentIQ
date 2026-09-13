using IncidentIQ.Application.Assistant.Generate;
using IncidentIQ.Application.Assistant.Grounding;
using IncidentIQ.Application.Common.Grounding;

namespace IncidentIQ.Infrastructure.AI.Assistant;

/// <summary>
/// Provides deterministic grounded Assistant responses for local development
/// without making external AI requests.
/// </summary>
public sealed class DevelopmentDummyOperationalAssistant
    : IOperationalAssistant
{
    public Task<OperationalAnswer> AnswerAsync(
        OperationalQuestionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sections =
            new List<OperationalAnswerSection>();

        if (context.HistoricalIncidents.Count > 0)
        {
            var incident =
                context.HistoricalIncidents[0];

            sections.Add(
                new OperationalAnswerSection(
                    Content:
                        $"A similar historical Incident was found: {incident.Title}.",
                    EvidenceReferences:
                    [
                        EvidenceReferenceId.HistoricalIncident(0)
                    ]));
        }

        if (context.RunbookChunks.Count > 0)
        {
            var runbookChunk =
                context.RunbookChunks[0];

            sections.Add(
                new OperationalAnswerSection(
                    Content:
                        $"Relevant operational guidance was found in the {runbookChunk.Title} Runbook.",
                    EvidenceReferences:
                    [
                        EvidenceReferenceId.RunbookChunk(0)
                    ]));
        }

        if (sections.Count == 0)
        {
            sections.Add(
                new OperationalAnswerSection(
                    Content:
                        "No relevant historical Incidents or Runbook guidance were retrieved for this question.",
                    EvidenceReferences: []));
        }

        var answer =
            new OperationalAnswer(
                Sections: sections,
                Model: "development-dummy",
                AnsweredAtUtc: DateTimeOffset.UtcNow);

        return Task.FromResult(answer);
    }
}