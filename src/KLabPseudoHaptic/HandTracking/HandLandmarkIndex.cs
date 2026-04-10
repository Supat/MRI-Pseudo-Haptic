namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// Indices into <see cref="HandLandmarks"/> for the 21 landmarks produced by
/// the MediaPipe Hands model.
/// </summary>
public static class HandLandmarkIndex
{
    /// <summary>Total number of landmarks per hand.</summary>
    public const int Count = 21;

    /// <summary>Wrist.</summary>
    public const int Wrist = 0;

    /// <summary>Thumb carpometacarpal joint.</summary>
    public const int ThumbCmc = 1;

    /// <summary>Thumb metacarpophalangeal joint.</summary>
    public const int ThumbMcp = 2;

    /// <summary>Thumb interphalangeal joint.</summary>
    public const int ThumbIp = 3;

    /// <summary>Thumb tip.</summary>
    public const int ThumbTip = 4;

    /// <summary>Index finger metacarpophalangeal joint.</summary>
    public const int IndexMcp = 5;

    /// <summary>Index finger proximal interphalangeal joint.</summary>
    public const int IndexPip = 6;

    /// <summary>Index finger distal interphalangeal joint.</summary>
    public const int IndexDip = 7;

    /// <summary>Index finger tip.</summary>
    public const int IndexTip = 8;

    /// <summary>Middle finger metacarpophalangeal joint.</summary>
    public const int MiddleMcp = 9;

    /// <summary>Middle finger proximal interphalangeal joint.</summary>
    public const int MiddlePip = 10;

    /// <summary>Middle finger distal interphalangeal joint.</summary>
    public const int MiddleDip = 11;

    /// <summary>Middle finger tip.</summary>
    public const int MiddleTip = 12;

    /// <summary>Ring finger metacarpophalangeal joint.</summary>
    public const int RingMcp = 13;

    /// <summary>Ring finger proximal interphalangeal joint.</summary>
    public const int RingPip = 14;

    /// <summary>Ring finger distal interphalangeal joint.</summary>
    public const int RingDip = 15;

    /// <summary>Ring finger tip.</summary>
    public const int RingTip = 16;

    /// <summary>Pinky metacarpophalangeal joint.</summary>
    public const int PinkyMcp = 17;

    /// <summary>Pinky proximal interphalangeal joint.</summary>
    public const int PinkyPip = 18;

    /// <summary>Pinky distal interphalangeal joint.</summary>
    public const int PinkyDip = 19;

    /// <summary>Pinky tip.</summary>
    public const int PinkyTip = 20;
}
