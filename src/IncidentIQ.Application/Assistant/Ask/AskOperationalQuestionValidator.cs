using FluentValidation;

namespace IncidentIQ.Application.Assistant.Ask;

public sealed class AskOperationalQuestionValidator
    : AbstractValidator<AskOperationalQuestionQuery>
{
    public AskOperationalQuestionValidator()
    {
        RuleFor(query => query.Question)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(query => query.Service)
            .MaximumLength(100);

        RuleFor(query => query.Environment)
            .MaximumLength(100);

        RuleFor(query => query.ConversationHistory)
            .Must(history => history is null || history.Count <= 10)
            .WithMessage(
                "Conversation history cannot contain more than 10 turns.");

        RuleForEach(query => query.ConversationHistory)
            .ChildRules(turn =>
            {
                turn.RuleFor(item => item.Content)
                    .NotEmpty()
                    .MaximumLength(4000);
            });
    }
}