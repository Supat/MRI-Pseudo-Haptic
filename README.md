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

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Python 3.9+ with `mediapipe` and `opencv-python`:
  ```bash
  pip install mediapipe opencv-python
  ```
- A working webcam accessible by OpenCV

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
