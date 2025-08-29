using CommunityToolkit.Maui.Core.Platform;
using DeviceFX.NfcApp.Views.Shared;

namespace DeviceFX.NfcApp.Views;

public partial class SearchPage : StepContentPage
{
    public SearchPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(1);
            searchBar.Focus();
        });
    }
}