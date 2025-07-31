using System.Globalization;

namespace DeviceFX.NfcApp.Helpers.Converters;

public class StringMapperConverter : IValueConverter
{
    public object DefaultValue { get; set; } = "Default Value";
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var dict = parameter as ResourceDictionary;
        if (dict != null && value is not null)
        {
            var key = value.ToString();
            return dict.ContainsKey(key) ? (string)dict[key] : DefaultValue;
        }
        return value; // Fallback
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}