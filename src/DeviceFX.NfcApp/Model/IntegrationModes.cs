namespace DeviceFX.NfcApp.Model;

public static class IntegrationModes
{
    public const string DeviceFX = "DeviceFX";
    public const string Webex = "Webex";
}

/// <summary>
/// Keyed AppSettings object applied when the hidden integration selector changes.
/// </summary>
public class IntegrationSettings
{
    public string? ClientId { get; set; }
    public string? ApiHost { get; set; }
    public string? CDAServiceUrl { get; set; }
    public string? Authority { get; set; }
    public string? AuthUrl { get; set; }
    public string? Scopes { get; set; }
}
