using System.Runtime.CompilerServices;
using OpenCvSharp;

namespace KLabPseudoHaptic.HandTracking.Onnx;

/// <summary>
/// <see cref="IHandLandmarkDetector"/> implementation that uses ONNX Runtime to
/// run the MediaPipe palm detection and hand landmark models natively in .NET,
/// with camera capture via OpenCvSharp. No Python installation is required.
/// </summary>
/// <remarks>
/// The detector implements the standard two-stage MediaPipe Hands pipeline:
/// <list type="number">
///   <item>A lightweight SSD palm detector locates hands in the full frame.</item>
///   <item>An affine-aligned crop of each detected palm is fed to the hand
///         landmark model, which produces 21 3D keypoints.</item>
/// </list>
/// Both ONNX models must be downloaded separately; see
/// <c>tools/download_models.sh</c>.
/// </remarks>
public sealed class OnnxHandDetector : IHandLandmarkDetector
{
    private readonly OnnxHandDetectorOptions _options;
    private bool _disposed;

    /// <summary>Creates a new detector with the given options.</summary>
    public OnnxHandDetector(OnnxHandDetectorOptions? options = null)
    {
        _options = options ?? new OnnxHandDetectorOptions();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<HandLandmarks> DetectAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var palmDetector = new PalmDetector(
            _options.PalmDetectorModelPath,
            _options.MinDetectionConfidence,
            _options.NmsIouThreshold);

        using var landmarkExtractor = new HandLandmarkExtractor(
            _options.HandLandmarkModelPath,
            _options.MinLandmarkConfidence);

        using var capture = new VideoCapture(_options.CameraIndex);
        if (!capture.IsOpened())
        {
            throw new InvalidOperationException(
                $"Failed to open camera at index {_options.CameraIndex}.");
        }

        using var frame = new Mat();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!capture.Read(frame) || frame.Empty())
            {
                // Allow cancellation checks between failed reads.
                await Task.Yield();
                continue;
            }

            // Preprocess for the palm detector: resize to 192 x 192, BGR → RGB,
            // normalise to [0, 1].
            var inputData = PreprocessForPalmDetector(frame);

            var palms = palmDetector.Detect(inputData);
            if (palms.Count == 0)
            {
                await Task.Yield();
                continue;
            }

            // Extract landmarks for the first (highest-scoring) palm.
            var landmarks = landmarkExtractor.Extract(frame, palms[0]);
            if (landmarks is not null)
            {
                yield return landmarks;
            }
            else
            {
                await Task.Yield();
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    private static float[] PreprocessForPalmDetector(Mat bgrFrame)
    {
        using var resized = new Mat();
        Cv2.Resize(bgrFrame, resized,
            new Size(PalmDetector.InputSize, PalmDetector.InputSize));

        using var rgb = new Mat();
        Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

        int total = PalmDetector.InputSize * PalmDetector.InputSize * 3;
        var data = new float[total];
        rgb.GetArray(out byte[] raw);
        for (int i = 0; i < raw.Length; i++)
        {
            data[i] = raw[i] / 255f;
        }

        return data;
    }
}
