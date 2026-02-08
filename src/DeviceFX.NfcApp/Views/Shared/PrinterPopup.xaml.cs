using CommunityToolkit.Maui.Views;
using DeviceFX.NfcApp.ViewModels;

namespace DeviceFX.NfcApp.Views.Shared;

public partial class PrinterPopup : Popup
{
    public PrinterPopup(PrinterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}