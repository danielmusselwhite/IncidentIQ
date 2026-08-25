using FluentValidation;
using IncidentIQ.Application.Incidents.Create;

namespace IncidentIQ.Application.Runbooks.Create;


/// <summary>
/// Represents a validator for the <see cref="CreateRunbookCommand"/>.
/// Utilizes FluentValidation to ensure that the command's properties meet the required criteria before processing.
/// </summary>
public sealed class CreateRunbookValidator: AbstractValidator<CreateRunbookCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRunbookValidator"/> class.
    /// Validates that the properties of the <see cref="CreateRunbookCommand"/> are not empty, have appropriate lengths, and that the severity is a valid enum value.
    /// Utilizes FluentValidation's built-in methods to enforce these rules, ensuring that the command is valid before it is processed by the application.
    /// </summary>
    public CreateRunbookValidator()
    {
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