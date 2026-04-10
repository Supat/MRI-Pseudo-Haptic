namespace KLabPseudoHaptic.WristAnalysis;

/// <summary>
/// Configuration for <see cref="WristAngleCalculator"/>.
/// </summary>
public sealed class WristAngleOptions
{
    /// <summary>
    /// Half-width in degrees of the neutral deadband. Angles with an absolute
    /// value less than this value are classified as <see cref="WristClassification.Neutral"/>.
    /// </summary>
    public double NeutralThresholdDegrees { get; set; } = 10.0;

    /// <summary>
    /// Calibration offset in degrees applied to the raw hand-axis angle before
    /// classification. Use this to zero out a resting pose that is not perfectly
    /// vertical in the camera frame.
    /// </summary>
    public double CalibrationOffsetDegrees { get; set; } = 0.0;
}
