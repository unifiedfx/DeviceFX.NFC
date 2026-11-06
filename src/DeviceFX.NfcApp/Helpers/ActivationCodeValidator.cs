using System.Globalization;
using System.Text;

namespace DeviceFX.NfcApp.Helpers;

/// <summary>
/// Cleans and validates Activation Code input. Two formats are valid: the standard
/// 16-digit code, and the short code Cisco documents for CDA redirection profiles,
/// which is <c>#</c> followed by 3–16 digits. The leading <c>#</c> is how the phone
/// tells a short code apart from a standard one.
/// </summary>
public static class ActivationCodeValidator
{
    public const char ShortCodePrefix = '#';
    public const int StandardLength = 16;
    public const int ShortCodeMinDigits = 3;
    public const int MaxDigits = 16;

    /// <summary>
    /// Keeps the digits and a leading <c>#</c>, drops separators such as spaces and
    /// dashes from pasted codes, and caps the digits at 16. A <c>#</c> is kept on its
    /// own so the user can type the prefix first. Returns null when nothing is left.
    /// </summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var code = new StringBuilder(MaxDigits + 1);
        var digits = 0;
        foreach (var c in input)
        {
            if (c == ShortCodePrefix)
            {
                // Only a '#' that comes before every digit is the short-code prefix.
                if (code.Length == 0) code.Append(c);
                continue;
            }
            // Map any Unicode decimal digit to ASCII so the phone always receives 0-9.
            var value = CharUnicodeInfo.GetDecimalDigitValue(c);
            if (value < 0) continue;
            code.Append((char)('0' + value));
            if (++digits == MaxDigits) break;
        }
        return code.Length == 0 ? null : code.ToString();
    }

    /// <summary>True for a standard 16-digit code or a short code (<c>#</c> plus 3–16 digits).</summary>
    public static bool IsValid(string? code) => IsStandard(code) || IsShortCode(code);

    /// <summary>True for a standard 16-digit code.</summary>
    public static bool IsStandard(string? code) =>
        code is { Length: StandardLength } && IsAsciiDigits(code);

    /// <summary>True for a short code: <c>#</c> followed by 3–16 digits.</summary>
    public static bool IsShortCode(string? code) =>
        code is { Length: >= ShortCodeMinDigits + 1 and <= MaxDigits + 1 }
        && code[0] == ShortCodePrefix
        && IsAsciiDigits(code.AsSpan(1));

    private static bool IsAsciiDigits(ReadOnlySpan<char> value) => !value.ContainsAnyExceptInRange('0', '9');
}
