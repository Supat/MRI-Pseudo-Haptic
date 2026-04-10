using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// <see cref="IHandLandmarkDetector"/> implementation that launches a Python
/// MediaPipe sidecar process and reads newline-delimited JSON landmark records
/// from its standard output.
/// </summary>
/// <remarks>
/// The sidecar script lives at <c>tools/mediapipe_hand_tracker.py</c>. Each
/// emitted line has the schema
/// <code>
/// { "t": &lt;unix-ms&gt;, "h": "Left|Right|Unknown", "s": &lt;score&gt;,
///   "lm": [[x, y, z] * 21] }
/// </code>
/// </remarks>
public sealed class MediaPipeHandDetector : IHandLandmarkDetector
{
    private readonly MediaPipeHandDetectorOptions _options;
    private Process? _process;
    private bool _disposed;

    /// <summary>Creates a new detector with the given options.</summary>
    public MediaPipeHandDetector(MediaPipeHandDetectorOptions? options = null)
    {
        _options = options ?? new MediaPipeHandDetectorOptions();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<HandLandmarks> DetectAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _process = StartSidecar();
        var reader = _process.StandardOutput;

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (line is null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            HandLandmarks? landmarks;
            try
            {
                landmarks = TryParseLine(line);
            }
            catch (JsonException)
            {
                continue;
            }

            if (landmarks is not null)
            {
                yield return landmarks;
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var process = _process;
        _process = null;

        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // Best effort: the sidecar may already be gone.
        }
        finally
        {
            process.Dispose();
        }
    }

    private Process StartSidecar()
    {
        var psi = new ProcessStartInfo
        {
            FileName = _options.PythonExecutable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add(_options.ScriptPath);
        psi.ArgumentList.Add("--camera");
        psi.ArgumentList.Add(_options.CameraIndex.ToString(CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("--max-hands");
        psi.ArgumentList.Add(_options.MaxHands.ToString(CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("--min-detection-confidence");
        psi.ArgumentList.Add(_options.MinDetectionConfidence.ToString(CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("--min-tracking-confidence");
        psi.ArgumentList.Add(_options.MinTrackingConfidence.ToString(CultureInfo.InvariantCulture));

        return Process.Start(psi)
            ?? throw new InvalidOperationException(
                $"Failed to start MediaPipe sidecar '{_options.PythonExecutable} {_options.ScriptPath}'.");
    }

    internal static HandLandmarks? TryParseLine(string line)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        if (!root.TryGetProperty("lm", out var lmArray) ||
            lmArray.ValueKind != JsonValueKind.Array ||
            lmArray.GetArrayLength() != HandLandmarkIndex.Count)
        {
            return null;
        }

        var points = new HandLandmark[HandLandmarkIndex.Count];
        var i = 0;
        foreach (var entry in lmArray.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() < 3)
            {
                return null;
            }

            var x = entry[0].GetSingle();
            var y = entry[1].GetSingle();
            var z = entry[2].GetSingle();
            points[i++] = new HandLandmark(x, y, z);
        }

        var handedness = Handedness.Unknown;
        if (root.TryGetProperty("h", out var handProp) && handProp.ValueKind == JsonValueKind.String)
        {
            handedness = handProp.GetString() switch
            {
                "Left" => Handedness.Left,
                "Right" => Handedness.Right,
                _ => Handedness.Unknown,
            };
        }

        float score = 0f;
        if (root.TryGetProperty("s", out var scoreProp) && scoreProp.ValueKind == JsonValueKind.Number)
        {
            score = scoreProp.GetSingle();
        }

        var timestamp = DateTimeOffset.UtcNow;
        if (root.TryGetProperty("t", out var tsProp) && tsProp.ValueKind == JsonValueKind.Number)
        {
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(tsProp.GetInt64());
        }

        return new HandLandmarks(points, handedness, score, timestamp);
    }
}
