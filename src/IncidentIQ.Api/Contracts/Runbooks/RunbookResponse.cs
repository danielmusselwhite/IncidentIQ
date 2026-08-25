using IncidentIQ.Domain.Runbooks;

namespace IncidentIQ.Api.Contracts.Runbooks;

/// <summary>
/// Represents a response DTO for a runbook.
/// </summary>
/// <param name="Id">The unique identifier of the runbook.</param>
/// <param name="Title">The title of the runbook.</param>
/// <param name="Description">The description of the runbook.</param>
/// <param name="Service">The service associated with the runbook.</param>
/// <param name="Content">The content of the runbook.</param>
/// <param name="CreatedAt">The date and time when the runbook was created.</param>
/// <param name="UpdatedAt">The date and time when the runbook was last updated.</param>
public sealed record RunbookResponse(
    Guid Id,
    string Title,
    string Description,
    string Service,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{

    /// <summary>
    /// Creates a RunbookResponse from a Runbook domain model.
    /// </summary>
    /// <param name="runbook">The Runbook domain model.</param>
    /// <returns>A RunbookResponse DTO.</returns>
    public static RunbookResponse FromDomain(Runbook runbook)
    {
        return new RunbookResponse(
            runbook.Id,
            runbook.Title,
            runbook.Description,
            runbook.Service,
            runbook.Content,
            runbook.CreatedAt,
            runbook.UpdatedAt);
    }
}