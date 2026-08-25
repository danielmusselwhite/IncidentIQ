using System.Text.Json.Serialization;
using IncidentIQ.Domain.Runbooks;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Represents a document for storing runbook data in Cosmos DB, as Cosmos uses "Documents" to store data.
/// </summary>
internal sealed class RunbookDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string Service { get; init; }

    public required string Content { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Creates a <see cref="RunbookDocument"/> from the specified domain <see cref="Runbook"/>.
    /// </summary>
    /// <param name="runbook">The domain runbook to convert.</param>
    /// <returns>The corresponding <see cref="RunbookDocument"/>.</returns>
    public static RunbookDocument FromDomain(Runbook runbook)
    {
        return new RunbookDocument
        {
            Id = runbook.Id.ToString(),
            Title = runbook.Title,
            Description = runbook.Description,
            Service = runbook.Service,
            Content = runbook.Content,
            CreatedAt = runbook.CreatedAt,
            UpdatedAt = runbook.UpdatedAt
        };
    }
    
    /// <summary>
    /// Converts this <see cref="RunbookDocument"/> to its corresponding domain <see cref="Runbook"/>.
    /// </summary>
    /// <returns>The corresponding domain <see cref="Runbook"/>.</returns>
    public Runbook ToDomain()
    {
        return Runbook.Restore(
            Guid.Parse(Id),
            Title,
            Description,
            Service,
            Content,
            CreatedAt,
            UpdatedAt);
    }
}