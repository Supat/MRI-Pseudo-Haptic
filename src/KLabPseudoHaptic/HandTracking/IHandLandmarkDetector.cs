namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// Produces a stream of <see cref="HandLandmarks"/> detections from some source
/// (camera, video file, network feed, ...).
/// </summary>
public interface IHandLandmarkDetector : IAsyncDisposable
{
    /// <summary>
    /// Asynchronously yields hand detections until the stream ends or
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    IAsyncEnumerable<HandLandmarks> DetectAsync(CancellationToken cancellationToken = default);
}
