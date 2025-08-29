using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Helpers;

namespace DeviceFX.NfcApp.ViewModels;

public partial class SettingsViewModel(Settings settings, ILocationService locationService, IWebexService webexService) : ObservableValidator, IQueryAttributable
{
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
    private async Task LoginAsync()
    {
        await GetUserAsync(true);
    }

    private async Task GetUserAsync(bool login = false)
    {
        await Settings.Webex.LoadAsync();
        var account = await webexService.GetAccount();
        if (account == null && login)
        {
            account = await webexService.LoginAsync(email: Settings.Webex.Email);
            Settings.Webex.Email = account?.Email;
            if(Settings.Webex.Email != null) await Settings.Webex.SaveAsync(nameof(Settings.Webex.Email));
        }
        Settings.User.Set(account);
        ImageSource = Settings.User.Picture ?? "grey_settings_gear.png";
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await webexService.LogoutAsync();
        Settings.User.Reset();
        Settings.Webex.Email = null;
        await Settings.Webex.RemoveAsync(nameof(Settings.Webex.Email));
        ImageSource = "grey_settings_gear.png";
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query) => Settings.ApplyQuery(query);
    
    public async Task ReadAsync()
    {
        webexService.RetryLogin = RetryLogin;
        await Settings.LoadAsync();
        await GetUserAsync();
    }

    private async Task<bool> RetryLogin()
    {
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