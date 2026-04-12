# MRI Pseudo-Haptic

A pure .NET 8 toolkit for estimating wrist posture from a webcam feed using
Google's [MediaPipe Hands](https://developers.google.com/mediapipe/solutions/vision/hand_landmarker)
ONNX models and broadcasting the result over TCP for use by downstream MRI
pseudo-haptic feedback components.

The entire runtime is native .NET — **no Python installation is required**.
Camera capture is handled by OpenCvSharp and model inference by ONNX Runtime.

## Pipeline

```
 camera (OpenCvSharp) ──► PalmDetector (ONNX) ──► HandLandmarkExtractor (ONNX)
                                                          │
                                                          ▼
                                                  WristAngleCalculator
                                                          │
                                               ┌──────────┴──────────┐
                                               ▼                     ▼
                                        console display     TcpAngleBroadcaster
                                                              127.0.0.1:5005
```

## Repository layout

```
.
├── KLabPseudoHaptic.sln
├── src/
│   └── KLabPseudoHaptic/               # class library
│       ├── HandTracking/               # landmark types + detector interface
│       │   └── Onnx/                   # ONNX-native detector (palm + landmark models)
│       ├── WristAnalysis/              # wrist angle + flexor/extensor classifier
│       ├── Networking/                 # TCP loopback broadcaster
│       └── ...                         # GenICam camera abstractions
├── app/
│   └── KLabPseudoHaptic.App/           # console runner
│       ├── Program.cs
│       └── models/                     # ONNX models go here (gitignored)
├── tests/
│   └── KLabPseudoHaptic.Tests/         # xUnit tests
└── tools/
    ├── download_models.sh              # Model download + conversion (Linux/macOS)
    └── download_models.ps1             # Model download + conversion (Windows)
```

## Recommended IDE

| IDE | Platform | Notes |
|-----|----------|-------|
| **[Visual Studio 2022](https://visualstudio.microsoft.com/)** (Community edition is free) | Windows | Best-in-class .NET experience. Open `KLabPseudoHaptic.sln` directly. |
| **[JetBrains Rider](https://www.jetbrains.com/rider/)** | Windows / macOS / Linux | Full .NET IDE with built-in test runner and NuGet manager. |
| **[Visual Studio Code](https://code.visualstudio.com/)** | Windows / macOS / Linux | Lightweight editor. Install the **C# Dev Kit** extension for IntelliSense, debugging, and test discovery. |

Any of the above will open the `.sln` file, resolve NuGet packages, and let you
build, run, and debug the project. Visual Studio 2022 is recommended on Windows
for the most seamless experience.

## Installation

### 1. Install the .NET 8 SDK

Download and install the .NET 8 SDK from
[https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0).

Verify the installation:

```bash
dotnet --version
# Expected output: 8.x.x
```

> **Visual Studio 2022 users:** the .NET 8 SDK is bundled with VS 2022 17.8+.
> Ensure the **.NET desktop development** workload is selected in the Visual
> Studio Installer.

### 2. Restore NuGet packages

```bash
dotnet restore KLabPseudoHaptic.sln
```

This downloads ONNX Runtime, OpenCvSharp, xUnit, and other dependencies. If you
are using Visual Studio or Rider, this step happens automatically when you open
the solution.

### 3. Download ONNX models

The detector needs two ONNX model files. A download-and-convert helper script
is provided:

```bash
# Linux / macOS
pip install tf2onnx
chmod +x tools/download_models.sh
./tools/download_models.sh
```

```powershell
# Windows (PowerShell)
pip install tf2onnx
.\tools\download_models.ps1
```

This downloads the official MediaPipe TFLite models from Google, converts them
to ONNX via `tf2onnx`, and places them in `app/KLabPseudoHaptic.App/models/`:

- `palm_detection_lite.onnx`
- `hand_landmark_lite.onnx`

> **`tf2onnx` is only needed once at setup time** to convert the model files.
> After the `.onnx` files are generated, Python is never used again — the
> application runs entirely on .NET.

### 4. Verify your webcam

```bash
dotnet run --project app/KLabPseudoHaptic.App -- --camera 0
```

If the app starts and prints `KLabPseudoHaptic wrist tracker (ONNX native)`,
your webcam is working. If you need a different camera, change the index.

## Build

```bash
dotnet build KLabPseudoHaptic.sln
```

## Run

```bash
dotnet run --project app/KLabPseudoHaptic.App

# With custom model paths
dotnet run --project app/KLabPseudoHaptic.App -- \
    --palm-model path/to/palm_detection_lite.onnx \
    --landmark-model path/to/hand_landmark_lite.onnx
```

All flags:

| Flag               | Default                                              | Description                                  |
|--------------------|------------------------------------------------------|----------------------------------------------|
| `--palm-model`     | `models/palm_detection_lite.onnx` in build output    | Path to the palm detection ONNX model.       |
| `--landmark-model` | `models/hand_landmark_lite.onnx` in build output     | Path to the hand landmark ONNX model.        |
| `--camera`         | `0`                                                  | OpenCV camera index.                         |
| `--port`           | `5005`                                               | TCP loopback port to broadcast on.           |

While running, the console shows a single-line live status:

```
[Right  ] wrist:   +12.35°  Extensor  clients:  1
```

Press `Ctrl+C` to exit cleanly.

## TCP broadcast protocol

The broadcaster binds to `127.0.0.1:<port>` and writes one UTF-8 line per wrist
update to every connected client:

```
<unix-ms>|<degrees>|<classification>\n
```

Example:

```
1712345678901|12.35|Extensor
1712345678934|12.41|Extensor
1712345678967|9.87|Neutral
```

Fields:

- `unix-ms` — broadcast timestamp in Unix epoch milliseconds.
- `degrees` — signed deviation from neutral, two decimal places. Positive =
  extension (dorsiflexion), negative = flexion (palmar flexion).
- `classification` — `Flexor`, `Neutral`, or `Extensor`.

Quick smoke test with `netcat`:

```bash
nc 127.0.0.1 5005
```

## Wrist angle estimation

`WristAngleCalculator` approximates the wrist angle as the signed angle, in the
image plane, between the hand-axis vector (`wrist → middle-finger MCP`) and the
image "up" direction. The classifier buckets the result using a configurable
neutral deadband:

```csharp
var calculator = new WristAngleCalculator(new WristAngleOptions
{
    NeutralThresholdDegrees = 10.0,   // half-width of the neutral deadband
    CalibrationOffsetDegrees = 0.0,   // subtract a calibrated resting angle
});
```

> **Note.** MediaPipe Hands does not provide forearm landmarks, so true
> flexion/extension relative to the forearm requires either integrating a pose
> model or calibrating the user's resting pose via
> `WristAngleOptions.CalibrationOffsetDegrees`. The default estimator is a
> lightweight heuristic intended for the pseudo-haptic feedback loop.

## Test

```bash
dotnet test
```

The test suite covers:

- `WristAngleCalculatorTests` — upright = neutral at 0°, tilts produce the
  expected signs and classifications, calibration offsets are applied, and
  small angles stay inside the deadband.
- `TcpAngleBroadcasterTests` — loopback binding, line-formatted broadcasts to
  a connected client, error handling before `Start`, and idempotent disposal.

## License

TBD.
