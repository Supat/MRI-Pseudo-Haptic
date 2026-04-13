using System.Runtime.CompilerServices;
using OpenCvSharp;

namespace KLabPseudoHaptic.HandTracking.Onnx;

/// <summary>
/// <see cref="IHandLandmarkDetector"/> implementation that uses ONNX Runtime to
/// run the MediaPipe palm detection and hand landmark models natively in .NET.
/// </summary>
/// <remarks>
/// Camera frames are obtained from an <see cref="ICameraSource"/>. When no
/// explicit source is provided via <see cref="OnnxHandDetectorOptions.CameraSource"/>,
/// a default <see cref="OpenCvCameraSource"/> is used. To capture from SphinxSDK
/// hardware, supply a <see cref="SphinxCameraSource"/> instance instead.
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

        // Use the supplied camera source, or fall back to OpenCV.
        var ownsSource = _options.CameraSource is null;
        var source = _options.CameraSource ?? new OpenCvCameraSource(_options.CameraIndex);

        try
        {
            if (!source.IsOpened)
            {
                source.Open();
            }

            using var frame = new Mat();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!source.TryReadFrame(frame))
                {
                    await Task.Yield();
                    continue;
                }

                var inputData = PreprocessForPalmDetector(frame);

                var palms = palmDetector.Detect(inputData);
                if (palms.Count == 0)
                {
                    await Task.Yield();
                    continue;
                }

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
        finally
        {
            if (ownsSource)
            {
                source.Dispose();
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
