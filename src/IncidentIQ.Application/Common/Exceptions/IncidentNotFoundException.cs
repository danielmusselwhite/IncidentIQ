namespace IncidentIQ.Application.Common.Exceptions;

public sealed class IncidentNotFoundException(string incidentId)
    : Exception($"Incident '{incidentId}' was not found.");