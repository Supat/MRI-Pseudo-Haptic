using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace KLabPseudoHaptic.HandTracking.Onnx;

/// <summary>
/// Runs the MediaPipe hand landmark ONNX model on a crop produced by the palm
/// detector and returns 21 landmarks mapped back to the original image space.
/// </summary>
internal sealed class HandLandmarkExtractor : IDisposable
{
    /// <summary>Square input size expected by the hand landmark model.</summary>
    internal const int InputSize = 224;

    /// <summary>Scale factor applied to the palm bounding box before cropping.</summary>
    private const float BoxScale = 2.6f;

    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly float _handPresenceThreshold;

    public HandLandmarkExtractor(string modelPath, float handPresenceThreshold = 0.5f)
    {
        _session = new InferenceSession(modelPath);
        _inputName = _session.InputNames[0];
        _handPresenceThreshold = handPresenceThreshold;
    }

    /// <summary>
    /// Extracts 21 hand landmarks from the image region indicated by
    /// <paramref name="palm"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="HandLandmarks"/> instance with normalized image-space
    /// coordinates, or <c>null</c> if the landmark model's hand-presence score
    /// is below the confidence threshold.
    /// </returns>
    public HandLandmarks? Extract(Mat bgrImage, PalmDetection palm)
    {
        int imgW = bgrImage.Width;
        int imgH = bgrImage.Height;

        // Compute rotation angle from palm keypoints (wrist → middle-finger MCP).
        var wrist = palm.Keypoints[0];
        var middleMcp = palm.Keypoints[2];
        float angleRad = MathF.Atan2(
            -(middleMcp.X - wrist.X) * imgW,
            (middleMcp.Y - wrist.Y) * imgH);
        float angleDeg = angleRad * 180f / MathF.PI;

        // Expanded square crop centered on the palm.
        float boxSize = MathF.Max(palm.Width * imgW, palm.Height * imgH) * BoxScale;
        float cx = palm.XCenter * imgW;
        float cy = palm.YCenter * imgH;

        // Build an affine transform: rotate about the palm center and scale
        // into the landmark model's input size.
        using var rotMat = Cv2.GetRotationMatrix2D(new Point2f(cx, cy), angleDeg, InputSize / boxSize);
        // Translate so the center lands at (InputSize/2, InputSize/2).
        rotMat.Set(0, 2, rotMat.Get<double>(0, 2) + InputSize / 2.0 - cx);
        rotMat.Set(1, 2, rotMat.Get<double>(1, 2) + InputSize / 2.0 - cy);

        using var crop = new Mat();
        Cv2.WarpAffine(bgrImage, crop, rotMat, new Size(InputSize, InputSize),
            InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));

        // Convert BGR → RGB, normalise to [0, 1], fill tensor.
        using var rgb = new Mat();
        Cv2.CvtColor(crop, rgb, ColorConversionCodes.BGR2RGB);
        var inputData = MatToFloatArray(rgb);

        var tensor = new DenseTensor<float>(inputData, [1, InputSize, InputSize, 3]);
        using var results = _session.Run([NamedOnnxValue.CreateFromTensor(_inputName, tensor)]);
        var outputs = results.ToList();

        // The model produces three outputs (order may vary by export):
        //   - landmarks  [1, 63]  (21 * 3: x, y, z in crop pixel space)
        //   - hand_flag  [1, 1]   (hand-presence logit)
        //   - handedness [1, 1]   (left/right logit, >0.5 after sigmoid = right)
        // Identify them by their last dimension.
        float[]? landmarkRaw = null;
        float handFlagLogit = 0f;
        float handednessLogit = 0f;

        foreach (var output in outputs)
        {
            var t = output.AsTensor<float>();
            int lastDim = t.Dimensions[^1];
            if (lastDim == 63)
            {
                landmarkRaw = t.ToArray();
            }
            else if (landmarkRaw is null)
            {
                // First scalar output encountered before landmarks → hand_flag
                handFlagLogit = t[0, 0];
            }
            else
            {
                handednessLogit = t[0, 0];
            }
        }

        // Re-scan to handle when scalar outputs appear after landmarks.
        if (landmarkRaw is null)
        {
            return null;
        }

        // Try a second pass to pick up scalars we might have missed.
        foreach (var output in outputs)
        {
            var t = output.AsTensor<float>();
            if (t.Dimensions[^1] == 1)
            {
                float val = t[0, 0];
                // The flag is typically a larger logit, the handedness is closer to 0.
                // Robust heuristic: assign by name if available, otherwise by magnitude.
                string name = output.Name.ToLowerInvariant();
                if (name.Contains("flag") || name.Contains("presence"))
                {
                    handFlagLogit = val;
                }
                else if (name.Contains("handed"))
                {
                    handednessLogit = val;
                }
            }
        }

        float handPresence = Sigmoid(handFlagLogit);
        if (handPresence < _handPresenceThreshold)
        {
            return null;
        }

        // Build the inverse affine matrix to map crop pixels → original image pixels.
        using var invRotMat = new Mat();
        Cv2.InvertAffineTransform(rotMat, invRotMat);

        var points = new HandLandmark[HandLandmarkIndex.Count];
        for (int i = 0; i < HandLandmarkIndex.Count; i++)
        {
            float lx = landmarkRaw[i * 3];
            float ly = landmarkRaw[i * 3 + 1];
            float lz = landmarkRaw[i * 3 + 2];

            // Transform (lx, ly) from crop space back to original image space.
            double ox = invRotMat.Get<double>(0, 0) * lx + invRotMat.Get<double>(0, 1) * ly + invRotMat.Get<double>(0, 2);
            double oy = invRotMat.Get<double>(1, 0) * lx + invRotMat.Get<double>(1, 1) * ly + invRotMat.Get<double>(1, 2);

            // Normalise to [0, 1].
            points[i] = new HandLandmark((float)(ox / imgW), (float)(oy / imgH), lz / InputSize);
        }

        float handednessScore = Sigmoid(handednessLogit);
        var handedness = handednessScore > 0.5f ? Handedness.Right : Handedness.Left;

        return new HandLandmarks(points, handedness, handPresence, DateTimeOffset.UtcNow);
    }

    public void Dispose() => _session.Dispose();

    private static float[] MatToFloatArray(Mat rgb)
    {
        int h = rgb.Rows, w = rgb.Cols;
        var data = new float[h * w * 3];
        rgb.GetArray(out byte[] raw);
        for (int i = 0; i < raw.Length; i++)
        {
            data[i] = raw[i] / 255f;
        }
        return data;
    }

    private static float Sigmoid(float x) => 1f / (1f + MathF.Exp(-x));
}
