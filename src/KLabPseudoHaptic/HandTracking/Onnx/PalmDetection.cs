namespace KLabPseudoHaptic.HandTracking.Onnx;

/// <summary>
/// Intermediate result from the palm detection stage, before landmarks are
/// extracted.
/// </summary>
internal sealed class PalmDetection
{
    /// <summary>Detection confidence score.</summary>
    public required float Score { get; init; }

    /// <summary>Bounding box center X in normalized image coordinates [0, 1].</summary>
    public required float XCenter { get; init; }

    /// <summary>Bounding box center Y in normalized image coordinates [0, 1].</summary>
    public required float YCenter { get; init; }

    /// <summary>Bounding box width in normalized image coordinates.</summary>
    public required float Width { get; init; }

    /// <summary>Bounding box height in normalized image coordinates.</summary>
    public required float Height { get; init; }

    /// <summary>
    /// Seven palm keypoints in normalized coordinates (x, y). Indices:
    /// 0 = wrist, 1 = index MCP, 2 = middle MCP, 3 = ring MCP,
    /// 4 = pinky MCP, 5 = thumb CMC, 6 = thumb tip.
    /// </summary>
    public required (float X, float Y)[] Keypoints { get; init; }
}
