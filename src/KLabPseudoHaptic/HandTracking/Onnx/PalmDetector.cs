using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace KLabPseudoHaptic.HandTracking.Onnx;

/// <summary>
/// Runs the MediaPipe palm detection ONNX model and returns decoded
/// <see cref="PalmDetection"/> results after non-maximum suppression.
/// </summary>
internal sealed class PalmDetector : IDisposable
{
    /// <summary>Expected square input size of the palm detection model.</summary>
    internal const int InputSize = 192;

    private const int NumKeypoints = 7;
    // Each regressor row: [dx, dy, w, h, kp0_x, kp0_y, ..., kp6_x, kp6_y] = 18 values
    private const int RegressorColumns = 4 + NumKeypoints * 2;

    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly (float X, float Y)[] _anchors;
    private readonly float _scoreThreshold;
    private readonly float _nmsIouThreshold;

    public PalmDetector(string modelPath, float scoreThreshold = 0.5f, float nmsIouThreshold = 0.3f)
    {
        _session = new InferenceSession(modelPath);
        _inputName = _session.InputNames[0];
        _anchors = SsdAnchorGenerator.Generate(InputSize);
        _scoreThreshold = scoreThreshold;
        _nmsIouThreshold = nmsIouThreshold;
    }

    /// <summary>
    /// Detect palms in a preprocessed RGB float array of shape [1, 192, 192, 3]
    /// with values normalised to [0, 1].
    /// </summary>
    public List<PalmDetection> Detect(float[] inputData)
    {
        var tensor = new DenseTensor<float>(inputData, [1, InputSize, InputSize, 3]);
        using var results = _session.Run([NamedOnnxValue.CreateFromTensor(_inputName, tensor)]);
        var outputs = results.ToList();

        // The model outputs two tensors.  Order varies by export, so pick the
        // wide tensor (18 cols) as regressors and the narrow one (1 col) as
        // classificators.
        var t0 = outputs[0].AsTensor<float>();
        var t1 = outputs[1].AsTensor<float>();

        Tensor<float> regressors, classificators;
        if (t0.Dimensions[^1] == RegressorColumns)
        {
            regressors = t0;
            classificators = t1;
        }
        else
        {
            regressors = t1;
            classificators = t0;
        }

        var detections = Decode(regressors, classificators);
        return Nms(detections);
    }

    public void Dispose() => _session.Dispose();

    private List<PalmDetection> Decode(Tensor<float> regressors, Tensor<float> classificators)
    {
        int numAnchors = _anchors.Length;
        var detections = new List<PalmDetection>();

        for (int i = 0; i < numAnchors; i++)
        {
            float rawScore = classificators[0, i, 0];
            float score = Sigmoid(rawScore);
            if (score < _scoreThreshold)
            {
                continue;
            }

            var (ax, ay) = _anchors[i];

            float dx = regressors[0, i, 0] / InputSize;
            float dy = regressors[0, i, 1] / InputSize;
            float w = regressors[0, i, 2] / InputSize;
            float h = regressors[0, i, 3] / InputSize;

            float cx = ax + dx;
            float cy = ay + dy;

            var keypoints = new (float X, float Y)[NumKeypoints];
            for (int k = 0; k < NumKeypoints; k++)
            {
                float kx = regressors[0, i, 4 + 2 * k] / InputSize + ax;
                float ky = regressors[0, i, 4 + 2 * k + 1] / InputSize + ay;
                keypoints[k] = (kx, ky);
            }

            detections.Add(new PalmDetection
            {
                Score = score,
                XCenter = cx,
                YCenter = cy,
                Width = w,
                Height = h,
                Keypoints = keypoints,
            });
        }

        return detections;
    }

    private List<PalmDetection> Nms(List<PalmDetection> detections)
    {
        detections.Sort((a, b) => b.Score.CompareTo(a.Score));

        var keep = new List<PalmDetection>();
        var suppressed = new bool[detections.Count];

        for (int i = 0; i < detections.Count; i++)
        {
            if (suppressed[i])
            {
                continue;
            }

            keep.Add(detections[i]);

            for (int j = i + 1; j < detections.Count; j++)
            {
                if (suppressed[j])
                {
                    continue;
                }

                if (Iou(detections[i], detections[j]) > _nmsIouThreshold)
                {
                    suppressed[j] = true;
                }
            }
        }

        return keep;
    }

    private static float Iou(PalmDetection a, PalmDetection b)
    {
        float ax1 = a.XCenter - a.Width / 2, ay1 = a.YCenter - a.Height / 2;
        float ax2 = a.XCenter + a.Width / 2, ay2 = a.YCenter + a.Height / 2;
        float bx1 = b.XCenter - b.Width / 2, by1 = b.YCenter - b.Height / 2;
        float bx2 = b.XCenter + b.Width / 2, by2 = b.YCenter + b.Height / 2;

        float ix1 = Math.Max(ax1, bx1), iy1 = Math.Max(ay1, by1);
        float ix2 = Math.Min(ax2, bx2), iy2 = Math.Min(ay2, by2);

        float interArea = Math.Max(0, ix2 - ix1) * Math.Max(0, iy2 - iy1);
        float unionArea = a.Width * a.Height + b.Width * b.Height - interArea;

        return unionArea > 0 ? interArea / unionArea : 0;
    }

    private static float Sigmoid(float x) => 1f / (1f + MathF.Exp(-x));
}
