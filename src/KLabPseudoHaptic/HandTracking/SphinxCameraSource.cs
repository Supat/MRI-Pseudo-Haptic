using System.Runtime.InteropServices;
using OpenCvSharp;

namespace KLabPseudoHaptic.HandTracking;

/// <summary>
/// <see cref="ICameraSource"/> backed by the SphinxSDK v2.1.4.1 native camera
/// interface.
/// </summary>
/// <remarks>
/// <para>
/// SphinxSDK is a proprietary GenICam-compatible camera SDK. This source wraps
/// the SDK's device discovery, frame acquisition, and buffer management behind
/// the <see cref="ICameraSource"/> contract so that the same ONNX hand-detection
/// pipeline works regardless of camera hardware.
/// </para>
/// <para>
/// The SDK must be installed and its native libraries must be loadable at
/// runtime. On Windows, ensure <c>SphinxSDK.dll</c> is on <c>PATH</c> or
/// alongside the application; on Linux, ensure <c>libsphinxsdk.so</c> is on
/// <c>LD_LIBRARY_PATH</c>.
/// </para>
/// </remarks>
public sealed class SphinxCameraSource : ICameraSource
{
    private readonly SphinxCameraOptions _options;
    private IntPtr _deviceHandle;
    private bool _opened;
    private bool _disposed;

    /// <summary>Creates a source with the given SphinxSDK options.</summary>
    public SphinxCameraSource(SphinxCameraOptions? options = null)
    {
        _options = options ?? new SphinxCameraOptions();
    }

    /// <inheritdoc />
    public bool IsOpened => _opened && _deviceHandle != IntPtr.Zero;

    /// <inheritdoc />
    public void Open()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Initialise the SDK runtime (idempotent).
        int initResult = SphinxNative.SPHINX_Initialize();
        if (initResult != SphinxNative.SPHINX_OK)
        {
            throw new InvalidOperationException(
                $"SphinxSDK initialisation failed (error code {initResult}).");
        }

        // Enumerate devices and open the requested one.
        int deviceCount = SphinxNative.SPHINX_GetDeviceCount();
        if (deviceCount <= 0)
        {
            throw new InvalidOperationException("No SphinxSDK cameras found.");
        }

        if (_options.DeviceIndex >= deviceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(_options.DeviceIndex),
                $"Requested device {_options.DeviceIndex} but only {deviceCount} device(s) available.");
        }

        _deviceHandle = SphinxNative.SPHINX_OpenDevice(_options.DeviceIndex);
        if (_deviceHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to open SphinxSDK device at index {_options.DeviceIndex}.");
        }

        // Configure acquisition parameters.
        if (_options.FrameWidth > 0)
        {
            SphinxNative.SPHINX_SetIntFeature(_deviceHandle, "Width", _options.FrameWidth);
        }

        if (_options.FrameHeight > 0)
        {
            SphinxNative.SPHINX_SetIntFeature(_deviceHandle, "Height", _options.FrameHeight);
        }

        if (_options.PixelFormat is not null)
        {
            SphinxNative.SPHINX_SetEnumFeature(_deviceHandle, "PixelFormat", _options.PixelFormat);
        }

        int startResult = SphinxNative.SPHINX_StartAcquisition(_deviceHandle);
        if (startResult != SphinxNative.SPHINX_OK)
        {
            SphinxNative.SPHINX_CloseDevice(_deviceHandle);
            _deviceHandle = IntPtr.Zero;
            throw new InvalidOperationException(
                $"SphinxSDK start-acquisition failed (error code {startResult}).");
        }

        _opened = true;
    }

    /// <inheritdoc />
    public bool TryReadFrame(Mat frame)
    {
        if (!_opened || _deviceHandle == IntPtr.Zero)
        {
            return false;
        }

        IntPtr bufferHandle = SphinxNative.SPHINX_GrabFrame(
            _deviceHandle, _options.GrabTimeoutMs);
        if (bufferHandle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            int width = SphinxNative.SPHINX_Buffer_GetWidth(bufferHandle);
            int height = SphinxNative.SPHINX_Buffer_GetHeight(bufferHandle);
            int stride = SphinxNative.SPHINX_Buffer_GetStride(bufferHandle);
            IntPtr dataPtr = SphinxNative.SPHINX_Buffer_GetDataPointer(bufferHandle);
            int pixelType = SphinxNative.SPHINX_Buffer_GetPixelType(bufferHandle);

            if (dataPtr == IntPtr.Zero || width <= 0 || height <= 0)
            {
                return false;
            }

            // Wrap the native buffer in a temporary Mat (no copy).
            using var raw = new Mat(height, width, PixelTypeToMatType(pixelType), dataPtr, stride);

            // Convert to BGR 8-bit as expected by the downstream pipeline.
            ConvertToBgr(raw, frame, pixelType);
            return !frame.Empty();
        }
        finally
        {
            SphinxNative.SPHINX_ReleaseBuffer(bufferHandle);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_deviceHandle != IntPtr.Zero)
        {
            if (_opened)
            {
                SphinxNative.SPHINX_StopAcquisition(_deviceHandle);
            }

            SphinxNative.SPHINX_CloseDevice(_deviceHandle);
            _deviceHandle = IntPtr.Zero;
        }

        _opened = false;
    }

    private static MatType PixelTypeToMatType(int pixelType) => pixelType switch
    {
        SphinxNative.SPHINX_PIXEL_MONO8 => MatType.CV_8UC1,
        SphinxNative.SPHINX_PIXEL_MONO16 => MatType.CV_16UC1,
        SphinxNative.SPHINX_PIXEL_BGR8 => MatType.CV_8UC3,
        SphinxNative.SPHINX_PIXEL_RGB8 => MatType.CV_8UC3,
        SphinxNative.SPHINX_PIXEL_BAYERRG8 => MatType.CV_8UC1,
        SphinxNative.SPHINX_PIXEL_BAYERGB8 => MatType.CV_8UC1,
        _ => MatType.CV_8UC1,
    };

    private static void ConvertToBgr(Mat source, Mat destination, int pixelType)
    {
        switch (pixelType)
        {
            case SphinxNative.SPHINX_PIXEL_BGR8:
                source.CopyTo(destination);
                break;
            case SphinxNative.SPHINX_PIXEL_RGB8:
                Cv2.CvtColor(source, destination, ColorConversionCodes.RGB2BGR);
                break;
            case SphinxNative.SPHINX_PIXEL_MONO8:
            case SphinxNative.SPHINX_PIXEL_MONO16:
                Cv2.CvtColor(source, destination, ColorConversionCodes.GRAY2BGR);
                break;
            case SphinxNative.SPHINX_PIXEL_BAYERRG8:
                Cv2.CvtColor(source, destination, ColorConversionCodes.BayerRG2BGR);
                break;
            case SphinxNative.SPHINX_PIXEL_BAYERGB8:
                Cv2.CvtColor(source, destination, ColorConversionCodes.BayerGB2BGR);
                break;
            default:
                Cv2.CvtColor(source, destination, ColorConversionCodes.GRAY2BGR);
                break;
        }
    }
}

