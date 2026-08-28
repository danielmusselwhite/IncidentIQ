namespace IncidentIQ.Application.Common.Exceptions;

public sealed class IncidentNotRetryableException(string incidentId)
    : Exception($"Incident '{incidentId}' is not in a retryable state.");