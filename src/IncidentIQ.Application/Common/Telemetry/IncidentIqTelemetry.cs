using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace IncidentIQ.Application.Common.Telemetry;

public static class IncidentIqTelemetry
{
    public const string ActivitySourceName =
        "IncidentIQ";

    public const string MeterName =
        "IncidentIQ";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);

    public static readonly Meter Meter =
        new(MeterName);

    public static readonly Histogram<double> QueueWaitDuration =
        Meter.CreateHistogram<double>(
            "incident.analysis.queue_wait.duration",
            "ms",
            "Time between an analysis command being queued and processing starting.");

    public static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>(
            "incident.analysis.processing.duration",
            "ms",
            "Time spent processing an analysis command.");

    public static readonly Histogram<double> AiDuration =
        Meter.CreateHistogram<double>(
            "incident.analysis.ai.duration",
            "ms",
            "Time spent generating the AI incident analysis.");

    public static readonly Counter<long> AnalysisFailures =
        Meter.CreateCounter<long>(
            "incident.analysis.failures",
            description:
                "Number of incident analyses that exhausted all retry attempts.");

    public static readonly Counter<long> AnalysisRetries =
        Meter.CreateCounter<long>(
            "incident.analysis.retries",
            description:
                "Number of administrator-triggered incident analysis retries.");
}