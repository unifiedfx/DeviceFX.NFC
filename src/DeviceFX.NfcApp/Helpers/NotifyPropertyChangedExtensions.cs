using System.ComponentModel;
using System.Reflection;
using DeviceFX.NfcApp.Helpers.Preference;

namespace DeviceFX.NfcApp.Helpers;

public static class NotifyPropertyChangedExtensions
{
    public static async Task LoadAsync(this INotifyPropertyChanged self, string? propertyName = null)
    {
        foreach (var property in GetProperties(self))
        {
            if(propertyName != null && property.info.Name != propertyName) continue; 
            await property.attribute.LoadAsync(self, property.info);
        }
    }
    
    public static async Task SaveAsync(this INotifyPropertyChanged self, string? propertyName = null)
    {
        foreach (var property in GetProperties(self))
        {
            if(propertyName != null && property.info.Name != propertyName) continue; 
            await property.attribute.SaveAsync(self, property.info);
        }
    }
    public static async Task ClearAsync(this INotifyPropertyChanged self, string propertyName)
    {
        var property = GetProperties(self).FirstOrDefault(p => p.info.Name == propertyName);
        if(property.attribute == null) return;
        await property.attribute.ClearAsync(self, property.info);
    }
    
    public static void ApplyQuery(this INotifyPropertyChanged self, IDictionary<string, object> query)
    {
        foreach (var property in GetProperties(self))
        {
            if(!query.ContainsKey(property.attribute.Key)) continue;
            property.info.SetValue(self,query[property.attribute.Key]);
        }
    }
    
    private static IEnumerable<(PropertyInfo info, PreferenceAttribute attribute)> GetProperties(this object self)
    {
        // Get all properties with PreferenceAttribute
        var properties = self.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.GetCustomAttributes(typeof(PreferenceAttribute), true).Any())
            .Select(prop => (info: prop, attribute: prop.GetCustomAttribute<PreferenceAttribute>()!)).ToList();
        // Get all fields with PreferenceAttribute
        var fields = self.GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(prop => prop.GetCustomAttributes(typeof(PreferenceAttribute), true).Any());
        foreach (var field in fields.Where(f => char.IsLower(f.Name[0])))
        {
            // Find property name matching field name (e.g. "myField" to "MyField") and return the property with the attribute from the matching field
            // This is primarily for fields tagged as ObservableProperty, essential to set values on properties so the PropertyChanged event is raised
            var property = self.GetType().GetProperty($"{char.ToUpper(field.Name[0])}{field.Name[1..]}");
            var attribute = field.GetCustomAttribute<PreferenceAttribute>();
            if(property == null || attribute == null) continue;
            if (properties.All(p => p.info != property)) properties.Add((property, attribute));
        }
        return properties;
    }
}