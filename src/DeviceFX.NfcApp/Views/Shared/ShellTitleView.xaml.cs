using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.ViewModels;
using DeviceFX.NfcApp.Helpers;

namespace DeviceFX.NfcApp.Views.Shared;

public partial class ShellTitleView : ContentView
{
    private CancellationTokenSource cancellationTokenSource = new ();
    private OperationState state;
    public ShellTitleView(AppViewModel appViewModel)
    {
        InitializeComponent();
        BindingContext = appViewModel;
        appViewModel.Operation.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != nameof(OperationBase.State) || state == appViewModel.Operation.State) return;
            state = appViewModel.Operation.State;
            cancellationTokenSource.Cancel();
            cancellationTokenSource = new ();
            if(state == OperationState.InProgress) _ = OperationButton.RotateAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        };
    }
}