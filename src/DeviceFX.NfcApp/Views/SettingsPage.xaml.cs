using DeviceFX.NfcApp.ViewModels;

namespace DeviceFX.NfcApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel settingsViewModel;
    
    public SettingsPage(SettingsViewModel settingsViewModel)
    {
        BindingContext = this.settingsViewModel = settingsViewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await settingsViewModel.ReadAsync();
    }
}