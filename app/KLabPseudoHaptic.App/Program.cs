using System.Globalization;
using KLabPseudoHaptic.HandTracking;
using KLabPseudoHaptic.HandTracking.Onnx;
using KLabPseudoHaptic.Networking;
using KLabPseudoHaptic.WristAnalysis;

namespace KLabPseudoHaptic.App;

/// <summary>
/// Console entry point that wires together the ONNX hand detector, the
/// wrist-angle calculator, the TCP broadcaster, and a real-time console display.
/// Pure .NET — no Python required.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var options = ProgramOptions.Parse(args);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        await using var detector = new OnnxHandDetector(new OnnxHandDetectorOptions
        {
            PalmDetectorModelPath = options.PalmModelPath,
            HandLandmarkModelPath = options.LandmarkModelPath,
            CameraIndex = options.CameraIndex,
        });
        var calculator = new WristAngleCalculator();
        await using var broadcaster = new TcpAngleBroadcaster(options.Port);
        broadcaster.Start();

        Console.WriteLine("KLabPseudoHaptic wrist tracker (ONNX native)");
        Console.WriteLine($"  palm model:     {options.PalmModelPath}");
        Console.WriteLine($"  landmark model: {options.LandmarkModelPath}");
        Console.WriteLine($"  camera:         {options.CameraIndex}");
        Console.WriteLine($"  tcp:            {broadcaster.Endpoint}");
        Console.WriteLine("  press Ctrl+C to exit");
        Console.WriteLine();

        try
        {
            await foreach (var landmarks in detector.DetectAsync(cts.Token).ConfigureAwait(false))
            {
                var angle = calculator.Compute(landmarks);
                RenderStatus(landmarks, angle, broadcaster.ClientCount);
                try
                {
                    await broadcaster.BroadcastAsync(angle, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
        }

        Console.WriteLine();
        Console.WriteLine("Shutting down.");
        return 0;
    }

    private static void RenderStatus(HandLandmarks landmarks, WristAngle angle, int clientCount)
    {
        var line = string.Format(
            CultureInfo.InvariantCulture,
            "\r[{0,-7}] wrist: {1,8:+0.00;-0.00;0.00}°  {2,-8}  clients: {3,2}   ",
            landmarks.Handedness,
            angle.Degrees,
            angle.Classification,
            clientCount);
        Console.Write(line);
    }

    private sealed record ProgramOptions(
        string PalmModelPath,
        string LandmarkModelPath,
        int CameraIndex,
        int Port)
    {
        public static ProgramOptions Parse(string[] args)
        {
            var modelsDir = Path.Combine(AppContext.BaseDirectory, "models");
            var palmModel = Path.Combine(modelsDir, "palm_detection_lite.onnx");
            var landmarkModel = Path.Combine(modelsDir, "hand_landmark_lite.onnx");
            var camera = 0;
            var port = 5005;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--palm-model" when i + 1 < args.Length:
                        palmModel = args[++i];
                        break;
                    case "--landmark-model" when i + 1 < args.Length:
                        landmarkModel = args[++i];
                        break;
                    case "--camera" when i + 1 < args.Length:
                        camera = int.Parse(args[++i], CultureInfo.InvariantCulture);
                        break;
                    case "--port" when i + 1 < args.Length:
                        port = int.Parse(args[++i], CultureInfo.InvariantCulture);
                        break;
                }
            }

            return new ProgramOptions(palmModel, landmarkModel, camera, port);
        }
    }
}
