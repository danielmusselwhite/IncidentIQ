using IncidentIQ.Evaluation.Data;

var dataDirectory =
    Path.Combine(
        AppContext.BaseDirectory,
        "Data");

try
{
    var dataset =
        await EvaluationDatasetLoader.LoadAsync(
            dataDirectory);

    Console.WriteLine("IncidentIQ evaluation dataset loaded successfully.");

    Console.WriteLine($"Historical Incidents: {dataset.HistoricalIncidents.Count}");

    Console.WriteLine($"Runbooks: {dataset.Runbooks.Count}");

    Console.WriteLine($"Evaluation cases: {dataset.Cases.Count}");
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);

    Environment.ExitCode = 1;
}