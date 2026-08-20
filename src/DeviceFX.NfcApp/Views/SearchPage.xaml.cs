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
        var viewmodel = BindingContext as MainViewModel;
        viewmodel?.SetSearchPageVisible(true);
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(1);
            searchBar.Focus();
        });
        if (viewmodel == null) return;
        if (!viewmodel.Settings.User.IsLoggedIn) await viewmodel.BackCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is MainViewModel viewmodel)
            viewmodel.SetSearchPageVisible(false);
        base.OnDisappearing();
    }
}