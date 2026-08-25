using FluentValidation;

namespace IncidentIQ.Application.Runbooks.Update;

/// <summary>
/// Represents a validator for the <see cref="UpdateRunbookCommand"/>.
/// </summary>
public sealed class UpdateRunbookValidator
    : AbstractValidator<UpdateRunbookCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRunbookValidator"/> class.
    /// </summary>
    public UpdateRunbookValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Service)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(50000);
    }
}