# MRI Pseudo-Haptic

A .NET 8 toolkit for estimating wrist posture from a webcam feed using Google's
[MediaPipe Hands](https://developers.google.com/mediapipe/solutions/vision/hand_landmarker)
and broadcasting the result over TCP for use by downstream MRI pseudo-haptic
feedback components.

The repository contains a reusable class library (`KLabPseudoHaptic`), a
console application that wires it end-to-end, and a small Python sidecar that
hosts the MediaPipe model.

## Pipeline

```
 camera ─► Python sidecar ─┐                               ┌─► console display
           (MediaPipe)     │ JSON lines (stdout)           │
                           ▼                               │
             MediaPipeHandDetector ──► WristAngleCalculator┤
                      (.NET)                (.NET)         │
                                                           └─► TcpAngleBroadcaster
                                                                 127.0.0.1:5005
```

## Repository layout

```
.
├── KLabPseudoHaptic.sln
├── src/
│   └── KLabPseudoHaptic/            # class library
│       ├── HandTracking/            # landmark types + MediaPipe detector
│       ├── WristAnalysis/           # wrist angle + flexor/extensor classifier
│       ├── Networking/              # TCP loopback broadcaster
│       └── ...                      # GenICam camera abstractions
├── app/
│   └── KLabPseudoHaptic.App/        # console runner
│       ├── Program.cs
│       └── KLabPseudoHaptic.App.csproj
├── tests/
│   └── KLabPseudoHaptic.Tests/      # xUnit tests
└── tools/
    └── mediapipe_hand_tracker.py    # Python MediaPipe sidecar
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

Follow the steps below to set up everything needed to build and run the project.

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

### 2. Install Python 3.9+

Download Python from [https://www.python.org/downloads/](https://www.python.org/downloads/)
(3.9 or later). On Windows, check **"Add Python to PATH"** during installation.

Verify the installation:

```bash
python3 --version   # or 'python --version' on Windows
# Expected output: Python 3.9.x or later
```

### 3. Install Python dependencies

The MediaPipe sidecar requires `mediapipe` and `opencv-python`. It is
recommended to use a virtual environment:

```bash
# Create and activate a virtual environment (optional but recommended)
python3 -m venv .venv
source .venv/bin/activate        # Linux / macOS
# .venv\Scripts\activate         # Windows (cmd)
# .venv\Scripts\Activate.ps1     # Windows (PowerShell)

# Install the packages
pip install mediapipe opencv-python
```

### 4. Restore NuGet packages

```bash
dotnet restore KLabPseudoHaptic.sln
```

This downloads the xUnit and test-SDK packages referenced by the test project.
If you are using Visual Studio or Rider, this step happens automatically when
you open the solution.

### 5. Verify your webcam

The sidecar uses OpenCV's default camera (`index 0`). Confirm your webcam is
accessible:

```bash
python3 -c "import cv2; cap = cv2.VideoCapture(0); print('OK' if cap.isOpened() else 'FAIL'); cap.release()"
```

If you need a different camera, pass `--camera <index>` when running the app.

## Build

```bash
dotnet build KLabPseudoHaptic.sln
```

The console app's `csproj` copies `tools/mediapipe_hand_tracker.py` to the
output directory as `tools/mediapipe_hand_tracker.py`, so `dotnet run` will
find it automatically.

## Run

```bash
dotnet run --project app/KLabPseudoHaptic.App
```

Optional flags:

| Flag       | Default                                  | Description                              |
|------------|------------------------------------------|------------------------------------------|
| `--python` | `python3` (or `$KLAB_PYTHON`)            | Python interpreter used for the sidecar. |
| `--script` | `tools/mediapipe_hand_tracker.py` in the build output | Path to the sidecar script.  |
| `--camera` | `0`                                      | OpenCV camera index.                     |
| `--port`   | `5005`                                   | TCP loopback port to broadcast on.       |

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
