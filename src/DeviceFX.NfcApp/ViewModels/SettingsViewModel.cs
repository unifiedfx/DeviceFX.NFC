using System.ComponentModel;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Helpers;
using Microsoft.Extensions.Logging;

namespace DeviceFX.NfcApp.ViewModels;

public partial class SettingsViewModel : ObservableValidator, IQueryAttributable
{
    private const int VersionTapCountToToggle = 5;
    private static readonly TimeSpan VersionTapWindow = TimeSpan.FromSeconds(2);

    private readonly Settings settings;
    private readonly ILocationService locationService;
    private readonly IWebexService webexService;
    private readonly IMessenger messenger;
    private readonly IPopupService popupService;
    private readonly ILogger<SettingsViewModel> logger;
    private bool firstLoad = true;
    private int versionTapCount;
    private DateTime lastVersionTapUtc;
    private string appliedIntegrationMode;
    private bool changingIntegration;

    public SettingsViewModel(Settings settings, ILocationService locationService, IWebexService webexService, IMessenger messenger, IPopupService popupService, ILogger<SettingsViewModel> logger)
    {
        this.settings = settings;
        this.locationService = locationService;
        this.webexService = webexService;
        this.messenger = messenger;
        this.popupService = popupService;
        this.logger = logger;
        appliedIntegrationMode = settings.IntegrationMode;
        settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    public Settings Settings => settings;
    public UserProfile User => settings.User;
    public string AppVersion { get; } = $"Version {AppInfo.VersionString}";

    [RelayCommand]
    private async Task CloseAsync()
    {
        await Settings.SaveAsync();
        await Shell.Current.Navigation.PopModalAsync();
    }
    [RelayCommand]
    private async Task CancelAsync()
    {
        await Settings.LoadAsync();
        Settings.ApplySelectedIntegration();
        appliedIntegrationMode = Settings.IntegrationMode;
        await Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private async Task LocationChangedAsync(bool value)
    {
        if(!value) return;
        var location = await locationService.GetLocationAsync();
        if(location != null) return;
        Settings.IncludeLocation = false;
    }
    
    [RelayCommand]
    private async Task PrinterAsync()
    {
        await popupService.ShowPopupAsync<PrinterViewModel>(Shell.Current);
    }

    [RelayCommand]
    private async Task OrganizationChangedAsync()
    {
        var selectedId = Settings.User.Organization?.Id;
        if (Settings.Webex.OrgId == selectedId) return;
        var previousId = Settings.Webex.OrgId;
        try
        {
            if (!await webexService.SwitchOrganizationAsync(Settings.User, selectedId))
            {
                Settings.User.Organization = Settings.User.Organizations.FirstOrDefault(o => o.Id == previousId);
                return;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "OrganizationChangedAsync");
            Settings.User.Organization = Settings.User.Organizations.FirstOrDefault(o => o.Id == previousId);
            await Shell.Current.DisplayAlertAsync("Organization", e.Message, "Ok");
            return;
        }

        Settings.Webex.OrgId = Settings.User.Organization?.Id;
        await Settings.Webex.SaveAsync(nameof(Settings.Webex.OrgId));
        messenger.Send(new OrganizationMessage(Settings.User.Organization?.Id));
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Settings.IntegrationMode) || changingIntegration) return;
        _ = ChangeIntegrationAsync();
    }

    private async Task ChangeIntegrationAsync()
    {
        var mode = Settings.IntegrationMode;
        if (string.IsNullOrWhiteSpace(mode) || mode == appliedIntegrationMode) return;
        changingIntegration = true;
        try
        {
            if (Settings.User.IsLoggedIn)
                await LogoutAsync();
            Settings.ApplySelectedIntegration();
            await Settings.SaveAsync(nameof(Settings.IntegrationMode));
            appliedIntegrationMode = mode;
        }
        finally
        {
            changingIntegration = false;
        }
    }

    [RelayCommand]
    private async Task VersionTappedAsync()
    {
        var now = DateTime.UtcNow;
        if (now - lastVersionTapUtc > VersionTapWindow)
            versionTapCount = 0;
        lastVersionTapUtc = now;
        versionTapCount++;
        if (versionTapCount < VersionTapCountToToggle) return;
        versionTapCount = 0;
        Settings.IntegrationSelectorVisible = !Settings.IntegrationSelectorVisible;
        await Settings.SaveAsync(nameof(Settings.IntegrationSelectorVisible));
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            await GetUserAsync(true);
        }
        catch (Exception e)
        {
            await Shell.Current.DisplayAlertAsync("Login error", $"{e.Message}", "Ok");
        }
    }

    private async Task GetUserAsync(bool login = false)
    {
        await Settings.Webex.LoadAsync();
        if (!await webexService.UpdateUser(Settings.User, Settings.Webex.OrgId) && login)
        {
            if (!await webexService.LoginAsync(Settings.User, Settings.Webex.OrgId, Settings.Webex.Email))
            {
                await Settings.Webex.RemoveAsync(nameof(Settings.Webex.Email));
                await Settings.Webex.RemoveAsync(nameof(Settings.Webex.OrgId));
                return;
            }
            Settings.Webex.Email = Settings.User.Email;
            Settings.Webex.OrgId = Settings.User.Organization?.Id; 
            if(Settings.Webex.Email != null) await Settings.Webex.SaveAsync(nameof(Settings.Webex.Email));
            if(Settings.Webex.OrgId != null) await Settings.Webex.SaveAsync(nameof(Settings.Webex.OrgId));
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await webexService.LogoutAsync();
        Settings.User.Reset();
        Settings.Webex.Email = null;
        Settings.Webex.OrgId = null;
        await Settings.Webex.RemoveAsync(nameof(Settings.Webex.Email));
        await Settings.Webex.RemoveAsync(nameof(Settings.Webex.OrgId));
        messenger.Send(new OrganizationMessage(Settings.User.Organization?.Id));
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query) => _ = Settings.ApplyQuery(query);
    
    public async Task ReadAsync()
    {
        webexService.RetryLogin = RetryLogin;
        await Settings.LoadAsync();
        Settings.ApplySelectedIntegration();
        appliedIntegrationMode = Settings.IntegrationMode;
        if (firstLoad)
        {
            try
            {
                await GetUserAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "ReadAsync");
            }
            firstLoad = false;
        }
    }

    private async Task<bool> RetryLogin()
    {
        if (!MainThread.IsMainThread) return await MainThread.InvokeOnMainThreadAsync(RetryLogin);
        var retryLogin = await Shell.Current.DisplayAlertAsync("Login", "Session timeout, login again?", "Login", "Ok");
        if (!retryLogin)
        {
            Settings.User.Reset();
            return false;
        }
        await LoginAsync();
        return Settings.User.IsLoggedIn;
    }
}