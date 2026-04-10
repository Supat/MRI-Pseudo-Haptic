namespace KLabPseudoHaptic;

/// <summary>
/// A single image frame delivered from a camera.
/// </summary>
public sealed class Frame
{
    /// <summary>Monotonic frame index assigned by the camera.</summary>
    public ulong FrameId { get; init; }

    /// <summary>Device timestamp in nanoseconds, if available.</summary>
    public ulong TimestampNs { get; init; }

    /// <summary>Image width in pixels.</summary>
    public int Width { get; init; }

    /// <summary>Image height in pixels.</summary>
    public int Height { get; init; }

    /// <summary>Pixel format of the payload.</summary>
    public PixelFormat PixelFormat { get; init; }

    /// <summary>Raw pixel buffer.</summary>
    public required ReadOnlyMemory<byte> Data { get; init; }
}
