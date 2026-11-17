using DeviceFX.NfcApp.Model;
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
            SyncListSelection(viewmodel);
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

    private async void OnSearchResultsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BindingContext is not MainViewModel viewmodel) return;
        var selected = e.CurrentSelection.OfType<SearchResult>().FirstOrDefault();
        await viewmodel.SelectionChangedCommand.ExecuteAsync(selected);
    }

    private void SyncListSelection(MainViewModel? viewmodel)
    {
        // Android CollectionView keeps the last highlighted row after the page was hidden,
        // even when SearchSelection was cleared. Drop native selection and re-apply the VM pick.
        SearchResults.SelectionMode = SelectionMode.None;
        SearchResults.SelectionMode = SelectionMode.Single;
        SearchResults.SelectedItem = viewmodel?.SearchSelection;
    }
}