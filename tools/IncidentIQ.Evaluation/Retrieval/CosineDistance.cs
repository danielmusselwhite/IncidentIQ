namespace IncidentIQ.Evaluation.Retrieval;

/// <summary>
/// Use cosine to calculate the distance between two embedding vectors.
/// The result is a value between 0 and 2, where 0 indicates that the vectors are identical, and 2 indicates that they are diametrically opposed.
/// We use cosine because it is a common metric for measuring the similarity between high-dimensional vectors, such as those produced by machine learning models for text embeddings.
/// </summary>
public static class CosineDistance
{
    public static double Calculate(
        IReadOnlyList<float> left,
        IReadOnlyList<float> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Count != right.Count)
        {
            throw new ArgumentException(
                "Embedding vectors must have the same dimensions.");
        }

        if (left.Count == 0)
        {
            throw new ArgumentException(
                "Embedding vectors cannot be empty.");
        }

        double dotProduct = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var index = 0;
             index < left.Count;
             index++)
        {
            dotProduct +=
                left[index] * right[index];

            leftMagnitude +=
                left[index] * left[index];

            rightMagnitude +=
                right[index] * right[index];
        }

        if (leftMagnitude == 0 ||
            rightMagnitude == 0)
        {
            throw new ArgumentException(
                "Embedding vectors cannot have zero magnitude.");
        }

        var similarity =
            dotProduct /
            (Math.Sqrt(leftMagnitude) *
             Math.Sqrt(rightMagnitude));

        return 1 - similarity;
    }
}