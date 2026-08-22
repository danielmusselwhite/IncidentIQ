namespace IncidentIQ.Domain.Incidents;

public sealed class Incident
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string Service { get; init; }

    public required string Environment { get; init; }

    //public required IncidentSeverity Severity { get; init; } // todo -

    public string? Symptoms { get; init; }

    //public IncidentStatus Status { get; private set; } // todo -

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; private set; }
}