using System.Diagnostics.CodeAnalysis;
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
    
    [ObservableProperty]
    [Preference<string>("printer-host")]
    private string printerHost;
    
    [ObservableProperty]
    [Preference<bool>("printer-enabled")]
    private bool printerEnabled;
    
    [ObservableProperty]
    [Preference<string>("printer-name")]
    private string? printerName;
    
    public Settings(IConfiguration configuration) =>
        Webex = Bind<WebexSettings>(configuration.GetSection("AppSettings"));

    public WebexSettings Webex { get; }
    public UserProfile User { get; } = new();

    // .NET 10 removed DAM from ConfigurationBinder.Get<T>(), so annotate T here
    // or Release trim drops WebexSettings setters (CdaServiceUrl stays null).
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "T is preserved by DynamicallyAccessedMembers.All")]
    private static T Bind<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(IConfiguration section)
        where T : class, new() =>
        section.Get<T>() ?? new T();
}