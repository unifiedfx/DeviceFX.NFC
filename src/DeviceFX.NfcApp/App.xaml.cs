using System.ComponentModel;
using System.Net;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace DeviceFX.NfcApp;

public partial class App : Application
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<App>? logger;
    private static Uri? pendingAppLink;
    private Dictionary<string, object>? pendingQuery;
    // Destination LoadAsync would overwrite query values just applied from the app link.
    private bool skipPreferenceLoad;

    public App(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        logger = serviceProvider.GetService<ILogger<App>>();
        UserAppTheme = AppTheme.Light;
        InitializeComponent();
    }

    internal static void StashAppLink(Uri uri) => pendingAppLink = uri;

    internal static void ForwardAppLink(Uri uri)
    {
        StashAppLink(uri);
        Current?.SendOnAppLinkRequestReceived(uri);
    }

    internal bool ConsumeSkipPreferenceLoad()
    {
        var skip = skipPreferenceLoad;
        skipPreferenceLoad = false;
        return skip;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var appShell = serviceProvider.GetRequiredService<AppShell>();
        return new Window(appShell);
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        StashAppLink(uri);
        // Cold start: Shell has no CurrentPage yet. AppShell.OnNavigated is the ready signal.
        if (Shell.Current?.CurrentPage is not null)
            _ = TryProcessPendingAppLinkAsync();
        else
            logger?.LogInformation("Stashed app link {Uri}; waiting for Shell to navigate", uri);
    }

    internal async Task TryProcessPendingAppLinkAsync()
    {
        if (pendingAppLink is null || Shell.Current?.CurrentPage is null) return;

        if (!MainThread.IsMainThread)
        {
            await MainThread.InvokeOnMainThreadAsync(TryProcessPendingAppLinkAsync);
            return;
        }

        var uri = pendingAppLink;
        pendingAppLink = null;
        var route = uri.ToShellRoute();
        var query = pendingQuery = ParseQueryString(uri);
        skipPreferenceLoad = true;
        try
        {
            logger?.LogInformation("Applying app link {Uri} as {Route}", uri, route);
            // Do not pass query into GoToAsync — Shell would keep it on the route and
            // re-apply IQueryAttributable (overwriting later edits) when a modal pops.
            await Shell.Current.GoToAsync(route, false);
        }
        catch (Exception e)
        {
            skipPreferenceLoad = false;
            logger?.LogError(e, "Failed to navigate app link {Uri}", uri);
        }

        // Settings is a cancelled Shell route pushed as a modal; query is applied after that page loads.
        if (route.Contains("settings", StringComparison.OrdinalIgnoreCase))
        {
            skipPreferenceLoad = false;
            return;
        }

        pendingQuery = null;
        if (Shell.Current.CurrentPage?.BindingContext is INotifyPropertyChanged viewModel)
            await viewModel.ApplyQuery(query);
    }

    internal async Task ApplyPendingAppLinkQueryAsync(INotifyPropertyChanged target)
    {
        if (pendingQuery is not { Count: > 0 } query) return;
        pendingQuery = null;
        await target.ApplyQuery(query);
    }

    private Dictionary<string, object> ParseQueryString(Uri uri)
    {
        var queryParams = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        var query = uri.Query;
        if (string.IsNullOrEmpty(query)) return queryParams;
        var pairs = query.TrimStart('?').Split('&');
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length != 2) continue;
            var key = WebUtility.UrlDecode(keyValue[0]);
            var value = WebUtility.UrlDecode(keyValue[1]);
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) continue;
            if (bool.TryParse(value, out var boolResult)) queryParams[key] = boolResult;
            else queryParams[key] = value;
        }
        if(queryParams.ContainsKey("wifi-name") && !string.IsNullOrWhiteSpace(queryParams["wifi-name"].ToString())) queryParams.TryAdd("wifi-include", true);
        if (!queryParams.ContainsKey("onboarding-mode"))
        {
            if (queryParams.ContainsKey("activation-code"))
            {
                queryParams.TryAdd("onboarding-mode", MainViewModel.OnboardingActivation);
            }
            else if (queryParams.ContainsKey("cloud-profile") || queryParams.ContainsKey("cloud-ca-rule"))
            {
                queryParams.TryAdd("onboarding-mode", MainViewModel.OnboardingCloud);
            }
            else if (queryParams.ContainsKey("cucm-server"))
            {
                queryParams.TryAdd("onboarding-mode", MainViewModel.OnboardingCUCM);
            }
        }
        return queryParams;
    }
}
