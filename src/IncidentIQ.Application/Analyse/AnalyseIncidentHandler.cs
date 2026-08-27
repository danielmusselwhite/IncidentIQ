using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;

public sealed class AnalyseIncidentHandler(
    IIncidentRepository incidentRepository)
{
    public async Task HandleAsync(
        AnalyseIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        // first, get the incident we have been messaged to analyse
        var incident = await incidentRepository.GetByIdAsync(
            command.IncidentId,
            cancellationToken);

        if (incident is null)
        {
            throw new InvalidOperationException(
                $"Incident '{command.IncidentId}' could not be found.");
        }

        // Now feed it to the AI analysis engine to get the results

        #region TODO - Temporary placeholder. Actual AI analysis will replace this later.
        // do the processing
        incident.StartProcessingAttempt();

        await incidentRepository.UpdateAsync(
            incident,
            cancellationToken);

        Thread.Sleep(5000); // just sleep so we can see the processing state in the UI
        incident.MarkCompleted();
        #endregion

        await incidentRepository.UpdateAsync(
            incident,
            cancellationToken);
    }
}