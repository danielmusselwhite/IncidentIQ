using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Analyse;
using IncidentIQ.Domain.Incidents;
using IncidentIQ.Application.Analyse.Retry;
using IncidentIQ.Application.Common.Exceptions;

namespace IncidentIQ.Application.Analyse.Retry;

public sealed class RetryAnalyseIncidentHandler(IIncidentRepository incidentRepository, IIncidentSubmissionStore incidentSubmissionStore)
{
    public async Task<Incident> HandleAsync(RetryAnalyseIncidentCommand command, CancellationToken cancellationToken = default)
    {
        // first, get the incident we have been messaged to retry
        var incident = await incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);
        
        // incident can only be retried if it exists and is in a failed state
        if (incident is null) throw new IncidentNotFoundException(command.IncidentId); // exception handling middleware will catch this and return a 404 response
        if (incident.Status is not IncidentStatus.Failed) throw new IncidentNotRetryableException(command.IncidentId); // exception handling middleware will catch this and return a 409 response

        // reset the incidents status to indicate it is ready for a retry
        incident.ResetForRetry();
        
        // generate retryIncidentAnalysisCommand, using this incident's Id, a new correlation and command Id, and the current UTC time
        var analyseIncidentCommand = new AnalyseIncidentCommand(
            Guid.NewGuid(),
            command.IncidentId,
            command.CorrelationId,
            DateTimeOffset.UtcNow);
            
        // now go to the submissionStore to UPDATE the incident AND generate a NEW Outbox in order for the retry to be processed by the system
        return await incidentSubmissionStore.RetryAsync(incident, analyseIncidentCommand, cancellationToken);
    }
}