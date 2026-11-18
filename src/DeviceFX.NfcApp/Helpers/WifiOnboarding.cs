namespace DeviceFX.NfcApp.Helpers;

/// <summary>
/// Adds the Wi-Fi settings to the NFC onboarding data. The key names must match Cisco's
/// "Prepare NFC onboarding data for Desk Phone 9800 Series" exactly: the phone doesn't
/// recognize any other spelling, so it can't apply the Wi-Fi settings and onboarding fails.
/// </summary>
public static class WifiOnboarding
{
    public const string NetworkNameKey = "Network_Name_1_";
    public const string UserIdKey = "Wi-Fi_User_ID_1_";
    public const string PasswordKey = "Wi-Fi_Password_1_";
    public const string SecurityModeKey = "Security_Mode_1_";
    public const string FrequencyBandKey = "Frequency_Band_1_";

    /// <summary>Written for the security mode or band when none is chosen.</summary>
    public const string Auto = "Auto";

    /// <summary>
    /// Adds the network name, user ID and password when they have a value, and always adds the
    /// security mode and frequency band, defaulting to <see cref="Auto"/>. Pass null for a user ID
    /// or password that the security mode doesn't use.
    /// </summary>
    public static void AddTo(IDictionary<string, string> onboarding, string? networkName, string? userId,
        string? password, string? securityMode, string? frequencyBand)
    {
        if (!string.IsNullOrWhiteSpace(networkName)) onboarding.Add(NetworkNameKey, networkName);
        if (!string.IsNullOrWhiteSpace(userId)) onboarding.Add(UserIdKey, userId);
        if (!string.IsNullOrWhiteSpace(password)) onboarding.Add(PasswordKey, password);
        onboarding.Add(SecurityModeKey, string.IsNullOrWhiteSpace(securityMode) ? Auto : securityMode);
        onboarding.Add(FrequencyBandKey, string.IsNullOrWhiteSpace(frequencyBand) ? Auto : frequencyBand);
    }
}
