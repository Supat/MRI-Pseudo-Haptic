namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// Which hand a detection corresponds to.
/// </summary>
public enum Handedness
{
    /// <summary>Handedness could not be determined.</summary>
    Unknown = 0,

    /// <summary>Left hand.</summary>
    Left,

    /// <summary>Right hand.</summary>
    Right,
}
