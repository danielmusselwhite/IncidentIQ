using IncidentIQ.Application.Assistant.Grounding;

namespace IncidentIQ.Application.Assistant.Generate;

public interface IOperationalAssistant
{
    Task<OperationalAnswer> AnswerAsync(
        OperationalQuestionContext context,
        CancellationToken cancellationToken = default);
}