using System;
using System.Collections.Generic;
using System.Text;

namespace IncidentIQ.Application.Analyse;

/// <summary>
/// Represents a recommended action for an incident along with the confidence level.
/// </summary>
public sealed record RecommendedAction(
    string Action
);