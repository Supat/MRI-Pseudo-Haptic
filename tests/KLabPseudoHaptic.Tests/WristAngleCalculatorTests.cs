using KLabPseudoHaptic.HandTracking;
using KLabPseudoHaptic.WristAnalysis;
using Xunit;

namespace KLabPseudoHaptic.Tests;

public class WristAngleCalculatorTests
{
    [Fact]
    public void Compute_HandPointingUp_IsNeutralAtZeroDegrees()
    {
        var landmarks = MakeHand(wrist: (0.5f, 0.9f), middleMcp: (0.5f, 0.5f));
        var calculator = new WristAngleCalculator();

        var result = calculator.Compute(landmarks);

        Assert.Equal(0.0, result.Degrees, precision: 3);
        Assert.Equal(WristClassification.Neutral, result.Classification);
    }

    [Fact]
    public void Compute_HandTiltedRight_IsExtensor()
    {
        var landmarks = MakeHand(wrist: (0.5f, 0.9f), middleMcp: (0.9f, 0.9f));
        var calculator = new WristAngleCalculator();

        var result = calculator.Compute(landmarks);

        Assert.Equal(90.0, result.Degrees, precision: 3);
        Assert.Equal(WristClassification.Extensor, result.Classification);
    }

    [Fact]
    public void Compute_HandTiltedLeft_IsFlexor()
    {
        var landmarks = MakeHand(wrist: (0.5f, 0.9f), middleMcp: (0.1f, 0.9f));
        var calculator = new WristAngleCalculator();

        var result = calculator.Compute(landmarks);

        Assert.Equal(-90.0, result.Degrees, precision: 3);
        Assert.Equal(WristClassification.Flexor, result.Classification);
    }

    [Fact]
    public void Compute_SmallTiltWithinDeadband_IsNeutral()
    {
        // ~5 degrees to the right of upright.
        var landmarks = MakeHand(
            wrist: (0.5f, 0.9f),
            middleMcp: (0.5f + 0.0349f, 0.5f));
        var calculator = new WristAngleCalculator(new WristAngleOptions
        {
            NeutralThresholdDegrees = 10.0,
        });

        var result = calculator.Compute(landmarks);

        Assert.InRange(result.Degrees, 4.0, 6.0);
        Assert.Equal(WristClassification.Neutral, result.Classification);
    }

    [Fact]
    public void Compute_CalibrationOffset_IsSubtractedFromAngle()
    {
        var landmarks = MakeHand(wrist: (0.5f, 0.9f), middleMcp: (0.9f, 0.9f));
        var calculator = new WristAngleCalculator(new WristAngleOptions
        {
            CalibrationOffsetDegrees = 90.0,
        });

        var result = calculator.Compute(landmarks);

        Assert.Equal(0.0, result.Degrees, precision: 3);
        Assert.Equal(WristClassification.Neutral, result.Classification);
    }

    private static HandLandmarks MakeHand((float X, float Y) wrist, (float X, float Y) middleMcp)
    {
        var points = new HandLandmark[HandLandmarkIndex.Count];
        var wristPoint = new HandLandmark(wrist.X, wrist.Y, 0f);
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = wristPoint;
        }
        points[HandLandmarkIndex.MiddleMcp] = new HandLandmark(middleMcp.X, middleMcp.Y, 0f);

        return new HandLandmarks(points, Handedness.Right, score: 1.0f, DateTimeOffset.UtcNow);
    }
}
