namespace CSGenICam.SDK;

/// <summary>
/// Describes a discovered camera device.
/// </summary>
/// <param name="Id">Unique device identifier (serial number).</param>
/// <param name="Vendor">Vendor name reported by the device.</param>
/// <param name="Model">Model name reported by the device.</param>
/// <param name="TransportLayer">Transport layer (e.g. GigEVision, USB3Vision).</param>
/// <param name="InterfaceId">Identifier of the interface the camera is attached to.</param>
public sealed record CameraInfo(
    string Id,
    string Vendor,
    string Model,
    string TransportLayer,
    string InterfaceId);
