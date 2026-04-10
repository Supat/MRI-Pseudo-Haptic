namespace KLabPseudoHaptic.Events;

/// <summary>
/// Generic camera lifecycle event data.
/// </summary>
public sealed class CameraEventArgs : EventArgs
{
    /// <summary>Creates a new instance.</summary>
    public CameraEventArgs(string cameraId, string? message = null)
    {
        CameraId = cameraId;
        Message = message;
    }

    /// <summary>Identifier of the camera the event relates to.</summary>
    public string CameraId { get; }

    /// <summary>Optional human-readable message.</summary>
    public string? Message { get; }
}
