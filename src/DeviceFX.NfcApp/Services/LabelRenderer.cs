using System.Text;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Services;

public class LabelRenderer
{
    public async Task<string> Render(LabelWithLogoModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("^XA");
        sb.AppendLine($"^PW{model.Width + model.Margin * 2}");
        var logoWidth = model.Width - model.Margin * 2;
        var logoHeight = await model.GetLogoHeight(logoWidth);
        sb.AppendLine($"^LL{model.Height + model.Margin * 4 + logoHeight}");
        sb.AppendLine("^LS0");
        sb.AppendLine($"^CF0,{model.FontSize}");
        var linePos = model.Margin * 4 + logoHeight;
        foreach (var row in model.Rows)
        {
            sb.AppendLine($"^FO{model.Margin * 2},{linePos}^FD{row}^FS");
            linePos += model.FontSize + model.Spacing;
        }
        sb.AppendLine($"^FO{model.Margin},{model.Margin}^GB{model.Width},{logoHeight + model.Margin * 2},2^FS");
        sb.AppendLine($"^FO{model.Margin * 2},{model.Margin * 2}{await model.GetLogoZpl(logoWidth)}");
        sb.AppendLine($"^FO{model.Margin},{model.Margin * 3 + logoHeight}^GB{model.Width},{model.Height},2^FS");
        sb.AppendLine("^XZ");
        return sb.ToString();
    }
}