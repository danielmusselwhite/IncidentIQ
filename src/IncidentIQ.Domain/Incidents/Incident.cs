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

    public DateTimeOffset? ProcessingStartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset? LastAttemptAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Incident"/> class with the specified details.
    /// </summary>
    /// <param name="id">The unique identifier of the incident.</param>
    /// <param name="title">The title of the incident.</param>
    /// <param name="description">The description of the incident.</param>
    /// <param name="service">The service affected by the incident.</param>
    /// <param name="environment">The environment in which the incident occurred.</param>
    /// <param name="severity">The severity of the incident.</param>
    /// <param name="symptoms">The symptoms of the incident.</param>
    /// <param name="createdAt">The date and time when the incident was created.</param>
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

    /// <summary>
    /// Restores an incident from the specified details, typically used for reconstructing incidents from a data source.
    /// </summary>
    /// <param name="id">The unique identifier of the incident.</param>
    /// <param name="title">The title of the incident.</param>
    /// <param name="description">The description of the incident.</param>
    /// <param name="service">The service affected by the incident.</param>
    /// <param name="environment">The environment in which the incident occurred.</param>
    /// <param name="severity">The severity of the incident.</param>
    /// <param name="symptoms">The symptoms of the incident.</param>
    /// <param name="status">The status of the incident.</param>
    /// <param name="createdAt">The date and time when the incident was created.</param>
    /// <param name="updatedAt">The date and time when the incident was last updated.</param>
    /// <returns>The restored incident.</returns>
    public static Incident Restore(
    string id,
    string title,
    string description,
    string service,
    string environment,
    IncidentSeverity severity,
    string? symptoms,
    IncidentStatus status,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt,
    DateTimeOffset? processingStartedAt,
    DateTimeOffset? completedAt,
    string? failureReason,
    DateTimeOffset? failedAt,
    int attemptCount,
    DateTimeOffset? lastAttemptAt)
    {
        var incident = new Incident(
            id,
            title,
            description,
            service,
            environment,
            severity,
            symptoms,
            createdAt)
        {
            Status = status,
            UpdatedAt = updatedAt,
            ProcessingStartedAt = processingStartedAt,
            CompletedAt = completedAt,
            FailureReason = failureReason,
            FailedAt = failedAt,
            AttemptCount = attemptCount,
            LastAttemptAt = lastAttemptAt
        };

        return incident;
    }

    #region Status Transitions
    /// <summary>
    /// Records the start of an analysis processing attempt.
    ///
    /// The first attempt moves the Incident from Queued to Processing.
    /// Subsequent attempts are allowed while the Incident is already Processing.
    /// </summary>
    public void StartProcessingAttempt()
    {
        if (Status is not IncidentStatus.Queued and not IncidentStatus.Processing) throw new InvalidOperationException($"Cannot process an incident with status '{Status}'.");
        
        var now = DateTimeOffset.UtcNow;

        if (Status == IncidentStatus.Queued)
        {
            Status = IncidentStatus.Processing;
            ProcessingStartedAt = now;
        }

        AttemptCount++;
        LastAttemptAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Marks the incident analysis as successfully completed.
    /// </summary>
    public void MarkCompleted()
    {
        if (Status != IncidentStatus.Processing) throw new InvalidOperationException($"Incident cannot move from {Status} to Completed.");

        var now = DateTimeOffset.UtcNow;

        Status = IncidentStatus.Completed;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(string failureReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        if (Status != IncidentStatus.Processing) throw new InvalidOperationException($"Incident cannot move from {Status} to Failed.");

        var now = DateTimeOffset.UtcNow;

        Status = IncidentStatus.Failed;
        FailureReason = failureReason;
        FailedAt = now;
        UpdatedAt = now;
    }

    public void ResetForRetry()
    {
        if (Status != IncidentStatus.Failed) throw new InvalidOperationException($"Incident cannot be reset for retry unless it is in Failed status. Current status: {Status}.");
        
        Status = IncidentStatus.Queued;
        FailureReason = null;
        FailedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        AttemptCount = 0;
        LastAttemptAt = null;
        ProcessingStartedAt = null;
        CompletedAt = null;
    }
    #endregion
}