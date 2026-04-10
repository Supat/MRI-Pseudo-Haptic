using KLabPseudoHaptic.Exceptions;

namespace KLabPseudoHaptic;

/// <summary>
/// Default <see cref="ICameraManager"/> implementation.
/// </summary>
public sealed class CameraManager : ICameraManager
{
    private bool _disposed;

    /// <inheritdoc />
    public IReadOnlyList<CameraInfo> Enumerate()
    {
        ThrowIfDisposed();
        // TODO: Query the underlying GenICam transport layer for devices.
        return Array.Empty<CameraInfo>();
    }

    /// <inheritdoc />
    public ICamera Open(string cameraId)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(cameraId);

        // TODO: Resolve cameraId via the transport layer and return a concrete camera.
        throw new CameraNotFoundException(cameraId);
    }

    /// <inheritdoc />
    public ICamera OpenFirst()
    {
        ThrowIfDisposed();

        var cameras = Enumerate();
        if (cameras.Count == 0)
        {
            throw new CameraNotFoundException("No GenICam cameras available.");
        }

        return Open(cameras[0].Id);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // TODO: Release transport layer / producer resources.
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
