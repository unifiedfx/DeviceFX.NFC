using DeviceFX.NfcApp.Services;

namespace DeviceFX.NfcApp.Abstractions;

public interface IPrintManager
{
    Task<PrinterInfo?> GetPrinter(CancellationToken cancellationToken = default);
    Task<bool> PrintAsync(List<string> rows, CancellationToken cancellationToken = default);
    Task<bool> CanPrintAsync(CancellationToken cancellationToken = default);
}

public class PrinterInfo(string address, int port = 9100)
{
    private readonly ZebraPrinterClient client = new(address, port);
    private int dpm = 8;
    public int Dpm => dpm;
    public string Address { get; init; } = address;
    public string? Model { get; set; }
    public string? Firmware { get; set; }

    public async Task<bool> QueryAsync(CancellationToken cancellationToken = default)
    {
        var hostInfo = await SendCommandAsync("~HI", true, cancellationToken);
        if(hostInfo == null) return false;
        var parts = hostInfo.Split(",", StringSplitOptions.TrimEntries);
        Model = parts.Length > 0 ? parts[0] : "Unknown Model";
        Firmware = parts.Length > 1 ? parts[1] : "Unknown Firmware";
        var printerDpm = parts.Length > 2 ? parts[2] : "8";
        return int.TryParse(printerDpm, out dpm);
    }

    public async Task<string?> SendCommandAsync(string command, bool expectResponse, CancellationToken cancellationToken = default) => 
        await client.SendCommandAsync(command, expectResponse, cancellationToken);
}