/// <summary>
/// Configuration for <see cref="SphinxCameraSource"/>.
/// </summary>
public sealed class SphinxCameraOptions
{
    /// <summary>Zero-based SphinxSDK device index.</summary>
    public int DeviceIndex { get; set; } = 0;

    /// <summary>Desired capture width in pixels, or 0 to use the device default.</summary>
    public int FrameWidth { get; set; } = 0;

    /// <summary>Desired capture height in pixels, or 0 to use the device default.</summary>
    public int FrameHeight { get; set; } = 0;

    /// <summary>
    /// GenICam pixel format name (e.g. "Mono8", "RGB8", "BayerRG8"), or
    /// <c>null</c> to use the device default.
    /// </summary>
    public string? PixelFormat { get; set; }

    /// <summary>Timeout in milliseconds for <see cref="SphinxCameraSource.TryReadFrame"/>.</summary>
    public int GrabTimeoutMs { get; set; } = 1000;
}

/// <summary>
/// P/Invoke declarations for SphinxSDK v2.1.4.1 native library.
/// </summary>
/// <remarks>
/// The SDK ships as <c>SphinxSDK.dll</c> (Windows) or <c>libsphinxsdk.so</c>
/// (Linux). Ensure the native library is discoverable at runtime.
/// </remarks>
internal static partial class SphinxNative
{
    private const string LibraryName = "SphinxSDK";

    // Status codes
    public const int SPHINX_OK = 0;

    // Pixel type constants
    public const int SPHINX_PIXEL_MONO8 = 0x01080001;
    public const int SPHINX_PIXEL_MONO16 = 0x01100007;
    public const int SPHINX_PIXEL_BGR8 = 0x02180015;
    public const int SPHINX_PIXEL_RGB8 = 0x02180014;
    public const int SPHINX_PIXEL_BAYERRG8 = 0x01080009;
    public const int SPHINX_PIXEL_BAYERGB8 = 0x0108000B;

    // SDK lifecycle
    [LibraryImport(LibraryName, EntryPoint = "SPHINX_Initialize")]
    public static partial int SPHINX_Initialize();

    // Device enumeration
    [LibraryImport(LibraryName, EntryPoint = "SPHINX_GetDeviceCount")]
    public static partial int SPHINX_GetDeviceCount();

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_OpenDevice")]
    public static partial IntPtr SPHINX_OpenDevice(int deviceIndex);

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_CloseDevice")]
    public static partial int SPHINX_CloseDevice(IntPtr deviceHandle);

    // Feature access
    [LibraryImport(LibraryName, EntryPoint = "SPHINX_SetIntFeature", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int SPHINX_SetIntFeature(IntPtr deviceHandle, string featureName, int value);

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_SetEnumFeature", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int SPHINX_SetEnumFeature(IntPtr deviceHandle, string featureName, string value);

    // Acquisition
    [LibraryImport(LibraryName, EntryPoint = "SPHINX_StartAcquisition")]
    public static partial int SPHINX_StartAcquisition(IntPtr deviceHandle);

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_StopAcquisition")]
    public static partial int SPHINX_StopAcquisition(IntPtr deviceHandle);

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_GrabFrame")]
    public static partial IntPtr SPHINX_GrabFrame(IntPtr deviceHandle, int timeoutMs);

    // Buffer access
    [LibraryImport(LibraryName, EntryPoint = "SPHINX_Buffer_GetWidth")]
    public static partial int SPHINX_Buffer_GetWidth(IntPtr bufferHandle);

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_Buffer_GetHeight")]
    public static partial int SPHINX_Buffer_GetHeight(IntPtr bufferHandle);

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_Buffer_GetStride")]
    public static partial int SPHINX_Buffer_GetStride(IntPtr bufferHandle);

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_Buffer_GetDataPointer")]
    public static partial IntPtr SPHINX_Buffer_GetDataPointer(IntPtr bufferHandle);

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_Buffer_GetPixelType")]
    public static partial int SPHINX_Buffer_GetPixelType(IntPtr bufferHandle);

    [LibraryImport(LibraryName, EntryPoint = "SPHINX_ReleaseBuffer")]
    public static partial int SPHINX_ReleaseBuffer(IntPtr bufferHandle);
}
