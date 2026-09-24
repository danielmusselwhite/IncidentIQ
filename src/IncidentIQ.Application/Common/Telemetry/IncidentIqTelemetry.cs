using System.Diagnostics;

namespace IncidentIQ.Application.Common.Telemetry;

public static class IncidentIqTelemetry
{
    public const string ActivitySourceName =
        "IncidentIQ";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);
}