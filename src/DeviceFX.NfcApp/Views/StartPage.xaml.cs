using DeviceFX.NfcApp.ViewModels;
using DeviceFX.NfcApp.Views.Shared;

namespace DeviceFX.NfcApp.Views;

public partial class StartPage : StepContentPage
{
    private readonly SettingsViewModel settingsViewModel;

    public StartPage(SettingsViewModel settingsViewModel)
    {
        this.settingsViewModel = settingsViewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await settingsViewModel.ReadAsync();
    }
}