using DeviceFX.NfcApp.ViewModels;
using DeviceFX.NfcApp.Views.Shared;

namespace DeviceFX.NfcApp.Views;

public partial class SearchPage : StepContentPage
{
    public SearchPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(1);
            searchBar.Focus();
        });
        var viewmodel = BindingContext as MainViewModel;
        if(viewmodel == null) return;
        if (!viewmodel.Settings.User.IsLoggedIn) await viewmodel.BackCommand.ExecuteAsync(null);
    }
}