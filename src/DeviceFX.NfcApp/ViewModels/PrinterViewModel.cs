using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.ViewModels;

public partial class PrinterViewModel(Settings settings, IPopupService popupService, IPrintManager printManager) : ObservableObject
{
    private bool firstLoad = true;
    public required Settings Settings { get; init; } = settings;
    
    [ObservableProperty]
    private string? error;
    
    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task PrinterHostChangedAsync(string value)
    {
        if (firstLoad)
        {
            firstLoad = false;
            return;
        }
        if(string.IsNullOrWhiteSpace(value) || !Settings.PrinterEnabled) return;
        Settings.PrinterEnabled = false;
        Settings.PrinterName = null;
    }

    public bool CanExecuteClose() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanExecuteClose))]
    private async Task CloseAsync()
    {
        Error = null;
        await Settings.SaveAsync(nameof(Settings.PrinterHost));
        await Settings.SaveAsync(nameof(Settings.PrinterName));
        await Settings.SaveAsync(nameof(Settings.PrinterEnabled));
        await popupService.ClosePopupAsync(Shell.Current);
    }

    [RelayCommand]
    private async Task PrinterChangedAsync(bool value)
    {
        Error = null;
        Settings.PrinterName = null;
        if(!value) return;
        IsBusy = true;
        (CloseCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        var printer = await printManager.GetPrinter();
        Settings.PrinterName = printer?.Model;
        Settings.PrinterEnabled = printer != null;
        IsBusy = false;
        if (printer == null)
        {
            Console.WriteLine("Printer not found");
            Error = "Printer not found";
            await Task.Delay(1000);
        }
        (CloseCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
    }
}