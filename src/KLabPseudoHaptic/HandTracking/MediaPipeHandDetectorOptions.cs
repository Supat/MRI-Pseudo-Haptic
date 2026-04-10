namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// Configuration for <see cref="MediaPipeHandDetector"/>.
/// </summary>
public sealed class MediaPipeHandDetectorOptions
{
    /// <summary>Path to the Python interpreter used to run the sidecar.</summary>
    public string PythonExecutable { get; set; } = "python3";

    /// <summary>Path to <c>mediapipe_hand_tracker.py</c>.</summary>
    public string ScriptPath { get; set; } = "tools/mediapipe_hand_tracker.py";

    /// <summary>Index of the OpenCV camera to open.</summary>
    public int CameraIndex { get; set; } = 0;

    /// <summary>Maximum number of hands to track simultaneously.</summary>
    public int MaxHands { get; set; } = 1;

    /// <summary>Minimum confidence required to emit an initial detection.</summary>
    public float MinDetectionConfidence { get; set; } = 0.5f;

    /// <summary>Minimum confidence required to continue tracking an existing hand.</summary>
    public float MinTrackingConfidence { get; set; } = 0.5f;
}
