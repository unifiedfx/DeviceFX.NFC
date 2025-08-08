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
        var user = await webexService.GetUser();
        if(user == null && login) user = await webexService.LoginAsync();
        Settings.User.DisplayName = user?.displayName;
        ImageSource = user?.Picture ?? "grey_settings_gear.png";
        Settings.User.Picture = user?.Picture;
        Settings.User.IsLoggedIn = user != null;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await webexService.LogoutAsync();
        Settings.User.IsLoggedIn = false;
        ImageSource = "grey_settings_gear.png";
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query) => Settings.ApplyQuery(query);
    
    public async Task ReadAsync()
    {
        await Settings.LoadAsync();
        await GetUserAsync();
    }
}