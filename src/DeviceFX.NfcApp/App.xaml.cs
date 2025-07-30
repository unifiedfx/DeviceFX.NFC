using DeviceFX.NfcApp.Helpers;

namespace DeviceFX.NfcApp;

public partial class App : Application
{
    private readonly IServiceProvider serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
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
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}