using System.ComponentModel;
using DeviceFX.NfcApp.Helpers;

namespace DeviceFX.NfcApp.Views.Shared;

public abstract class StepContentPage : ContentPage
{
    public string Name => GetType().Name.Replace("Page", string.Empty).ToLowerInvariant();
    public int Priority { get; set; } = 0;
    public string? Group { get; set; }
    
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is not INotifyPropertyChanged viewModel) return;
        await viewModel.SaveAsync();
    }
}