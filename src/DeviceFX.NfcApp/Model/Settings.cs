using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Helpers.Preference;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;

namespace DeviceFX.NfcApp.Model;

public partial class Settings : ObservableValidator
{
    private readonly IConfiguration configuration;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDeviceFx))]
    [Preference<string>("integration-mode", IntegrationModes.DeviceFX)]
    private string integrationMode = IntegrationModes.DeviceFX;

    [ObservableProperty]
    [Preference<bool>("integration-selector-visible", false)]
    private bool integrationSelectorVisible;

    public bool IsDeviceFx =>
        !string.Equals(IntegrationMode, IntegrationModes.Webex, StringComparison.OrdinalIgnoreCase);

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
    
    public Settings(IConfiguration configuration)
    {
        this.configuration = configuration;
        Webex = Bind<WebexSettings>(configuration.GetSection("AppSettings"));
        integrationMode = Preferences.Default.Get("integration-mode", IntegrationModes.DeviceFX) ?? IntegrationModes.DeviceFX;
        integrationSelectorVisible = Preferences.Default.Get("integration-selector-visible", false);
        ApplySelectedIntegration();
    }

    public WebexSettings Webex { get; }
    public UserProfile User { get; } = new();

    public void ApplySelectedIntegration()
    {
        var shared = Bind<WebexSettings>(configuration.GetSection("AppSettings"));
        var deviceFx = Bind<IntegrationSettings>(configuration.GetSection($"AppSettings:{IntegrationModes.DeviceFX}"));
        var webex = Bind<IntegrationSettings>(configuration.GetSection($"AppSettings:{IntegrationModes.Webex}"));

        // Safe in both modes.
        if (!string.IsNullOrWhiteSpace(shared.RedirectUrl)) Webex.RedirectUrl = shared.RedirectUrl;
        if (shared.LicenseFilters is { Length: > 0 }) Webex.LicenseFilters = shared.LicenseFilters;

        var anonymousCda = FirstConfigured(IsDeviceFx ? deviceFx.CDAServiceUrl : webex.CDAServiceUrl, shared.CdaServiceUrl);
        if (anonymousCda != null)
            Webex.CdaServiceUrl = anonymousCda;

        if (IsDeviceFx)
        {
            Webex.ClientId = IsConfigured(deviceFx.ClientId) ? deviceFx.ClientId! : "devicefx-nfc";
            Webex.Authority = RewriteLoopbackHost(FirstConfigured(deviceFx.Authority, "https://loginfx.com/identity")!);
            Webex.ApiHost = RewriteLoopbackHost(FirstConfigured(deviceFx.ApiHost, "https://app.devicefx.com")!);
            Webex.Scopes = FirstConfigured(deviceFx.Scopes, "openid profile email roles offline_access")!;
            Webex.AuthUrl = string.Empty;
            return;
        }

        Webex.ClientId = FirstConfigured(shared.ClientId, webex.ClientId, configuration["AppSettings:ClientId"]) ?? string.Empty;
        Webex.AuthUrl = FirstConfigured(webex.AuthUrl, shared.AuthUrl, "https://webexapis.com/v1/authorize")!;
        Webex.ApiHost = FirstConfigured(webex.ApiHost, "https://webexapis.com")!;
        Webex.Scopes = FirstConfigured(webex.Scopes, shared.Scopes)!;
        Webex.Authority = string.Empty;
    }

    private static string? FirstConfigured(params string?[] values)
    {
        foreach (var value in values)
        {
            if (IsConfigured(value)) return value;
        }

        return null;
    }

    private static bool IsConfigured(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Contains("__", StringComparison.Ordinal);

    // Android emulator loopback is 10.0.2.2, not localhost (iOS simulator can use localhost).
    private static string RewriteLoopbackHost(string url)
    {
#if ANDROID
        if (DeviceInfo.Current.DeviceType == DeviceType.Virtual)
        {
            return url
                .Replace("://localhost", "://10.0.2.2", StringComparison.OrdinalIgnoreCase)
                .Replace("://127.0.0.1", "://10.0.2.2", StringComparison.OrdinalIgnoreCase);
        }
#endif
        return url;
    }

    // .NET 10 removed DAM from ConfigurationBinder.Get<T>(), so annotate T here
    // or Release trim drops WebexSettings setters (CdaServiceUrl stays null).
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "T is preserved by DynamicallyAccessedMembers.All")]
    private static T Bind<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(IConfiguration section)
        where T : class, new() =>
        section.Get<T>() ?? new T();
}