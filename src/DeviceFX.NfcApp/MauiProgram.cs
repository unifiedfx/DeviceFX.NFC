using System.Reflection;
using CommunityToolkit.Maui;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Services;
using DeviceFX.NfcApp.ViewModels;
using DeviceFX.NfcApp.Views;
using DeviceFX.NfcApp.Views.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using DeviceFX.NfcApp.Helpers;
using UFX.DeviceFX.NFC;

namespace DeviceFX.NfcApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        builder.ConfigureLifecycleEvents(events =>
        {
#if IOS
            events.AddiOS(ios =>
            {
                ios
                    .OpenUrl((app, url, options) =>
                    {
                        if (Uri.TryCreate(url.AbsoluteString, UriKind.Absolute, out var uri))
                        {
                            Application.Current?.SendOnAppLinkRequestReceived(uri);
                        }

                        return true;
                    })
                    .ContinueUserActivity((app, userActivity, completionHandler) =>
                    {
                        if (userActivity.ActivityType != Foundation.NSUserActivityType.BrowsingWeb) return true;
                        Application.Current?.SendOnAppLinkRequestReceived(userActivity.WebPageUrl);
                        return true;
                    });
            });
#elif ANDROID
            events.AddAndroid(android =>
            {
                android.OnCreate((activity, bundle) =>
                {
                    if(activity.Intent?.Data == null) return;
                    var uri = new Uri(activity.Intent?.Data?.ToString());
                    Application.Current?.SendOnAppLinkRequestReceived(uri);
                });
                android.OnNewIntent((activity, intent) =>
                {
                    if(intent?.Data == null) return;
                    var uri = new Uri(intent?.Data?.ToString());
                    Application.Current?.SendOnAppLinkRequestReceived(uri);
                });
            });
#endif
        });
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IHttpClientFactory, HttpClientFactory>();
        builder.Services.AddSingleton<IInventoryService, InventoryService>();
        builder.Services.AddSingleton<IDeviceService, DeviceService>();
        builder.Services.AddSingleton<ISearchService, SearchService>();
        builder.Services.AddTransientAssembly<StepContentPage>(typeof(App).GetTypeInfo().Assembly);
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<Settings>();
        builder.Services.AddSingleton<Operation>();
        builder.Services.AddSingleton<AppViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<ShellTitleView>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<WizardViewModelBase>(provider => provider.GetRequiredService<MainViewModel>());
        builder.AddNfc();
#if ANDROID
        // Custom mapping for Android to remove unnecessary space on the left side of the Shell TitleView
        //https://stackoverflow.com/questions/78704179/how-to-disable-unnecessary-space-on-left-side-of-shell-titleview
        Microsoft.Maui.Handlers.ToolbarHandler.Mapper.AppendToMapping("CustomNavigationView", (handler, view) =>
        {
            handler.PlatformView.ContentInsetStartWithNavigation = 0;
            handler.PlatformView.SetContentInsetsAbsolute(0, 0);
        });
#endif


#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}