using System.ComponentModel;
using System.Reflection;

namespace DeviceFX.NfcApp.Helpers.Preference;

public abstract class PreferenceAttribute(string key) : Attribute
{
    public string Key { get; } = key;
    public abstract ValueTask LoadAsync(INotifyPropertyChanged instance, PropertyInfo property);
    public abstract ValueTask SaveAsync(INotifyPropertyChanged instance, PropertyInfo property);
    public abstract ValueTask ClearAsync(INotifyPropertyChanged instance, PropertyInfo property);
}

/// <summary>
/// Attach to Property or ObservableProperty Field to save and load preferences.
/// </summary>
/// <param name="key"></param>
/// <param name="defaultValue"></param>
/// <typeparam name="T"></typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field )]
public class PreferenceAttribute<T>(string key, T? defaultValue = default) : PreferenceAttribute(key)
{
    public T? DefaultValue { get; } = defaultValue;

    public override ValueTask LoadAsync(INotifyPropertyChanged instance, PropertyInfo property)
    {
        var value = Preferences.Default.Get(Key, DefaultValue);
        property.SetValue(instance, value);
        return new ValueTask();
    }

    public override ValueTask SaveAsync(INotifyPropertyChanged instance, PropertyInfo property)
    {
        var value = property.GetValue(instance);
        switch (value)
        {
            case string s:
                Preferences.Default.Set(Key, s);
                break;
            case bool b:
                Preferences.Default.Set(Key, b);
                break;
            case int b:
                Preferences.Default.Set(Key, b);
                break;
            case long b:
                Preferences.Default.Set(Key, b);
                break;
            case DateTime t:
                Preferences.Default.Set(Key, t);
                break;
        }
        return new ValueTask();
    }
    
    public override ValueTask ClearAsync(INotifyPropertyChanged instance, PropertyInfo property)
    {
        Preferences.Default.Clear(Key);
        return new ValueTask();
    }
}