using OpenCvSharp;

namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// Abstraction over a camera device that delivers BGR frames as OpenCvSharp
/// <see cref="Mat"/> instances. Implementations can wrap OpenCV, SphinxSDK, or
/// any other capture source.
/// </summary>
public interface ICameraSource : IDisposable
{
    /// <summary>Whether the underlying device has been opened successfully.</summary>
    bool IsOpened { get; }

    /// <summary>Opens the camera device. Throws on failure.</summary>
    void Open();

    /// <summary>
    /// Reads the next frame into <paramref name="frame"/> (BGR, 8-bit).
    /// Returns <c>false</c> if no frame is available (dropped, timeout, etc.).
    /// </summary>
    bool TryReadFrame(Mat frame);
}
