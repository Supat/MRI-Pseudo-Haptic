namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// A single 3D landmark produced by the MediaPipe hand model.
/// Coordinates are in the MediaPipe normalized image space:
/// X and Y are in [0, 1] relative to the image width/height, and
/// Z is the landmark depth relative to the wrist (smaller = closer to the camera).
/// </summary>
/// <param name="X">Normalized X coordinate (0..1, left to right).</param>
/// <param name="Y">Normalized Y coordinate (0..1, top to bottom).</param>
/// <param name="Z">Depth relative to the wrist.</param>
public readonly record struct HandLandmark(float X, float Y, float Z);
