using System.ComponentModel.DataAnnotations;

namespace DeviceFX.Proxy.CDA;

public class CiscoOptions
{
    /// <summary>
    /// Cisco CDA Api Client Id
    /// </summary>
    [Required]
    public required string ClientId { get; set; }

    /// <summary>
    /// Cisco CDA Api Client Secret'
    /// </summary>
    [Required]
    public required string ClientSecret { get; set; }
    
    /// <summary>
    /// Your iOS App's Bundle ID (e.g., "com.company.appname")
    /// </summary>
    [Required]
    public required string AppleAppId { get; set; }
    
    /// <summary>
    /// Your Apple Team ID (10-character string)
    /// </summary>
    [Required]
    [MinLength(10)]
    public required string AppleTeamId { get; set; }
    
    /// <summary>
    /// Apple App Attest Root Certificate PEM
    /// </summary>
    [Required]
    public required string AppleAppAttestRootCertPem { get; set; }
    
    /// <summary>
    /// Google Service JSON file content
    /// </summary>
    [Required]
    public required string GoogleServiceJson { get; set; }
    
    /// <summary>
    /// Google App Package Name e.g. "com.devicefx.nfc"
    /// </summary>
    [Required]
    public required string GooglePackageName { get; set; }
    
    /// <summary>
    /// Google App Name e.g. "DeviceFX NFC"
    /// </summary>
    [Required]
    public required string GoogleAppName { get; set; }
    
    /// <summary>
    /// Secret key for signing JWT tokens (min 32 characters)
    /// </summary>
    [Required]
    [MinLength(32)]
    public required string SigningKey { get; set; }
    
    /// <summary>
    /// JWT token issuer
    /// </summary>
    public string Issuer { get; set; } = "DeviceFX.Proxy.CDA";
    
    /// <summary>
    /// JWT token audience
    /// </summary>
    public string Audience { get; set; } = "DeviceFX.NfcApp";
    
    /// <summary>
    /// JWT token expiration in minutes
    /// </summary>
    public int TokenExpirationSeconds { get; set; } = 3600;
    
    public int PermitLimit { get; set; } = 100;
    public int WindowSizeSeconds { get; set; } = 60;
}