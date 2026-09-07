namespace IncidentIQ.Application.Runbooks.Index;

/// <summary>
/// Splits Runbook content into smaller overlapping pieces suitable for
/// embedding generation and vector retrieval.
/// </summary>
public sealed class RunbookChunker
{
    private const int TargetChunkSize = 1500;
    private const int ChunkOverlap = 200;

    /// <summary>
    /// Splits the supplied Runbook content into deterministic, overlapping chunks.
    /// </summary>
    /// <param name="content">The Runbook content to split.</param>
    /// <returns>The ordered collection of generated chunks.</returns>
    public IReadOnlyList<string> Chunk(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        // Normalise line endings so identical text produces identical chunks regardless of the operating system it originated from.
        var normalisedContent = content
            .Replace("\r\n", "\n")
            .Trim();

        var chunks = new List<string>();
        var start = 0;

        while (start < normalisedContent.Length)
        {
            var maximumEnd = Math.Min(
                start + TargetChunkSize,
                normalisedContent.Length);

            var end = maximumEnd;

            // When more content remains, try to finish the chunk at a natural text boundary rather than cutting directly at 1500 characters.
            if (maximumEnd < normalisedContent.Length)
            {
                end = FindPreferredBreak(
                    normalisedContent,
                    start,
                    maximumEnd);
            }

            var chunk = normalisedContent[start..end].Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (end >= normalisedContent.Length)
            {
                break;
            }

            // Move backwards slightly so adjacent chunks share some context.
            var nextStart = Math.Max(start + 1, end - ChunkOverlap);

            // Avoid beginning the next chunk halfway through a word.
            while (nextStart < end &&
                   !char.IsWhiteSpace(normalisedContent[nextStart]))
            {
                nextStart++;
            }

            // Do not preserve leading whitespace in the next chunk.
            while (nextStart < normalisedContent.Length &&
                   char.IsWhiteSpace(normalisedContent[nextStart]))
            {
                nextStart++;
            }

            // Defensive guard to guarantee that each iteration moves forward.
            if (nextStart <= start)
            {
                nextStart = end;
            }

            start = nextStart;
        }

        return chunks;
    }

    /// <summary>
    /// Finds a sensible place to finish a chunk, preferring paragraph,
    /// line and word boundaries in that order.
    /// </summary>
    private static int FindPreferredBreak(
        string content,
        int start,
        int maximumEnd)
    {
        // Avoid producing an unnecessarily tiny chunk simply because a newline
        // happens to occur near the start of the current range.
        var minimumPreferredEnd = start + ((maximumEnd - start) / 2);

        // Prefer ending at a paragraph boundary.
        for (var i = maximumEnd - 1; i > minimumPreferredEnd; i--)
        {
            if (i > 0 &&
                content[i] == '\n' &&
                content[i - 1] == '\n')
            {
                return i;
            }
        }

        // Otherwise prefer a normal line boundary.
        for (var i = maximumEnd - 1; i > minimumPreferredEnd; i--)
        {
            if (content[i] == '\n')
            {
                return i;
            }
        }

        // Finally try to avoid splitting a word.
        for (var i = maximumEnd - 1; i > minimumPreferredEnd; i--)
        {
            if (char.IsWhiteSpace(content[i]))
            {
                return i;
            }
        }

        // If there is no sensible boundary, fall back to the configured size.
        return maximumEnd;
    }
}