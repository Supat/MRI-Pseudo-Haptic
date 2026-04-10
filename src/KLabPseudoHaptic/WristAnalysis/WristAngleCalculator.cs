using KLabPseudoHaptic.HandTracking;

namespace KLabPseudoHaptic.WristAnalysis;

/// <summary>
/// Estimates a signed wrist angle from a <see cref="HandLandmarks"/> record and
/// classifies it as flexor / neutral / extensor.
/// </summary>
/// <remarks>
/// The estimator approximates the wrist angle as the signed deviation, in the
/// image plane, between the hand-axis vector (wrist &#x2192; middle-finger MCP)
/// and the upward image axis. This is intentionally a lightweight heuristic:
/// MediaPipe Hands alone does not provide forearm landmarks, so true
/// flexion/extension relative to the forearm requires either a pose model or
/// user calibration via <see cref="WristAngleOptions.CalibrationOffsetDegrees"/>.
/// </remarks>
public sealed class WristAngleCalculator
{
    private readonly WristAngleOptions _options;

    /// <summary>Creates a new calculator with default or supplied options.</summary>
    public WristAngleCalculator(WristAngleOptions? options = null)
    {
        _options = options ?? new WristAngleOptions();
    }

    /// <summary>Active options instance.</summary>
    public WristAngleOptions Options => _options;

    /// <summary>
    /// Computes the signed wrist angle in degrees and its classification.
    /// </summary>
    public WristAngle Compute(HandLandmarks landmarks)
    {
        ArgumentNullException.ThrowIfNull(landmarks);

        var wrist = landmarks[HandLandmarkIndex.Wrist];
        var middleMcp = landmarks[HandLandmarkIndex.MiddleMcp];

        // Hand-axis vector in normalized image coordinates. Note that the
        // image Y axis grows downward, so a hand pointing "up" in the frame
        // has dy < 0.
        double dx = middleMcp.X - wrist.X;
        double dy = middleMcp.Y - wrist.Y;

        // Angle between the hand axis and the image "up" direction (0, -1).
        // atan2(dx, -dy) yields 0 for a perfectly upright hand, positive for
        // rotations to the image right, and negative to the image left.
        double rawRadians = Math.Atan2(dx, -dy);
        double degrees = (rawRadians * 180.0 / Math.PI) - _options.CalibrationOffsetDegrees;

        var classification = Classify(degrees, _options.NeutralThresholdDegrees);
        return new WristAngle(degrees, classification);
    }

    private static WristClassification Classify(double degrees, double neutralThreshold)
    {
        if (degrees > neutralThreshold)
        {
            return WristClassification.Extensor;
        }

        if (degrees < -neutralThreshold)
        {
            return WristClassification.Flexor;
        }

        return WristClassification.Neutral;
    }
}
