namespace KLabPseudoHaptic;

/// <summary>
/// Operational state of an <see cref="ICamera"/>.
/// </summary>
public enum CameraState
{
    /// <summary>Camera has not yet been opened.</summary>
    Closed,

    /// <summary>Camera is open but not acquiring frames.</summary>
    Open,

    /// <summary>Camera is actively acquiring frames.</summary>
    Acquiring,

    /// <summary>Camera has entered a fault state.</summary>
    Faulted,
}
