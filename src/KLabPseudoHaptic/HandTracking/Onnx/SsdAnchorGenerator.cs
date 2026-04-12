namespace KLabPseudoHaptic.HandTracking.Onnx;

/// <summary>
/// Generates the fixed SSD anchors used by the MediaPipe palm detection model
/// (192 x 192 input, 4 layers, strides [8, 16, 16, 16], 2 anchors per cell).
/// </summary>
internal static class SsdAnchorGenerator
{
    private static readonly int[] Strides = [8, 16, 16, 16];
    private const int AnchorsPerCell = 2;

    /// <summary>
    /// Returns the 2016 anchors for a <paramref name="inputSize"/> x
    /// <paramref name="inputSize"/> image grid.
    /// </summary>
    public static (float X, float Y)[] Generate(int inputSize = 192)
    {
        var anchors = new List<(float X, float Y)>();

        foreach (var stride in Strides)
        {
            int gridSize = (inputSize + stride - 1) / stride;

            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    float cx = (x + 0.5f) / gridSize;
                    float cy = (y + 0.5f) / gridSize;

                    for (int n = 0; n < AnchorsPerCell; n++)
                    {
                        anchors.Add((cx, cy));
                    }
                }
            }
        }

        return anchors.ToArray();
    }
}
