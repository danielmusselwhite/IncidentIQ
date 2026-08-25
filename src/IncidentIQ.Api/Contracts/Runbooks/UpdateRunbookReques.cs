namespace IncidentIQ.Api.Contracts.Runbooks;

/// <summary>
/// Represents a request DTO to update a runbook.
/// </summary>
/// <param name="Title">The title of the runbook.</param>
/// <param name="Description">The description of the runbook.</param>
/// <param name="Service">The service associated with the runbook.</param>
/// <param name="Content">The content of the runbook.</param>
public sealed record UpdateRunbookRequest(
    string Title,
    string Description,
    string Service,
    string Content);