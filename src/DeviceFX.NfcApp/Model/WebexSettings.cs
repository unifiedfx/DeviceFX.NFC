using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Helpers.Preference;

namespace DeviceFX.NfcApp.Model;

public partial class WebexSettings : ObservableObject
{
    [ObservableProperty]
    [Preference<string>("client-id","CLIENT_ID")]
    private string clientId;

    [ObservableProperty]
    private string redirectUrl ="devicefxnfc://auth/callback";

    [ObservableProperty]
    [Preference<string>("auth-url", "https://webexapis.com/v1/authorize")]
    private string authUrl;

    [ObservableProperty]
    [Preference<string>("scopes", "identity:people_read spark:organizations_read identity:organizations_read")]
    private string scopes;

    [ObservableProperty]
    [Preference<long>("expires-in", 0)]
    private long tokenExpires;

    [ObservableProperty]
    [SecurePreference<string>("access-token")]
    private string? accessToken;
}