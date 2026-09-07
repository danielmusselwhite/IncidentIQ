using System;
using System.Collections.Generic;
using System.Text;

namespace IncidentIQ.Application.Incidents.Analyse;

/// <summary>
/// Represents a likely cause of an incident along with the confidence level.
/// </summary>
public sealed record LikelyCause(
    string Cause,
    double Confidence
);