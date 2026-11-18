using DeviceFX.NfcApp.Helpers;
using Xunit;

namespace DeviceFX.NfcApp.Tests;

/// <summary>
/// The expected key names are copied from Cisco's "Prepare NFC onboarding data for Desk Phone
/// 9800 Series". The phone can't apply Wi-Fi settings written under any other name (#20).
/// </summary>
public class WifiOnboardingTests
{
    [Fact]
    public void AddTo_AllSettings_UsesTheDocumentedKeyNames()
    {
        var onboarding = new Dictionary<string, string>();

        WifiOnboarding.AddTo(onboarding, "Office", "alice", "secret", "EAP-PEAP", "5 GHz");

        Assert.Equal(new Dictionary<string, string>
        {
            ["Network_Name_1_"] = "Office",
            ["Wi-Fi_User_ID_1_"] = "alice",
            ["Wi-Fi_Password_1_"] = "secret",
            ["Security_Mode_1_"] = "EAP-PEAP",
            ["Frequency_Band_1_"] = "5 GHz",
        }, onboarding);
    }

    [Fact]
    public void AddTo_NoUserIdOrPassword_LeavesThemOut()
    {
        var onboarding = new Dictionary<string, string>();

        WifiOnboarding.AddTo(onboarding, "Guest", userId: null, password: null, "None", "2.4 GHz");

        Assert.Equal(new Dictionary<string, string>
        {
            ["Network_Name_1_"] = "Guest",
            ["Security_Mode_1_"] = "None",
            ["Frequency_Band_1_"] = "2.4 GHz",
        }, onboarding);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddTo_NoSecurityModeOrBand_DefaultsToAuto(string? missing)
    {
        var onboarding = new Dictionary<string, string>();

        WifiOnboarding.AddTo(onboarding, "Office", null, "secret", missing, missing);

        Assert.Equal("Auto", onboarding["Security_Mode_1_"]);
        Assert.Equal("Auto", onboarding["Frequency_Band_1_"]);
    }
}
