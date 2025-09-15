using System.Reflection;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.Messaging;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Services;
using DeviceFX.NfcApp.ViewModels;
using DeviceFX.NfcApp.Views;
using DeviceFX.NfcApp.Views.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using DeviceFX.NfcApp.Helpers;
using FFImageLoading.Maui;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using UFX.DeviceFX.NFC;

namespace DeviceFX.NfcApp;

public static class MauiProgram
{
    public static async Task<MauiApp> CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseFFImageLoading()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
#if IOS
        // https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-9?view=net-maui-9.0#collectionview-and-carouselview
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
            handlers.AddHandler<Microsoft.Maui.Controls.CarouselView, Microsoft.Maui.Controls.Handlers.Items2.CarouselViewHandler2>();
        });
#endif
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
                    if(activity.Intent?.DataString == null || activity.Intent.Data?.Host == "auth") return;
                    var uri = new Uri(activity.Intent.DataString);
                    Application.Current?.SendOnAppLinkRequestReceived(uri);
                });
                android.OnNewIntent((activity, intent) =>
                {
                    if(intent?.DataString == null || intent.Data?.Host == "auth") return;
                    var uri = new Uri(intent.DataString);
                    Application.Current?.SendOnAppLinkRequestReceived(uri);
                });
            });
#endif
        });
        await using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
        var configBuilder = new ConfigurationBuilder();
        if (stream != null) configBuilder.AddJsonStream(stream);
        var configuration = configBuilder.Build();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddTransientPopup<PhoneDetailsPopup, MainViewModel>();
        builder.Services.AddSingleton<WebexService>();
        builder.Services.AddSingleton<ISearchService>(provider => provider.GetRequiredService<WebexService>());
        builder.Services.AddSingleton<IWebexService>(provider => provider.GetRequiredService<WebexService>());
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IHttpClientFactory, HttpClientFactory>();
        builder.Services.AddSingleton<IInventoryService, InventoryService>();
        builder.Services.AddSingleton<IDeviceService, DeviceService>();
        builder.Services.AddTransientAssembly<StepContentPage>(typeof(App).GetTypeInfo().Assembly);
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<Settings>();
        builder.Services.AddSingleton<Operation>();
        builder.Services.AddSingleton<AppViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<ShellTitleView>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<IMessenger, WeakReferenceMessenger>();
        builder.Services.AddSingleton<WizardViewModelBase>(provider => provider.GetRequiredService<MainViewModel>());
        var applicationInsights = configuration.GetConnectionString("ApplicationInsights");
        if (!string.IsNullOrWhiteSpace(applicationInsights) || applicationInsights.Contains("__"))
        {
            var installationId = Preferences.Get("installationId", String.Empty);
            if (string.IsNullOrEmpty(installationId))
            {
                installationId = Guid.NewGuid().ToString();
                Preferences.Set("installationId", installationId);
            }
            builder.Services.AddSingleton<TelemetryClient>(provider =>
            {
                var config = new TelemetryConfiguration
                {
                    ConnectionString = applicationInsights
                };
                var telemetryClient = new TelemetryClient(config);
                telemetryClient.Context.Component.Version = AppInfo.VersionString;
                telemetryClient.Context.Session.Id = installationId;
                telemetryClient.Context.Device.OperatingSystem = $"{DeviceInfo.Platform} {DeviceInfo.Current.VersionString}";
                telemetryClient.Context.Device.Model = DeviceInfo.Model;
                telemetryClient.Context.Device.OemName = DeviceInfo.Manufacturer;
                telemetryClient.Context.Device.Type = DeviceInfo.DeviceType.ToString();
                return telemetryClient;
            });
            builder.Logging.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
            {
                config.ConnectionString = applicationInsights;
            }, configureApplicationInsightsLoggerOptions: (options) =>
            {
                options.IncludeScopes = true;
            });
            builder.Logging.AddFilter((category, level) =>
            {
                if (category == null) return false;
                if(category.StartsWith(nameof(Microsoft)) && level <= LogLevel.Error) return false;
                return true;
            });
        }
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