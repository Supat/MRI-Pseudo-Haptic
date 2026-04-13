using OpenCvSharp;

namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// <see cref="ICameraSource"/> backed by an OpenCvSharp <see cref="VideoCapture"/>.
/// </summary>
public sealed class OpenCvCameraSource : ICameraSource
{
    private readonly int _cameraIndex;
    private VideoCapture? _capture;

    /// <summary>Creates a source targeting the given OpenCV camera index.</summary>
    public OpenCvCameraSource(int cameraIndex = 0)
    {
        _cameraIndex = cameraIndex;
    }

    /// <inheritdoc />
    public bool IsOpened => _capture?.IsOpened() ?? false;

    /// <inheritdoc />
    public void Open()
    {
        _capture?.Dispose();
        _capture = new VideoCapture(_cameraIndex);
        if (!_capture.IsOpened())
        {
            throw new InvalidOperationException(
                $"Failed to open OpenCV camera at index {_cameraIndex}.");
        }
    }

    /// <inheritdoc />
    public bool TryReadFrame(Mat frame) =>
        _capture is not null && _capture.Read(frame) && !frame.Empty();

    /// <inheritdoc />
    public void Dispose()
    {
        _capture?.Dispose();
        _capture = null;
    }
}
