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

    private async void SearchResults_OnScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        // await searchBar.HideKeyboardAsync();
    }

    private async void SearchBar_OnUnfocused(object? sender, FocusEventArgs e)
    {
        await searchBar.HideKeyboardAsync();
    }

    private async void SearchResults_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        await searchBar.HideKeyboardAsync();
    }
}