using DeviceFX.NfcApp.Services;

namespace DeviceFX.NfcApp.Model;

public class LabelWithLogoModel(int dpm = 8)
{
    private readonly float margin = 3;
    private readonly float fontSize = 6;
    
    private readonly int width;
    public int Width
    {
        get
        {
            return width > 0
                ? width * dpm
                : (int) (FontSize * 0.47 * Rows.Max(r => r.Length) + Margin * 2);
        }
        init => width = value;
    }

    public int Height => (Spacing + FontSize) * Rows.Count + Margin * 2 - Spacing;

    public int Margin
    {
        get => (int) (margin * dpm);
        init => margin = value;
    }

    public int FontSize
    {
        get => (int) (fontSize * dpm);
        init => fontSize = value;
    }
    public int Spacing => dpm * 1;
    public List<string> Rows { get; set; } = [];
    public required string LogoName { get; set; }
    
    public async Task<string> GetLogoZpl(int width)
    {
        await using var logoStream = await GetLogoStream();
        return ZebraPrinterClient.ImageHelper.GetZplGfaFromPng(logoStream, targetWidth: width);
    }

    public async Task<int> GetLogoHeight(int width)
    {
        await using var logoStream = await GetLogoStream();
        return ZebraPrinterClient.ImageHelper.GetImageHeight(logoStream, width);
    }

    private async Task<Stream> GetLogoStream() => await FileSystem.OpenAppPackageFileAsync(LogoName);
}