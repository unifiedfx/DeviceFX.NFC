using CommunityToolkit.Mvvm.ComponentModel;

namespace DeviceFX.NfcApp.Model;

public abstract partial class OperationBase : ObservableObject
{
    private OperationState state;
    public OperationState State 
    {
        get => state;
        set
        {
            SetProperty(ref state, value);
            OnPropertyChanged(nameof(TintColor));
        }
    }

    public Color TintColor => state switch
    {
        OperationState.InProgress => Colors.Orange,
        OperationState.Success => Colors.Green,
        OperationState.Failure => Colors.Red,
        _ => Colors.Gray
    };
}