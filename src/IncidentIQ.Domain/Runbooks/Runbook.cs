namespace IncidentIQ.Domain.Runbooks;

/// <summary>
/// Represents a runbook in the IncidentIQ domain.
/// </summary>
public sealed class Runbook
{
    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Service { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private Runbook()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Runbook"/> class with the specified properties.
    /// </summary>
    /// <param name="id">The unique identifier of the runbook.</param>
    /// <param name="title">The title of the runbook.</param>
    /// <param name="description">The description of the runbook.</param>
    /// <param name="service">The service associated with the runbook.</param>
    /// <param name="content">The content of the runbook.</param>
    /// <param name="createdAt">The date and time when the runbook was created.</param>
    /// <param name="updatedAt">The date and time when the runbook was last updated.</param>
    private Runbook(
        Guid id,
        string title,
        string description,
        string service,
        string content,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Service = service;
        Content = content;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Runbook"/> class with the specified properties.
    /// </summary>
    /// <param name="title">The title of the runbook.</param>
    /// <param name="description">The description of the runbook.</param>
    /// <param name="service">The service associated with the runbook.</param>
    /// <param name="content">The content of the runbook.</param>
    /// <returns>A new instance of the <see cref="Runbook"/> class.</returns>
    public static Runbook Create(
        string title,
        string description,
        string service,
        string content)
    {
        var now = DateTime.UtcNow;

        return new Runbook(
            Guid.NewGuid(),
            title,
            description,
            service,
            content,
            now,
            now);
    }
    /// <summary>
    /// Restores an existing instance of the <see cref="Runbook"/> class with the specified properties.
    /// </summary>
    /// <param name="id">The unique identifier of the runbook.</param>
    /// <param name="title">The title of the runbook.</param>
    /// <param name="description">The description of the runbook.</param>
    /// <param name="service">The service associated with the runbook.</param>
    /// <param name="content">The content of the runbook.</param>
    /// <param name="createdAt">The date and time when the runbook was created.</param>
    /// <param name="updatedAt">The date and time when the runbook was last updated.</param>
    /// <returns>An existing instance of the <see cref="Runbook"/> class.</returns>
    public static Runbook Restore(
        Guid id,
        string title,
        string description,
        string service,
        string content,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Runbook(
            id,
            title,
            description,
            service,
            content,
            createdAt,
            updatedAt);
    }

    /// <summary>
    /// Updates the properties of the runbook with the specified values and sets the updated timestamp to the current UTC time.
    /// </summary>
    /// <param name="title">The title of the runbook.</param>
    /// <param name="description">The description of the runbook.</param>
    /// <param name="service">The service associated with the runbook.</param>
    /// <param name="content">The content of the runbook.</param>
    public void Update(
        string title,
        string description,
        string service,
        string content)
    {
        Title = title;
        Description = description;
        Service = service;
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }
}