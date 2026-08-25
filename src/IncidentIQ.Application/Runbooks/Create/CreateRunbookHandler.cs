using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Runbooks;

namespace IncidentIQ.Application.Runbooks.Create;

/// <summary>
/// Represents a handler for creating a runbook.
/// </summary>
/// <param name="runbookRepository">The repository used to manage runbooks.</param>
/// <param name="validator">The validator used to validate the create runbook command.</param>
public sealed class CreateRunbookHandler(
    IRunbookRepository runbookRepository,
    IValidator<CreateRunbookCommand> validator)
{

    /// <summary>
    /// Handles the creation of a runbook based on the provided command.
    /// </summary>
    /// <param name="command">The command containing the details of the runbook to be created.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The created <see cref="Runbook"/>.</returns>
    public async Task<Runbook> HandleAsync(
        CreateRunbookCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(
            command,
            cancellationToken);

        var runbook = Runbook.Create(
            command.Title,
            command.Description,
            command.Service,
            command.Content);

        await runbookRepository.CreateAsync(
            runbook,
            cancellationToken);

        return runbook;
    }
}