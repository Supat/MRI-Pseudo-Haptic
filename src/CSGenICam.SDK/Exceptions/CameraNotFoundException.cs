namespace CSGenICam.SDK.Exceptions;

/// <summary>
/// Thrown when a requested camera cannot be located on the transport layer.
/// </summary>
public sealed class CameraNotFoundException : GenICamException
{
    /// <summary>Creates a new instance for the given camera identifier.</summary>
    public CameraNotFoundException(string cameraId)
        : base($"Camera '{cameraId}' was not found.")
    {
        CameraId = cameraId;
    }

    /// <summary>Identifier of the camera that could not be found.</summary>
    public string? CameraId { get; }
}
