namespace IncidentIQ.Application.Runbooks.Index;

/// <summary>
/// Builds the stable natural-language representation used when embedding
/// Runbook chunks for semantic retrieval.
/// </summary>
public static class RunbookEmbeddingTextBuilder
{
    public static string Build(
        string title,
        string service,
        string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return $"""
            Runbook: {title}
            Service: {service}

            {content}
            """;
    }
}