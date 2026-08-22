namespace IncidentIQ.Domain.Incidents;

/// <summary>
/// Represents an incident in the system.
/// </summary>
public sealed class Incident
{
    public string Id { get; }

    public string Title { get; }

    public string Description { get; }

    public string Service { get; }

    public string Environment { get; }

    public IncidentSeverity Severity { get; }

    public string? Symptoms { get; }

    public IncidentStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Incident(
        string id,
        string title,
        string description,
        string service,
        string environment,
        IncidentSeverity severity,
        string? symptoms,
        DateTimeOffset createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Service = service;
        Environment = environment;
        Severity = severity;
        Symptoms = symptoms;

        Status = IncidentStatus.Queued;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new incident with the specified details.
    /// </summary>
    /// <param name="title">The title of the incident.</param>
    /// <param name="description">The description of the incident.</param>
    /// <param name="service">The service affected by the incident.</param>
    /// <param name="environment">The environment in which the incident occurred.</param>
    /// <param name="severity">The severity of the incident.</param>
    /// <param name="symptoms">The symptoms of the incident.</param>
    /// <returns>The newly created incident.</returns>
    public static Incident Create(
        string title,
        string description,
        string service,
        string environment,
        IncidentSeverity severity,
        string? symptoms)
    {
        var now = DateTimeOffset.UtcNow;

        return new Incident(
            Guid.NewGuid().ToString(),
            title,
            description,
            service,
            environment,
            severity,
            symptoms,
            now);
    }
}