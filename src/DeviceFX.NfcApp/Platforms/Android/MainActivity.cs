using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using UFX.DeviceFX.NFC;

namespace DeviceFX.NfcApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "https",
    DataHost = "nfc.devicefx.com",
    DataPathPattern = "/.*",
    AutoVerify = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "devicefxnfc")]
public class MainActivity : MauiAppCompatActivity, NfcAdapter.IReaderCallback
{    public void OnTagDiscovered(Tag? tag) => NfcTagService.Current?.OnTagDiscovered(tag);
    protected override void OnResume()
    {
        base.OnResume();
        NfcTagService.Current?.OnResume();
    }
    protected override void OnPause()
    {
        base.OnPause();
        NfcTagService.Current?.OnPause();
    }

}