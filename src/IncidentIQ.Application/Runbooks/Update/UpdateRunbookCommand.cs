namespace IncidentIQ.Application.Runbooks.Update;

/// <summary>
/// Represents a command to update a runbook.
/// </summary>
/// <param name="Id">The unique identifier of the runbook to be updated.</param>
/// <param name="Title">The title of the runbook.</param>
/// <param name="Description">The description of the runbook.</param>
/// <param name="Service">The service associated with the runbook.</param>
/// <param name="Content">The content of the runbook.</param>
public sealed record UpdateRunbookCommand(
    Guid Id,
    string Title,
    string Description,
    string Service,
    string Content);