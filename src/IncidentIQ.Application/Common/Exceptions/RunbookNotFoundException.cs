namespace IncidentIQ.Application.Common.Exceptions;

public sealed class RunbookNotFoundException(Guid runbookId)
    : Exception($"Runbook '{runbookId}' was not found.");