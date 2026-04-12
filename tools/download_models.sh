#!/usr/bin/env bash
#
# Downloads the MediaPipe hand-tracking ONNX models required by the
# KLabPseudoHaptic ONNX-native detector.
#
# Usage:
#   chmod +x tools/download_models.sh
#   ./tools/download_models.sh
#
# The models are placed in:
#   app/KLabPseudoHaptic.App/models/
#
# These are the TFLite → ONNX conversions of the MediaPipe hand models.
# If the URLs below become stale, convert the TFLite models yourself:
#   pip install tf2onnx
#   python -m tf2onnx.convert --tflite palm_detection_lite.tflite --output palm_detection_lite.onnx
#   python -m tf2onnx.convert --tflite hand_landmark_lite.tflite  --output hand_landmark_lite.onnx
#
# Original TFLite models are hosted at:
#   https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/latest/hand_landmarker.task
# (unzip the .task file to find the individual TFLite models)

set -euo pipefail

MODELS_DIR="$(cd "$(dirname "$0")/.." && pwd)/app/KLabPseudoHaptic.App/models"
mkdir -p "$MODELS_DIR"

PALM_URL="https://storage.googleapis.com/mediapipe-assets/palm_detection_lite.tflite"
LANDMARK_URL="https://storage.googleapis.com/mediapipe-assets/hand_landmark_lite.tflite"

echo "Downloading palm detection model..."
curl -fSL "$PALM_URL" -o "$MODELS_DIR/palm_detection_lite.tflite"

echo "Downloading hand landmark model..."
curl -fSL "$LANDMARK_URL" -o "$MODELS_DIR/hand_landmark_lite.tflite"

echo ""
echo "Converting TFLite models to ONNX..."
echo "(Requires: pip install tf2onnx)"
echo ""

if command -v python3 &>/dev/null && python3 -c "import tf2onnx" 2>/dev/null; then
    python3 -m tf2onnx.convert \
        --tflite "$MODELS_DIR/palm_detection_lite.tflite" \
        --output "$MODELS_DIR/palm_detection_lite.onnx"

    python3 -m tf2onnx.convert \
        --tflite "$MODELS_DIR/hand_landmark_lite.tflite" \
        --output "$MODELS_DIR/hand_landmark_lite.onnx"

    echo ""
    echo "Done. ONNX models written to:"
    echo "  $MODELS_DIR/palm_detection_lite.onnx"
    echo "  $MODELS_DIR/hand_landmark_lite.onnx"
else
    echo "WARNING: tf2onnx not found. Install it and convert manually:"
    echo "  pip install tf2onnx"
    echo "  python3 -m tf2onnx.convert --tflite $MODELS_DIR/palm_detection_lite.tflite --output $MODELS_DIR/palm_detection_lite.onnx"
    echo "  python3 -m tf2onnx.convert --tflite $MODELS_DIR/hand_landmark_lite.tflite  --output $MODELS_DIR/hand_landmark_lite.onnx"
    exit 1
fi
