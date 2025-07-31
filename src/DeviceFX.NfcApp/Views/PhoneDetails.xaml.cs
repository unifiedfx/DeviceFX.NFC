using CommunityToolkit.Maui.Views;
using DeviceFX.NfcApp.ViewModels;

namespace DeviceFX.NfcApp.Views;

public partial class PhoneDetailsPopup : Popup
{
    public PhoneDetailsPopup(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    
    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }
}