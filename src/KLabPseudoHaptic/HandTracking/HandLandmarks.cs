namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// A single hand's detection result: 21 landmarks plus metadata.
/// </summary>
public sealed class HandLandmarks
{
    /// <summary>Creates a new instance.</summary>
    /// <param name="points">
    /// Exactly <see cref="HandLandmarkIndex.Count"/> landmarks in the order defined by
    /// <see cref="HandLandmarkIndex"/>.
    /// </param>
    /// <param name="handedness">Which hand the detection corresponds to.</param>
    /// <param name="score">Confidence score in the range [0, 1].</param>
    /// <param name="timestamp">Timestamp from the source frame.</param>
    public HandLandmarks(
        IReadOnlyList<HandLandmark> points,
        Handedness handedness,
        float score,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count != HandLandmarkIndex.Count)
        {
            throw new ArgumentException(
                $"Expected {HandLandmarkIndex.Count} landmarks, got {points.Count}.",
                nameof(points));
        }

        Points = points;
        Handedness = handedness;
        Score = score;
        Timestamp = timestamp;
    }

    /// <summary>The 21 landmarks in MediaPipe order.</summary>
    public IReadOnlyList<HandLandmark> Points { get; }

    /// <summary>Which hand this detection corresponds to.</summary>
    public Handedness Handedness { get; }

    /// <summary>Detection confidence in [0, 1].</summary>
    public float Score { get; }

    /// <summary>Source frame timestamp.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Indexed landmark access.</summary>
    public HandLandmark this[int index] => Points[index];
}
