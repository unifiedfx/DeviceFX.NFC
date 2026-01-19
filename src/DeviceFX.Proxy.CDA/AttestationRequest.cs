using System.ComponentModel.DataAnnotations;

namespace DeviceFX.Proxy.CDA;

/// <summary>
/// Request model for iOS Device Attestation exchange
/// </summary>
public class AttestationRequest
{
    /// <summary>
    /// The platform to Attest: Apple or Google
    /// </summary>
    [Required]
    public required string Platform { get; set; }
    /// <summary>
    /// Base64-encoded attestation object from DCAppAttestService.attestKey
    /// </summary>
    [Required]
    public required string AttestationToken { get; set; }

    /// <summary>
    /// The key identifier returned by DCAppAttestService.generateKey
    /// </summary>
    public string? KeyId { get; set; }
    
    /// <summary>
    /// Optional device identifier for tracking purposes
    /// </summary>
    public string? DeviceId { get; set; }
}

/// <summary>
/// Response model for attestation endpoints
/// </summary>
public class AttestationResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public int? ExpiresIn { get; set; }
    public string? Error { get; set; }
}