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
    }
}