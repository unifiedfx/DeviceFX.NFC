using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Helpers.Preference;

namespace DeviceFX.NfcApp.Model;

public partial class WebexSettings : ObservableObject
{
    [ObservableProperty]
    private string clientId;

    [ObservableProperty]
    private string redirectUrl;

    [ObservableProperty]
    private string authUrl;

    [ObservableProperty]
    private string scopes;

    [ObservableProperty]
    [Preference<long>("expires-in", 0)]
    private long tokenExpires;

    [ObservableProperty]
    [SecurePreference<string>("access-token")]
    private string? accessToken;

    [ObservableProperty]
    [Preference<string>("email")]
    private string? email;
}