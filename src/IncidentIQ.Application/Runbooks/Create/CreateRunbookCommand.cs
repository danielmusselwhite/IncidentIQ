namespace IncidentIQ.Application.Runbooks.Create;

/// <summary>
/// Represents a command to create a runbook.
/// (What the application is being asked to do.)
/// </summary>
/// <param name="Title">The title of the runbook.</param>
/// <param name="Description">The description of the runbook.</param>
/// <param name="Service">The service associated with the runbook.</param>
/// <param name="Content">The content of the runbook.</param>
public sealed record CreateRunbookCommand(
    string Title,
    string Description,
    string Service,
    string Content);