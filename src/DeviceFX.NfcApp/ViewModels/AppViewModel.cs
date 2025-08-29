using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.ViewModels;

public partial class AppViewModel(SettingsViewModel settings, Operation operation) : ObservableObject
{
    private string? title;
    public string? Title
    {
        get => title ?? Shell.Current?.CurrentItem?.CurrentItem?.CurrentItem?.Title ?? Shell.Current?.CurrentPage?.Title;
        set
        {
            if(value == title) return;
            title = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync() => await Shell.Current.GoToAsync("//settings");

    public Command OperationCommand => new Command(() =>
    {
        if(Operation.State == OperationState.Idle) Operation.State = OperationState.InProgress;
        else if(Operation.State == OperationState.InProgress) Operation.State = OperationState.Idle;
    });
    
    public Operation Operation { get; } = operation;
    public SettingsViewModel Settings { get; } = settings;
}