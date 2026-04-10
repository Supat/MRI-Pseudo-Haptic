namespace KLabPseudoHaptic.WristAnalysis;

/// <summary>
/// Estimated wrist angle and its posture classification.
/// </summary>
/// <param name="Degrees">
/// Signed deviation from neutral in degrees. Positive values indicate extension
/// (dorsiflexion), negative values indicate flexion (palmar flexion).
/// </param>
/// <param name="Classification">Discrete posture bucket for the given angle.</param>
public readonly record struct WristAngle(double Degrees, WristClassification Classification);
