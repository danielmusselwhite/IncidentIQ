using IncidentIQ.Evaluation.Models;

namespace IncidentIQ.Evaluation.Data;

/// <summary>
/// Validates structural consistency and referential integrity within the
/// controlled IncidentIQ evaluation dataset.
/// </summary>
public static class EvaluationDatasetValidator
{
    /// <summary>
    /// Validates the supplied evaluation dataset and throws when one or more
    /// consistency problems are found.
    /// </summary>
    public static void Validate(
        EvaluationDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        var errors = new List<string>();

        ValidateHistoricalIncidents(
            dataset.HistoricalIncidents,
            errors);

        ValidateRunbooks(
            dataset.Runbooks,
            errors);

        ValidateCases(
            dataset,
            errors);

        if (errors.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Evaluation dataset validation failed:"
            + Environment.NewLine
            + string.Join(
                Environment.NewLine,
                errors.Select(error => $"- {error}")));
    }

    private static void ValidateHistoricalIncidents(
        IReadOnlyList<EvaluationHistoricalIncident> incidents,
        ICollection<string> errors)
    {
        var duplicateIds =
            incidents
                .GroupBy(incident => incident.IncidentId)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

        foreach (var duplicateId in duplicateIds)
        {
            errors.Add(
                $"Historical Incident ID '{duplicateId}' is duplicated.");
        }

        foreach (var incident in incidents)
        {
            if (incident.IncidentId == Guid.Empty)
            {
                errors.Add(
                    "Historical Incident contains an empty IncidentId.");
            }

            if (string.IsNullOrWhiteSpace(incident.Title))
            {
                errors.Add(
                    $"Historical Incident '{incident.IncidentId}' has no title.");
            }

            if (string.IsNullOrWhiteSpace(incident.Description))
            {
                errors.Add(
                    $"Historical Incident '{incident.IncidentId}' has no description.");
            }

            if (string.IsNullOrWhiteSpace(incident.Service))
            {
                errors.Add(
                    $"Historical Incident '{incident.IncidentId}' has no service.");
            }

            if (string.IsNullOrWhiteSpace(incident.Environment))
            {
                errors.Add(
                    $"Historical Incident '{incident.IncidentId}' has no environment.");
            }
        }
    }

    private static void ValidateRunbooks(
        IReadOnlyList<EvaluationRunbook> runbooks,
        ICollection<string> errors)
    {
        var duplicateIds =
            runbooks
                .GroupBy(runbook => runbook.RunbookId)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

        foreach (var duplicateId in duplicateIds)
        {
            errors.Add(
                $"Runbook ID '{duplicateId}' is duplicated.");
        }

        foreach (var runbook in runbooks)
        {
            if (runbook.RunbookId == Guid.Empty)
            {
                errors.Add(
                    "Runbook contains an empty RunbookId.");
            }

            if (string.IsNullOrWhiteSpace(runbook.Title))
            {
                errors.Add(
                    $"Runbook '{runbook.RunbookId}' has no title.");
            }

            if (string.IsNullOrWhiteSpace(runbook.Description))
            {
                errors.Add(
                    $"Runbook '{runbook.RunbookId}' has no description.");
            }

            if (string.IsNullOrWhiteSpace(runbook.Service))
            {
                errors.Add(
                    $"Runbook '{runbook.RunbookId}' has no service.");
            }

            if (string.IsNullOrWhiteSpace(runbook.Content))
            {
                errors.Add(
                    $"Runbook '{runbook.RunbookId}' has no content.");
            }
        }
    }

    private static void ValidateCases(
        EvaluationDataset dataset,
        ICollection<string> errors)
    {
        var duplicateCaseIds =
            dataset.Cases
                .GroupBy(
                    evaluationCase => evaluationCase.Id,
                    StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

        foreach (var duplicateId in duplicateCaseIds)
        {
            errors.Add(
                $"Evaluation case ID '{duplicateId}' is duplicated.");
        }

        var historicalIncidentIds =
            dataset.HistoricalIncidents
                .Select(incident => incident.IncidentId)
                .ToHashSet();

        var runbookIds =
            dataset.Runbooks
                .Select(runbook => runbook.RunbookId)
                .ToHashSet();

        foreach (var evaluationCase in dataset.Cases)
        {
            ValidateCaseShape(
                evaluationCase,
                errors);

            ValidateExpectedEvidence(
                evaluationCase,
                historicalIncidentIds,
                runbookIds,
                errors);

            ValidateExpectedEvidenceAgainstFilters(
                evaluationCase,
                dataset,
                errors);
        }
    }

    private static void ValidateCaseShape(
        EvaluationCase evaluationCase,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(evaluationCase.Id))
        {
            errors.Add(
                "Evaluation case contains an empty ID.");

            return;
        }

        if (string.IsNullOrWhiteSpace(evaluationCase.Name))
        {
            errors.Add(
                $"Evaluation case '{evaluationCase.Id}' has no name.");
        }

        switch (evaluationCase.ScenarioType)
        {
            case EvaluationScenarioType.OperationalQuestion:
                if (string.IsNullOrWhiteSpace(
                        evaluationCase.Input.Question))
                {
                    errors.Add(
                        $"OperationalQuestion case '{evaluationCase.Id}' must contain a question.");
                }

                if (evaluationCase.Input.Incident is not null)
                {
                    errors.Add(
                        $"OperationalQuestion case '{evaluationCase.Id}' must not contain Incident input.");
                }

                break;

            case EvaluationScenarioType.IncidentAnalysis:
                if (evaluationCase.Input.Incident is null)
                {
                    errors.Add(
                        $"IncidentAnalysis case '{evaluationCase.Id}' must contain Incident input.");
                }

                if (!string.IsNullOrWhiteSpace(
                        evaluationCase.Input.Question))
                {
                    errors.Add(
                        $"IncidentAnalysis case '{evaluationCase.Id}' must not contain a question.");
                }

                break;

            default:
                errors.Add(
                    $"Evaluation case '{evaluationCase.Id}' has unsupported scenario type '{evaluationCase.ScenarioType}'.");
                break;
        }
    }

