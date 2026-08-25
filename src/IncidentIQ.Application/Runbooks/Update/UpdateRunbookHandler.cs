using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Domain.Runbooks;

namespace IncidentIQ.Application.Runbooks.Update;

/// <summary>
/// Represents a handler for updating a runbook.
/// </summary>
/// <param name="runbookRepository">The repository used to manage runbooks.</param>
/// <param name="validator">The validator used to validate the update command.</param>
public sealed class UpdateRunbookHandler(
    IRunbookRepository runbookRepository,
    IValidator<UpdateRunbookCommand> validator)
{
    /// <summary>
    /// Handles the update of a runbook.
    /// </summary>
    /// <param name="command">The command containing the update information.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The updated <see cref="Runbook"/>.</returns>
    public async Task<Runbook> HandleAsync(
        UpdateRunbookCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(
            command,
            cancellationToken);

        var runbook = await runbookRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (runbook is null)
        {
            throw new RunbookNotFoundException(command.Id);
        }

        runbook.Update(
            command.Title,
            command.Description,
            command.Service,
            command.Content);

        await runbookRepository.UpdateAsync(
            runbook,
            cancellationToken);

        return runbook;
    }
}