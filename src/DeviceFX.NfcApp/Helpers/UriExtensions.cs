namespace DeviceFX.NfcApp.Helpers;

public static class UriExtensions
{
    public static Uri ToRoute(this Uri uri) => uri.Scheme is "http" or "https"
        ? new Uri($"/{uri.PathAndQuery}", UriKind.Relative)
        : new Uri($"//{uri.Host}{uri.PathAndQuery.TrimStart('/')}", UriKind.Relative);
}