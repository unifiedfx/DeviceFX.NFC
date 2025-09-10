using Android.App;
using Android.Content.PM;

namespace DeviceFX.NfcApp;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Android.Content.Intent.ActionView },
    Categories = new[] { 
        Android.Content.Intent.CategoryDefault,
        Android.Content.Intent.CategoryBrowsable 
    },
    DataPath="/callback",
    DataHost = "auth",
    DataScheme = "devicefxnfc")]

public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity { }