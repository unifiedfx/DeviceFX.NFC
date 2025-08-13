using Foundation;

namespace DeviceFX.NfcApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp().GetAwaiter().GetResult();
}