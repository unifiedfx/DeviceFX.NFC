using System.Security.Cryptography;
using System.Text;

namespace DeviceFX.NfcApp.Helpers;

public static class OidcPkce
{
    public static (string Verifier, string Challenge, string State) Create()
    {
        var verifier = Base64Url(RandomBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomBytes(16));
        return (verifier, challenge, state);
    }

    public static string Base64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }
}
