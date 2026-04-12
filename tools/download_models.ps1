# Downloads the MediaPipe hand-tracking ONNX models required by the
# KLabPseudoHaptic ONNX-native detector.
#
# Usage (PowerShell):
#   .\tools\download_models.ps1
#
# Models are placed in app\KLabPseudoHaptic.App\models\
#
# If the conversion step fails, install tf2onnx first:
#   pip install tf2onnx

$ErrorActionPreference = "Stop"

$ModelsDir = Join-Path $PSScriptRoot "..\app\KLabPseudoHaptic.App\models"
New-Item -ItemType Directory -Path $ModelsDir -Force | Out-Null
$ModelsDir = Resolve-Path $ModelsDir

$PalmUrl     = "https://storage.googleapis.com/mediapipe-assets/palm_detection_lite.tflite"
$LandmarkUrl = "https://storage.googleapis.com/mediapipe-assets/hand_landmark_lite.tflite"

Write-Host "Downloading palm detection model..."
Invoke-WebRequest -Uri $PalmUrl -OutFile "$ModelsDir\palm_detection_lite.tflite"

Write-Host "Downloading hand landmark model..."
Invoke-WebRequest -Uri $LandmarkUrl -OutFile "$ModelsDir\hand_landmark_lite.tflite"

Write-Host ""
Write-Host "Converting TFLite models to ONNX..."
Write-Host "(Requires: pip install tf2onnx)"
Write-Host ""

try {
    python -m tf2onnx.convert `
        --tflite "$ModelsDir\palm_detection_lite.tflite" `
        --output "$ModelsDir\palm_detection_lite.onnx"

    python -m tf2onnx.convert `
        --tflite "$ModelsDir\hand_landmark_lite.tflite" `
        --output "$ModelsDir\hand_landmark_lite.onnx"

    Write-Host ""
    Write-Host "Done. ONNX models written to:"
    Write-Host "  $ModelsDir\palm_detection_lite.onnx"
    Write-Host "  $ModelsDir\hand_landmark_lite.onnx"
}
catch {
    Write-Warning "tf2onnx conversion failed. Install it and convert manually:"
    Write-Host "  pip install tf2onnx"
    Write-Host "  python -m tf2onnx.convert --tflite $ModelsDir\palm_detection_lite.tflite --output $ModelsDir\palm_detection_lite.onnx"
    Write-Host "  python -m tf2onnx.convert --tflite $ModelsDir\hand_landmark_lite.tflite  --output $ModelsDir\hand_landmark_lite.onnx"
    exit 1
}
