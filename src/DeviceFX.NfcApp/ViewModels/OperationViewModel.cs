using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.ViewModels;

public class OperationViewModel(Operation operation) : ObservableObject
{
    public Operation Operation { get; } = operation;
}