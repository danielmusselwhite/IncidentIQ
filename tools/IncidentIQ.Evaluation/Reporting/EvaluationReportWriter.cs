using System.Text.Json;

namespace IncidentIQ.Evaluation.Reporting;

internal static class EvaluationReportWriter
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            WriteIndented = true
        };

    public static async Task<string> WriteAsync(
        EvaluationReport report,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException(
                "Output directory is required.",
                nameof(outputDirectory));
        }

        Directory.CreateDirectory(outputDirectory);

        var fileName =
            $"evaluation-{report.GeneratedAtUtc:yyyyMMdd-HHmmss}.json";

        var path =
            Path.Combine(
                outputDirectory,
                fileName);

        await using var stream =
            File.Create(path);

        await JsonSerializer.SerializeAsync(
            stream,
            report,
            SerializerOptions,
            cancellationToken);

        return path;
    }
}