namespace KLabPseudoHaptic.WristAnalysis;

/// <summary>
/// Classification of the current wrist posture, relative to a neutral resting
/// position, used by the pseudo-haptic feedback loop.
/// </summary>
public enum WristClassification
{
    /// <summary>Wrist is within the neutral deadband.</summary>
    Neutral = 0,

    /// <summary>Wrist is flexed (palmar flexion).</summary>
    Flexor,

    /// <summary>Wrist is extended (dorsiflexion).</summary>
    Extensor,
}
