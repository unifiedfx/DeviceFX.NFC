using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Helpers.Preference;
using Microsoft.Extensions.Configuration;

namespace DeviceFX.NfcApp.Model;

public partial class Settings : ObservableValidator
{
    [ObservableProperty]
    [Preference<string>("asset-tag")]
    private string asssetTag;

    [ObservableProperty]
    [Preference<bool>("include-location", false)]
    private bool includeLocation;

    public Settings(IConfiguration configuration) => 
        Webex = configuration.GetSection("AppSettings").Get<WebexSettings>() ?? new WebexSettings();

    public WebexSettings Webex { get; }
    public UserProfile User { get; } = new();
}