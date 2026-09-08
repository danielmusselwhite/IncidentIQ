using System;
using System.Collections.Generic;
using System.Text;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.Analyse;

/// <summary>
/// Represents the input required for analyzing an incident.
/// </summary>
public sealed record IncidentAnalysisInput(
    string Title,
    string Description,
    string Service,
    string Environment,
    string? Symptoms,
    IncidentSeverity Severity
);
