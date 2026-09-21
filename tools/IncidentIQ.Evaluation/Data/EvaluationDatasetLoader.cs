using IncidentIQ.Evaluation.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IncidentIQ.Evaluation.Data;

/// <summary>
/// Loads the version-controlled IncidentIQ evaluation dataset from JSON files.
/// </summary>
public static class EvaluationDatasetLoader
{
    private const string HistoricalIncidentsFileName =
        "historical-incidents.json";

    private const string RunbooksFileName =
        "runbooks.json";

    private const string EvaluationCasesFileName =
        "evaluation-cases.json";

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    /// <summary>
    /// Loads and validates the complete evaluation dataset.
    /// </summary>
    /// <param name="dataDirectory">
    /// Directory containing the evaluation JSON files.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel file loading.
    /// </param>
    public static async Task<EvaluationDataset> LoadAsync(
        string dataDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

        if (!Directory.Exists(dataDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Evaluation data directory was not found: {dataDirectory}");
        }

        var historicalIncidents =
            await LoadFileAsync<EvaluationHistoricalIncident>(
                Path.Combine(
                    dataDirectory,
                    HistoricalIncidentsFileName),
                cancellationToken);

        var runbooks =
            await LoadFileAsync<EvaluationRunbook>(
                Path.Combine(
                    dataDirectory,
                    RunbooksFileName),
                cancellationToken);

        var evaluationCases =
            await LoadFileAsync<EvaluationCase>(
                Path.Combine(
                    dataDirectory,
                    EvaluationCasesFileName),
                cancellationToken);

        var dataset =
            new EvaluationDataset(
                HistoricalIncidents: historicalIncidents,
                Runbooks: runbooks,
                Cases: evaluationCases);

        EvaluationDatasetValidator.Validate(dataset);

        return dataset;
    }

    private static async Task<IReadOnlyList<T>> LoadFileAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Evaluation data file was not found: {path}",
                path);
        }

        await using var stream = File.OpenRead(path);

        try
        {
            var items =
                await JsonSerializer.DeserializeAsync<List<T>>(
                    stream,
                    SerializerOptions,
                    cancellationToken);

            return items
                ?? throw new InvalidOperationException(
                    $"Evaluation data file '{path}' contained null JSON.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Evaluation data file '{path}' contains invalid JSON.",
                exception);
        }
    }
}