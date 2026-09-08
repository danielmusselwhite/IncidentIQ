using FluentValidation;
using FluentValidation.Results;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using Moq;

namespace IncidentIQ.Application.Tests.Runbooks.RetrieveChunks;

public sealed class RetrieveRunbookChunksHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenQueryIsValid_GeneratesEmbeddingAndRetrievesChunks()
    {
        // Arrange
        var query = new RetrieveRunbookChunksQuery(
            Query: "payment gateway timeout",
            Service: "Payments",
            TopK: 5);

        IReadOnlyList<float> embedding = [0.1f, 0.2f, 0.3f];

        IReadOnlyList<RunbookChunkMatch> expectedMatches =
        [
            new(
                Guid.NewGuid(),
                0,
                "Payment Gateway Recovery",
                "Payments",
                "Check the external payment gateway...",
                0.12)
        ];

        var embeddingGenerator = new Mock<IEmbeddingGenerator>();
        var retriever = new Mock<IRunbookChunkRetriever>();
        var validator = new Mock<IValidator<RetrieveRunbookChunksQuery>>();

        validator
            .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        embeddingGenerator
            .Setup(x => x.GenerateAsync(query.Query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        retriever
            .Setup(x => x.RetrieveAsync(
                embedding,
                query.Service,
                query.TopK,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMatches);

        var handler = new RetrieveRunbookChunksHandler(
            embeddingGenerator.Object,
            retriever.Object,
            validator.Object);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.Equal(expectedMatches, result);

        embeddingGenerator.Verify(
            x => x.GenerateAsync(
                query.Query,
                It.IsAny<CancellationToken>()),
            Times.Once);

        retriever.Verify(
            x => x.RetrieveAsync(
                embedding,
                "Payments",
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_DoesNotGenerateEmbeddingOrRetrieveChunks()
    {
        // Arrange
        var query = new RetrieveRunbookChunksQuery(
            Query: "",
            Service: null,
            TopK: 5);

        var embeddingGenerator = new Mock<IEmbeddingGenerator>();
        var retriever = new Mock<IRunbookChunkRetriever>();
        var validator = new RetrieveRunbookChunksValidator();

        var handler = new RetrieveRunbookChunksHandler(
            embeddingGenerator.Object,
            retriever.Object,
            validator);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(query));

        embeddingGenerator.Verify(
            x => x.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        retriever.Verify(
            x => x.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    public sealed class RetrieveRunbookChunksValidatorTests
    {
        private readonly RetrieveRunbookChunksValidator _validator = new();

        [Theory]
        [InlineData(0)]
        [InlineData(21)]
        public async Task ValidateAsync_WhenTopKIsOutsideAllowedRange_IsInvalid(int topK)
        {
            var query = new RetrieveRunbookChunksQuery(
                "payment gateway timeout",
                null,
                topK);

            var result = await _validator.ValidateAsync(query);

            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(20)]
        public async Task ValidateAsync_WhenTopKIsWithinAllowedRange_IsValid(int topK)
        {
            var query = new RetrieveRunbookChunksQuery(
                "payment gateway timeout",
                null,
                topK);

            var result = await _validator.ValidateAsync(query);

            Assert.True(result.IsValid);
        }
    }
}