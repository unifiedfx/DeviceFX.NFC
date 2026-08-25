namespace DeviceFX.NfcApp.Helpers;

public static class UriExtensions
{
    public static string ToShellRoute(this Uri uri)
    {
        var path = uri.Scheme is "http" or "https"
            ? uri.AbsolutePath
            : string.IsNullOrEmpty(uri.Host)
                ? uri.AbsolutePath
                : uri.Host;
        path = path.Trim('/');
        if (string.IsNullOrEmpty(path)) return "//start";
        var slash = path.IndexOf('/');
        if (slash >= 0) path = path[..slash];
        return $"//{path.ToLowerInvariant()}";
    }
}
