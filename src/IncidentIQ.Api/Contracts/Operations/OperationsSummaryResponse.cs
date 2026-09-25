namespace IncidentIQ.Api.Contracts.Operations;

public sealed record OperationsSummaryResponse(
    int Queued,
    int Processing,
    int Completed,
    int Failed,
    int Total);