#!/usr/bin/env python3
"""MediaPipe Hands sidecar for the KLabPseudoHaptic .NET pipeline.

The script opens a camera via OpenCV, runs MediaPipe Hands on each frame, and
writes one newline-delimited JSON record per detected hand to stdout. The
KLabPseudoHaptic `MediaPipeHandDetector` spawns this process and consumes that
stream.

Output schema (one JSON object per line):

    {
      "t":  <unix epoch milliseconds: int>,
      "h":  "Left" | "Right" | "Unknown",
      "s":  <handedness score: float in [0, 1]>,
      "lm": [[x, y, z], ...  21 entries ...]
    }

Landmark coordinates are MediaPipe's normalized image-space values: x and y in
[0, 1], z in the MediaPipe-defined relative-depth space (smaller = closer).

Dependencies:
    pip install mediapipe opencv-python
"""

from __future__ import annotations

import argparse
import json
import sys
import time


def _fail(msg: str, code: int = 1) -> None:
    sys.stderr.write(msg + "\n")
    sys.stderr.flush()
    sys.exit(code)


def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--camera", type=int, default=0,
                        help="OpenCV camera index (default: 0).")
    parser.add_argument("--max-hands", type=int, default=1,
                        help="Maximum number of hands to track (default: 1).")
    parser.add_argument("--min-detection-confidence", type=float, default=0.5,
                        help="Minimum detection confidence (default: 0.5).")
    parser.add_argument("--min-tracking-confidence", type=float, default=0.5,
                        help="Minimum tracking confidence (default: 0.5).")
    return parser.parse_args()


def main() -> None:
    try:
        import cv2  # type: ignore
        import mediapipe as mp  # type: ignore
    except ImportError as exc:
        _fail(f"missing dependency: {exc}. Install with `pip install mediapipe opencv-python`.", 2)
        return

    args = _parse_args()

    capture = cv2.VideoCapture(args.camera)
    if not capture.isOpened():
        _fail(f"failed to open camera {args.camera}", 1)

    hands = mp.solutions.hands.Hands(
        max_num_hands=args.max_hands,
        min_detection_confidence=args.min_detection_confidence,
        min_tracking_confidence=args.min_tracking_confidence,
    )

    try:
        while True:
            ok, frame = capture.read()
            if not ok:
                continue

            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            result = hands.process(rgb)
            if not result.multi_hand_landmarks:
                continue

            timestamp_ms = int(time.time() * 1000)
            handedness_info = result.multi_handedness or []

            for i, hand_landmarks in enumerate(result.multi_hand_landmarks):
                label = "Unknown"
                score = 0.0
                if i < len(handedness_info):
                    classification = handedness_info[i].classification[0]
                    label = classification.label
                    score = float(classification.score)

                points = [[p.x, p.y, p.z] for p in hand_landmarks.landmark]
                record = {"t": timestamp_ms, "h": label, "s": score, "lm": points}
                sys.stdout.write(json.dumps(record, separators=(",", ":")) + "\n")
                sys.stdout.flush()
    except KeyboardInterrupt:
        pass
    finally:
        try:
            capture.release()
        except Exception:
            pass
        try:
            hands.close()
        except Exception:
            pass


if __name__ == "__main__":
    main()
