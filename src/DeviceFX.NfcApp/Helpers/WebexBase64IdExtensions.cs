using System.Text;

namespace DeviceFX.NfcApp.Helpers;

public static class WebexBase64IdExtensions
{
    public static string ConvertToBase64Id(this WebexIDTypes type, string id)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (!webexIdPrefixes.TryGetValue(type.ToString(), out var prefix)) throw new ArgumentException($"Invalid WebexIDType: {type}");
        if (Guid.TryParse(id, out var guid))
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{prefix}{guid}")).TrimEnd('=');
        }
        return id;
    }
    public static string ConvertToGuid(this WebexIDTypes type, string id)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (!webexIdPrefixes.TryGetValue(type.ToString(), out var prefix)) throw new ArgumentException($"Invalid WebexIDType: {type}");
        if (Guid.TryParse(id, out var guid)) return id;
        var mod = id.Length % 4;
        var padding = mod == 0 ? 0 : 4 - mod;
        Span<byte> buffer = new byte[(id.Length + mod) * 3 / 4 + 1];
        if(!Convert.TryFromBase64String(id + new string('=', padding), buffer, out var count )) throw new ArgumentException("Invalid Base64 string", nameof(id));
        var decoded = Encoding.UTF8.GetString(buffer[..count]);
        return decoded.Remove(0, prefix.Length);
    }
    private static Dictionary<string, string> webexIdPrefixes = new()
    {
        {nameof(WebexIDTypes.Organization), "ciscospark://us/ORGANIZATION/"},
        {nameof(WebexIDTypes.People), "ciscospark://us/PEOPLE/"}
    };
}

public enum WebexIDTypes
{
    Organization,
    People,
}
