using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Helpers;
using Microsoft.Extensions.Logging;

namespace DeviceFX.NfcApp.ViewModels;

public partial class SettingsViewModel(Settings settings, ILocationService locationService, IWebexService webexService, IMessenger messenger, ILogger<SettingsViewModel> logger) : ObservableValidator, IQueryAttributable
{
    private bool firstLoad = true;
    [ObservableProperty]
    private string imageSource = "grey_settings_gear.png";
    public Settings Settings { get; } = settings;

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
    private async Task OrganizationChangedAsync()
    {
        if(Settings.Webex.OrgId == Settings.User.Organization?.Id) return;
        Settings.Webex.OrgId = Settings.User.Organization?.Id;
        Settings.Webex.SaveAsync(nameof(Settings.Webex.OrgId));
        messenger.Send(new OrganizationMessage(Settings.User.Organization?.Id));
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
            await Shell.Current.DisplayAlert("Login error", $"{e.Message}", "Ok");
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
        ImageSource = Settings.User.Picture ?? "grey_settings_gear.png";
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
        ImageSource = "grey_settings_gear.png";
        messenger.Send(new OrganizationMessage(Settings.User.Organization?.Id));
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query) => Settings.ApplyQuery(query);
    
    public async Task ReadAsync()
    {
        webexService.RetryLogin = RetryLogin;
        await Settings.LoadAsync();
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
        var retryLogin = await Shell.Current.DisplayAlert("Login", "Session timeout, login again?", "Login", "Ok");
        if (!retryLogin)
        {
            Settings.User.Reset();
            ImageSource = "grey_settings_gear.png";
            return false;
        }
        await LoginAsync();
        return Settings.User.IsLoggedIn;
    }
}