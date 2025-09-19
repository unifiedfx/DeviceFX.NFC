using System.Globalization;

namespace DeviceFX.NfcApp.Helpers;

public class ActivationCodeFormatter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string {Length: 16} code)
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