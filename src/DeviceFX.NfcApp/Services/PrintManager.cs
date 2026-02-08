using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Services;

public class PrintManager(Settings settings) : IPrintManager
{

    public async Task<PrinterInfo?> GetPrinter(CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(settings.PrinterHost)) return null;
        var parts = settings.PrinterHost.Split(':');
        var address = parts[0];
        var port = parts.Length > 1 ? int.Parse(parts[1]) : 9100;
        var printer = new PrinterInfo(address, port);
        if(!await printer.QueryAsync(cancellationToken)) return null;
        return printer;
    }

    public async Task<bool> PrintAsync(List<string> rows, CancellationToken cancellationToken = default)
    {
        var printer = await GetPrinter(cancellationToken);
        if (printer == null) return false;
        var renderer = new LabelRenderer();
        var zpl = await renderer.Render(new LabelWithLogoModel(printer.Dpm)
        {
            LogoName = "Logo-WithText-BW.png",
            Width = 85,
            Rows = rows
        });
        var response = await printer.SendCommandAsync(zpl, false, cancellationToken);
        return response != null;
    }

    public async Task<bool> CanPrintAsync(CancellationToken cancellationToken = default)
    {
        var printer = await GetPrinter(cancellationToken);
        if (printer == null) return false;
        return await printer.QueryAsync(cancellationToken);
    }
}