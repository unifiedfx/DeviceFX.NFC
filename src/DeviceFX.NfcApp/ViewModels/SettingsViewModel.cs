using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Helpers;

namespace DeviceFX.NfcApp.ViewModels;

public partial class SettingsViewModel(Settings settings, ILocationService locationService) : ObservableValidator, IQueryAttributable
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

    public void ApplyQueryAttributes(IDictionary<string, object> query) => Settings.ApplyQuery(query);
}