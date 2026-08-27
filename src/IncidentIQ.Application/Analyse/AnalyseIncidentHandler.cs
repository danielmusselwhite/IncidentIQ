using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Domain.Incidents;

public sealed class AnalyseIncidentHandler(IIncidentRepository incidentRepository)
{
    public async Task HandleAsync(AnalyseIncidentCommand command, CancellationToken cancellationToken = default)
    {
        // first, get the incident we have been messaged to analyse
        var incident = await incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);
        if (incident is null) throw new InvalidOperationException($"Incident '{command.IncidentId}' could not be found.");

        // basic state-based idempotency: (if incident has already been processed, return and treat as a no-op so worker can safely complete it, prevents same work from being done multiple times)
        if (incident.Status == IncidentStatus.Completed) return;

        #region TODO - Temporary placeholder. Actual AI analysis will replace this later.
        // now do the actual processing
        incident.StartProcessingAttempt();
        await incidentRepository.UpdateAsync(incident, cancellationToken);

        Thread.Sleep(5000); // just sleep so we can see the processing state in the UI

        incident.MarkCompleted();
        await incidentRepository.UpdateAsync(incident, cancellationToken);
        #endregion

    }
}