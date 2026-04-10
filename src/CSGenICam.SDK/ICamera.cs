using KLabPseudoHaptic.Events;

namespace KLabPseudoHaptic;

/// <summary>
/// Represents a single GenICam-compatible camera device.
/// </summary>
public interface ICamera : IDisposable
{
    /// <summary>
    /// Unique identifier of the camera (typically the device serial number).
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Static information describing this camera.
    /// </summary>
    CameraInfo Info { get; }

    /// <summary>
    /// Current operational state.
    /// </summary>
    CameraState State { get; }

    /// <summary>
    /// Raised when a new frame has been acquired from the camera.
    /// </summary>
    event EventHandler<FrameReceivedEventArgs>? FrameReceived;

    /// <summary>
    /// Raised when the camera disconnects unexpectedly.
    /// </summary>
    event EventHandler<CameraEventArgs>? Disconnected;

    /// <summary>
    /// Opens the camera and prepares it for acquisition.
    /// </summary>
    Task OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the camera, releasing any underlying resources.
    /// </summary>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts continuous frame acquisition.
    /// </summary>
    Task StartAcquisitionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops frame acquisition.
    /// </summary>
    Task StopAcquisitionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a GenICam feature value by name.
    /// </summary>
    T GetFeature<T>(string name);

    /// <summary>
    /// Writes a GenICam feature value by name.
    /// </summary>
    void SetFeature<T>(string name, T value);
}
