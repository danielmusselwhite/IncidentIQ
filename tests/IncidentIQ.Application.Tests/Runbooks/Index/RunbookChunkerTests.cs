using IncidentIQ.Application.Runbooks.Index;

namespace IncidentIQ.Application.Tests.Runbooks.Index;

public sealed class RunbookChunkerTests
{
    private readonly RunbookChunker _chunker = new();

    [Fact]
    public void Chunk_WhenContentIsShort_ShouldReturnSingleChunk()
    {
        const string content =
            "Check the payment gateway health endpoint and review recent timeout telemetry.";

        var chunks = _chunker.Chunk(content);

        var chunk = Assert.Single(chunks);

        Assert.Equal(content, chunk);
    }

    [Fact]
    public void Chunk_WhenContentIsLong_ShouldReturnMultipleChunks()
    {
        var content = string.Join(
            "\n\n",
            Enumerable.Range(1, 100)
                .Select(index =>
                    $"Step {index}: Review the service telemetry, dependency health and recent application logs before continuing with recovery."));

        var chunks = _chunker.Chunk(content);

        Assert.True(chunks.Count > 1);

        Assert.All(
            chunks,
            chunk => Assert.False(string.IsNullOrWhiteSpace(chunk)));
    }

    [Fact]
    public void Chunk_WhenContentIsWhitespace_ShouldReturnNoChunks()
    {
        var chunks = _chunker.Chunk("   \r\n   \r\n   ");

        Assert.Empty(chunks);
    }

    [Fact]
    public void Chunk_WhenCalledWithSameContent_ShouldReturnSameChunks()
    {
        var content = string.Join(
            "\n\n",
            Enumerable.Range(1, 100)
                .Select(index =>
                    $"Recovery step {index}: Investigate the affected service and verify its dependencies."));

        var firstResult = _chunker.Chunk(content);
        var secondResult = _chunker.Chunk(content);

        Assert.Equal(firstResult, secondResult);
    }

    [Fact]
    public void Chunk_WhenContentIsLong_ShouldPreserveOverlapBetweenChunks()
    {
        var content = string.Join(
            " ",
            Enumerable.Range(1, 1000)
                .Select(index => $"word-{index}"));

        var chunks = _chunker.Chunk(content);

        Assert.True(chunks.Count > 1);

        var firstChunkWords = chunks[0].Split(' ');
        var secondChunkWords = chunks[1].Split(' ');

        Assert.Contains(
            firstChunkWords,
            word => secondChunkWords.Contains(word));
    }
}