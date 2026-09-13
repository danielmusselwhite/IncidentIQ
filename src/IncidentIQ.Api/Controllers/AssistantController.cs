using IncidentIQ.Api.Contracts.Assistant;
using IncidentIQ.Application.Assistant.Ask;
using IncidentIQ.Application.Assistant.Conversation;
using IncidentIQ.Application.Common.Grounding;
using Microsoft.AspNetCore.Mvc;

namespace IncidentIQ.Api.Assistant;

[ApiController]
[Route("api/assistant")]
public sealed class AssistantController(
    AskOperationalQuestionHandler handler)
    : ControllerBase
{
    [HttpPost("questions")]
    public async Task<ActionResult<OperationalAssistantResponse>> Ask(
        AskOperationalQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var conversationHistory = request.ConversationHistory?
            .Select(turn =>
                new ConversationTurn(
                    Role: ParseRole(turn.Role),
                    Content: turn.Content))
            .ToList();

        var query = new AskOperationalQuestionQuery(
            Question: request.Question,
            Service: request.Service,
            Environment: request.Environment,
            ConversationHistory: conversationHistory);

        var result = await handler.HandleAsync(
            query,
            cancellationToken);

        var response = new OperationalAssistantResponse(
            Sections: result.Answer.Sections
                .Select(section =>
                    new OperationalAnswerSectionResponse(
                        section.Content,
                        section.EvidenceReferences))
                .ToList(),

            Evidence: new AssistantEvidenceResponse(
                HistoricalIncidents: result.HistoricalIncidents
                    .Select((incident, index) =>
                        new AssistantHistoricalIncidentEvidenceResponse(
                            ReferenceId:
                                EvidenceReferenceId.HistoricalIncident(index),
                            IncidentId: incident.IncidentId,
                            Title: incident.Title,
                            Description: incident.Description,
                            Symptoms: incident.Symptoms,
                            Service: incident.Service,
                            Environment: incident.Environment,
                            Severity: incident.Severity.ToString(),
                            CompletedAtUtc: incident.CompletedAtUtc))
                    .ToList(),

                RunbookChunks: result.RunbookChunks
                    .Select((chunk, index) =>
                        new AssistantRunbookEvidenceResponse(
                            ReferenceId:
                                EvidenceReferenceId.RunbookChunk(index),
                            RunbookId: chunk.RunbookId,
                            ChunkIndex: chunk.ChunkIndex,
                            Title: chunk.Title,
                            Service: chunk.Service,
                            Content: chunk.Content))
                    .ToList()),

            Model: result.Answer.Model,
            AnsweredAtUtc: result.Answer.AnsweredAtUtc);

        return Ok(response);
    }

    private static ConversationRole ParseRole(string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            "user" => ConversationRole.User,
            "assistant" => ConversationRole.Assistant,

            _ => throw new ArgumentException(
                $"Unsupported conversation role '{role}'.",
                nameof(role))
        };
    }
}