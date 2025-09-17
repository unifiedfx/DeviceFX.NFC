using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Helpers.Preference;
using Microsoft.Extensions.Configuration;

namespace DeviceFX.NfcApp.Model;

public partial class Settings : ObservableValidator
{
    [ObservableProperty]
    [Preference<string>("asset-tag")]
    private string? assetTag;

    [ObservableProperty]
    [Preference<bool>("include-location", false)]
    private bool includeLocation;

    [ObservableProperty]
    [Preference<string>("auto-number")]
    private string? autoNumber;

    [ObservableProperty]
    [Preference<bool>("enable-debug")]
    private bool enableDebug;
    
    public Settings(IConfiguration configuration) => 
        Webex = configuration.GetSection("AppSettings").Get<WebexSettings>() ?? new WebexSettings();

    public WebexSettings Webex { get; }
    public UserProfile User { get; } = new();
}