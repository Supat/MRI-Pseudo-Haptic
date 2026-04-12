using System.Globalization;
using KLabPseudoHaptic.HandTracking;
using KLabPseudoHaptic.HandTracking.Onnx;
using KLabPseudoHaptic.Networking;
using KLabPseudoHaptic.WristAnalysis;

namespace KLabPseudoHaptic.App;

/// <summary>
/// Console entry point that wires together a hand detector (ONNX-native or
/// Python-sidecar), the wrist-angle calculator, the TCP broadcaster, and a
/// simple real-time console display.
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

        await using var detector = CreateDetector(options);
        var calculator = new WristAngleCalculator();
        await using var broadcaster = new TcpAngleBroadcaster(options.Port);
        broadcaster.Start();

        Console.WriteLine("KLabPseudoHaptic wrist tracker");
        Console.WriteLine($"  detector:  {options.Detector}");
        Console.WriteLine($"  camera:    {options.CameraIndex}");
        Console.WriteLine($"  tcp:       {broadcaster.Endpoint}");
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

    private static IHandLandmarkDetector CreateDetector(ProgramOptions options)
    {
        return options.Detector switch
        {
            DetectorKind.Onnx => new OnnxHandDetector(new OnnxHandDetectorOptions
            {
                PalmDetectorModelPath = options.PalmModelPath,
                HandLandmarkModelPath = options.LandmarkModelPath,
                CameraIndex = options.CameraIndex,
            }),
            DetectorKind.MediaPipe => new MediaPipeHandDetector(new MediaPipeHandDetectorOptions
            {
                PythonExecutable = options.PythonExecutable,
                ScriptPath = options.ScriptPath,
                CameraIndex = options.CameraIndex,
                MaxHands = 1,
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(options)),
        };
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

    private enum DetectorKind { Onnx, MediaPipe }

    private sealed record ProgramOptions(
        DetectorKind Detector,
        string PalmModelPath,
        string LandmarkModelPath,
        string PythonExecutable,
        string ScriptPath,
        int CameraIndex,
        int Port)
    {
        public static ProgramOptions Parse(string[] args)
        {
            var detector = DetectorKind.Onnx;
            var modelsDir = Path.Combine(AppContext.BaseDirectory, "models");
            var palmModel = Path.Combine(modelsDir, "palm_detection_lite.onnx");
            var landmarkModel = Path.Combine(modelsDir, "hand_landmark_lite.onnx");
            var python = Environment.GetEnvironmentVariable("KLAB_PYTHON") ?? "python3";
            var script = Path.Combine(AppContext.BaseDirectory, "tools", "mediapipe_hand_tracker.py");
            var camera = 0;
            var port = 5005;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--detector" when i + 1 < args.Length:
                        detector = args[++i].ToLowerInvariant() switch
                        {
                            "onnx" => DetectorKind.Onnx,
                            "mediapipe" or "python" => DetectorKind.MediaPipe,
                            _ => throw new ArgumentException(
                                $"Unknown detector '{args[i]}'. Use 'onnx' or 'mediapipe'."),
                        };
                        break;
                    case "--palm-model" when i + 1 < args.Length:
                        palmModel = args[++i];
                        break;
                    case "--landmark-model" when i + 1 < args.Length:
                        landmarkModel = args[++i];
                        break;
                    case "--python" when i + 1 < args.Length:
                        python = args[++i];
                        break;
                    case "--script" when i + 1 < args.Length:
                        script = args[++i];
                        break;
                    case "--camera" when i + 1 < args.Length:
                        camera = int.Parse(args[++i], CultureInfo.InvariantCulture);
                        break;
                    case "--port" when i + 1 < args.Length:
                        port = int.Parse(args[++i], CultureInfo.InvariantCulture);
                        break;
                }
            }

            return new ProgramOptions(detector, palmModel, landmarkModel, python, script, camera, port);
        }
    }
}
