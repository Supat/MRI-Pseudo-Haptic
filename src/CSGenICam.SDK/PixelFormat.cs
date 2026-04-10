namespace CSGenICam.SDK;

/// <summary>
/// Common GenICam PFNC pixel formats.
/// </summary>
public enum PixelFormat
{
    /// <summary>Unknown or unsupported format.</summary>
    Unknown = 0,

    /// <summary>8-bit monochrome.</summary>
    Mono8,

    /// <summary>10-bit monochrome packed into 16 bits.</summary>
    Mono10,

    /// <summary>12-bit monochrome packed into 16 bits.</summary>
    Mono12,

    /// <summary>16-bit monochrome.</summary>
    Mono16,

    /// <summary>8-bit Bayer RG pattern.</summary>
    BayerRG8,

    /// <summary>24-bit RGB.</summary>
    RGB8,

    /// <summary>24-bit BGR.</summary>
    BGR8,
}
