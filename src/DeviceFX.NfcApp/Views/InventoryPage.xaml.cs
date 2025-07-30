using DeviceFX.NfcApp.ViewModels;
using DeviceFX.NfcApp.Views.Shared;

namespace DeviceFX.NfcApp.Views;

public partial class InventoryPage : StepContentPage
{
    public InventoryPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is not MainViewModel viewModel) return;
        await viewModel.LoadPhonesAsync();
    }
}