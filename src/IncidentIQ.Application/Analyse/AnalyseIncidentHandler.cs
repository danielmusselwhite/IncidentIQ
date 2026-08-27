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

        Thread.Sleep(15000); // just sleep so we can see the processing state in the UI

        incident.MarkCompleted();
        await incidentRepository.UpdateAsync(incident, cancellationToken);
        #endregion

    }

    /// <summary>
    /// Marks the specified incident as failed with the given failure reason.
    /// </summary>
    /// <param name="command">The command containing the incident ID to mark as failed.</param>
    /// <param name="failureReason">The reason why the incident is being marked as failed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task MarkFailedAsync(
    AnalyseIncidentCommand command,
    string failureReason,
    CancellationToken cancellationToken = default)
    {
        var incident = await incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);

        if (incident is null)
        {
            return;
        }

        if (incident.Status is IncidentStatus.Completed or IncidentStatus.Failed)
        {
            return;
        }

        incident.MarkFailed(failureReason);
        await incidentRepository.UpdateAsync(incident, cancellationToken);
    }
}