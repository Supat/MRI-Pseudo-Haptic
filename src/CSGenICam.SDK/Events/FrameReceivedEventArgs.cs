namespace KLabPseudoHaptic.Events;

/// <summary>
/// Event data for <see cref="ICamera.FrameReceived"/>.
/// </summary>
public sealed class FrameReceivedEventArgs : EventArgs
{
    /// <summary>Creates a new instance.</summary>
    public FrameReceivedEventArgs(Frame frame)
    {
        Frame = frame;
    }

    /// <summary>The acquired frame.</summary>
    public Frame Frame { get; }
}
