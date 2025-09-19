using DeviceFX.NfcApp.ViewModels;
using DeviceFX.NfcApp.Views.Shared;

namespace DeviceFX.NfcApp.Views;

public partial class ProvisionPage : StepContentPage
{
    public ProvisionPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var viewmodel = BindingContext as MainViewModel;
        if(viewmodel == null) return;
        if (viewmodel.SearchSelection == null) await viewmodel.BackCommand.ExecuteAsync(viewmodel.Settings.User.IsLoggedIn ? null : "start");
    }
}