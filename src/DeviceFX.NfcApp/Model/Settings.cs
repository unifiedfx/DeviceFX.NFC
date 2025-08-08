using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Helpers.Preference;

namespace DeviceFX.NfcApp.Model;

public partial class Settings : ObservableValidator
{
    [ObservableProperty]
    [Preference<string>("asset-tag")]
    private string asssetTag;

    [ObservableProperty]
    [Preference<bool>("include-location", false)]
    private bool includeLocation;
    
    public WebexSettings Webex { get; } = new();
    public UserProfile User { get; } = new();
}