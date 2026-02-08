using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.ViewModels;

namespace DeviceFX.NfcApp;

public partial class App : Application
{
    private readonly IServiceProvider serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        UserAppTheme = AppTheme.Light;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var appShell = serviceProvider.GetRequiredService<AppShell>();
        return new Window(appShell);
    }

    protected override async void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        var route = uri.ToRoute();
        if(route.ToString() == "//") route = new Uri("//start", UriKind.Relative);
        try
        {
            var query = ParseQueryString(uri.ToString());
            await Shell.Current.GoToAsync(route, query);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    private Dictionary<string, object> ParseQueryString(string query)
    {
        var queryParams = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        if (string.IsNullOrEmpty(query)) return queryParams;
        var index = query.LastIndexOf('?') + 1;
        var pairs = query[index..].Split('&');
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length != 2) continue;
            if (int.TryParse(keyValue[1], out var intResult)) queryParams[keyValue[0]] = intResult;
            else if (double.TryParse(keyValue[1], out var doubleResult)) queryParams[keyValue[0]] = doubleResult;
            else if (bool.TryParse(keyValue[1], out var boolResult)) queryParams[keyValue[0]] = boolResult;
            else if (DateTime.TryParse(keyValue[1], out var dateTimeResult)) queryParams[keyValue[0]] = dateTimeResult;
            else queryParams[keyValue[0]] = keyValue[1];
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