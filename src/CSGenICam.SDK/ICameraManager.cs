namespace CSGenICam.SDK;

/// <summary>
/// Entry point for discovering and opening GenICam cameras.
/// </summary>
public interface ICameraManager : IDisposable
{
    /// <summary>
    /// Enumerates all cameras currently visible to the transport layer.
    /// </summary>
    IReadOnlyList<CameraInfo> Enumerate();

    /// <summary>
    /// Opens the camera with the given identifier.
    /// </summary>
    ICamera Open(string cameraId);

    /// <summary>
    /// Opens the first available camera, or throws if none are present.
    /// </summary>
    ICamera OpenFirst();
}
