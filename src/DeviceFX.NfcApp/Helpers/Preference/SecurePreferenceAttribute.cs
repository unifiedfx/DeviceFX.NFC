using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace DeviceFX.NfcApp.Helpers.Preference;

/// <summary>
/// Attach to Property or ObservableProperty Field to save and load secure preferences.
/// </summary>
/// <param name="key"></param>
/// <param name="defaultValue"></param>
/// <typeparam name="T"></typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field )]
public class SecurePreferenceAttribute<T>(string key, T? defaultValue = default) : PreferenceAttribute(key)
{
    public T? DefaultValue { get; } = defaultValue;

    public override async ValueTask LoadAsync(INotifyPropertyChanged instance, PropertyInfo property)
    {
        var value = await SecureStorage.Default.GetAsync(Key);
        switch (typeof(T))
        {
            case Type boolType when boolType == typeof(bool):
                if (bool.TryParse(value, out var b)) property.SetValue(instance, b);
                break;
            case Type intType when intType == typeof(int):
                if (int.TryParse(value, out var i)) property.SetValue(instance, i);
                break;
            case Type longType when longType == typeof(long):
                if (long.TryParse(value, out var l)) property.SetValue(instance, l);
                break;
            case Type dateTimeType when dateTimeType == typeof(DateTime):
                if (DateTime.TryParse(value, out var t)) property.SetValue(instance, t);
                break;
            default:
                property.SetValue(instance, value);
                break;
        }
    }

    public override async ValueTask SaveAsync(INotifyPropertyChanged instance, PropertyInfo property)
    {
        var value = property.GetValue(instance);
        switch (value)
        {
            case string s:
                await SecureStorage.Default.SetAsync(Key, s);
                break;
            case bool b:
                await SecureStorage.Default.SetAsync(Key, b.ToString());
                break;
            case int i:
                await SecureStorage.Default.SetAsync(Key, i.ToString());
                break;
            case long l:
                await SecureStorage.Default.SetAsync(Key, l.ToString());
                break;
            case DateTime t:
                await SecureStorage.Default.SetAsync(Key, t.ToString(CultureInfo.InvariantCulture));
                break;
        }
    }
    
    public override ValueTask ClearAsync(INotifyPropertyChanged instance, PropertyInfo property)
    {
        SecureStorage.Default.Remove(Key);
        property.SetValue(instance, null);
        return new ValueTask();
    }
}