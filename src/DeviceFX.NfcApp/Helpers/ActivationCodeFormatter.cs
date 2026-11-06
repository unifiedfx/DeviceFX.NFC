using System.Globalization;

namespace DeviceFX.NfcApp.Helpers;

public class ActivationCodeFormatter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Short codes (#nnn) are shown as entered; only standard 16-digit codes are grouped.
        if (value is string code && ActivationCodeValidator.IsStandard(code))
        {
            return $"{code.Substring(0, 4)}-{code.Substring(4, 4)}-{code.Substring(8, 4)}-{code.Substring(12, 4)}";
        }
        return value; // Fallback to raw value
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException(); // Not needed for one-way binding
    }
}