    private static void ValidateExpectedEvidence(
        EvaluationCase evaluationCase,
        IReadOnlySet<Guid> historicalIncidentIds,
        IReadOnlySet<Guid> runbookIds,
        ICollection<string> errors)
    {
        foreach (var incidentId in
                 evaluationCase.ExpectedEvidence
                     .HistoricalIncidentIds)
        {
            if (!historicalIncidentIds.Contains(incidentId))
            {
                errors.Add(
                    $"Evaluation case '{evaluationCase.Id}' references unknown historical Incident '{incidentId}'.");
            }
        }

        foreach (var runbookId in
                 evaluationCase.ExpectedEvidence
                     .RunbookIds)
        {
            if (!runbookIds.Contains(runbookId))
            {
                errors.Add(
                    $"Evaluation case '{evaluationCase.Id}' references unknown Runbook '{runbookId}'.");
            }
        }

        if (evaluationCase.ExpectedEvidence
                .HistoricalIncidentIds
                .Distinct()
                .Count()
            != evaluationCase.ExpectedEvidence
                .HistoricalIncidentIds
                .Count)
        {
            errors.Add(
                $"Evaluation case '{evaluationCase.Id}' contains duplicate historical Incident expectations.");
        }

        if (evaluationCase.ExpectedEvidence
                .RunbookIds
                .Distinct()
                .Count()
            != evaluationCase.ExpectedEvidence
                .RunbookIds
                .Count)
        {
            errors.Add(
                $"Evaluation case '{evaluationCase.Id}' contains duplicate Runbook expectations.");
        }
    }

    private static void ValidateExpectedEvidenceAgainstFilters(
        EvaluationCase evaluationCase,
        EvaluationDataset dataset,
        ICollection<string> errors)
    {
        var service =
            evaluationCase.ScenarioType switch
            {
                EvaluationScenarioType.OperationalQuestion =>
                    evaluationCase.Input.ServiceFilter,

                EvaluationScenarioType.IncidentAnalysis =>
                    evaluationCase.Input.Incident?.Service,

                _ => null
            };

        var environment =
            evaluationCase.ScenarioType switch
            {
                EvaluationScenarioType.OperationalQuestion =>
                    evaluationCase.Input.EnvironmentFilter,

                EvaluationScenarioType.IncidentAnalysis =>
                    evaluationCase.Input.Incident?.Environment,

                _ => null
            };

        foreach (var expectedIncidentId in
                 evaluationCase.ExpectedEvidence
                     .HistoricalIncidentIds)
        {
            var incident =
                dataset.HistoricalIncidents
                    .FirstOrDefault(
                        candidate =>
                            candidate.IncidentId ==
                            expectedIncidentId);

            if (incident is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(service)
                && !string.Equals(
                    incident.Service,
                    service,
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"Evaluation case '{evaluationCase.Id}' expects historical Incident '{expectedIncidentId}', "
                    + $"but its service '{incident.Service}' does not match filter '{service}'.");
            }

            if (!string.IsNullOrWhiteSpace(environment)
                && !string.Equals(
                    incident.Environment,
                    environment,
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"Evaluation case '{evaluationCase.Id}' expects historical Incident '{expectedIncidentId}', "
                    + $"but its environment '{incident.Environment}' does not match filter '{environment}'.");
            }
        }

        foreach (var expectedRunbookId in
                 evaluationCase.ExpectedEvidence
                     .RunbookIds)
        {
            var runbook =
                dataset.Runbooks
                    .FirstOrDefault(
                        candidate =>
                            candidate.RunbookId ==
                            expectedRunbookId);

            if (runbook is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(service)
                && !string.Equals(
                    runbook.Service,
                    service,
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"Evaluation case '{evaluationCase.Id}' expects Runbook '{expectedRunbookId}', "
                    + $"but its service '{runbook.Service}' does not match filter '{service}'.");
            }
        }
    }
}