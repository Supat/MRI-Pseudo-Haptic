namespace KLabPseudoHaptic.HandTracking.Onnx;

/// <summary>
/// Configuration for <see cref="OnnxHandDetector"/>.
/// </summary>
public sealed class OnnxHandDetectorOptions
{
    /// <summary>Path to the MediaPipe palm detection ONNX model.</summary>
    public string PalmDetectorModelPath { get; set; } = "models/palm_detection_lite.onnx";

    /// <summary>Path to the MediaPipe hand landmark ONNX model.</summary>
    public string HandLandmarkModelPath { get; set; } = "models/hand_landmark_lite.onnx";

    /// <summary>
    /// Camera source providing BGR frames. If <c>null</c>, a default
    /// <see cref="OpenCvCameraSource"/> is created from <see cref="CameraIndex"/>.
    /// </summary>
    public ICameraSource? CameraSource { get; set; }

    /// <summary>
    /// OpenCV camera index. Only used when <see cref="CameraSource"/> is
    /// <c>null</c> (the default).
    /// </summary>
    public int CameraIndex { get; set; } = 0;

    /// <summary>Minimum palm detection score (after sigmoid) to accept a detection.</summary>
    public float MinDetectionConfidence { get; set; } = 0.5f;

    /// <summary>Non-maximum-suppression IoU threshold for palm detections.</summary>
    public float NmsIouThreshold { get; set; } = 0.3f;

    /// <summary>
    /// Minimum hand-presence score from the landmark model required to emit a
    /// result. Frames where the landmark model is not confident are silently
    /// skipped.
    /// </summary>
    public float MinLandmarkConfidence { get; set; } = 0.5f;
}
