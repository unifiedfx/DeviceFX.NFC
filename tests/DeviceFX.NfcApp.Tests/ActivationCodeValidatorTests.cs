using DeviceFX.NfcApp.Helpers;
using Xunit;

namespace DeviceFX.NfcApp.Tests;

public class ActivationCodeValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc-")]
    public void Normalize_NothingUsable_ReturnsNull(string? input)
    {
        Assert.Null(ActivationCodeValidator.Normalize(input));
    }

    [Theory]
    [InlineData("1234567890123456", "1234567890123456")]
    [InlineData("1234-5678-9012-3456", "1234567890123456")]
    [InlineData("1234 5678 9012 3456", "1234567890123456")]
    [InlineData("12345678901234567890", "1234567890123456")]
    public void Normalize_StandardCode_KeepsUpTo16Digits(string input, string expected)
    {
        Assert.Equal(expected, ActivationCodeValidator.Normalize(input));
    }

    [Theory]
    [InlineData("#", "#")]
    [InlineData("#123", "#123")]
    [InlineData(" #123 ", "#123")]
    [InlineData("#123-456", "#123456")]
    [InlineData("#1234567890123456", "#1234567890123456")]
    [InlineData("#12345678901234567", "#1234567890123456")]
    public void Normalize_ShortCode_KeepsLeadingHash(string input, string expected)
    {
        Assert.Equal(expected, ActivationCodeValidator.Normalize(input));
    }

    [Theory]
    [InlineData("12#34", "1234")]
    [InlineData("123#", "123")]
    [InlineData("##123", "#123")]
    [InlineData("#12#3", "#123")]
    [InlineData("*123", "123")]
    public void Normalize_HashNotAtStart_IsDropped(string input, string expected)
    {
        Assert.Equal(expected, ActivationCodeValidator.Normalize(input));
    }

    [Fact]
    public void Normalize_NonAsciiDigits_AreMappedToAscii()
    {
        // Arabic-Indic digits ٠١٢٣ → 0123
        Assert.Equal("#0123", ActivationCodeValidator.Normalize("#٠١٢٣"));
    }

    [Theory]
    [InlineData("1234-5678-9012-3456")]
    [InlineData("#12345678901234567")]
    [InlineData("#")]
    [InlineData("12#34")]
    public void Normalize_IsIdempotent(string input)
    {
        var once = ActivationCodeValidator.Normalize(input);

        Assert.Equal(once, ActivationCodeValidator.Normalize(once));
    }

    [Theory]
    [InlineData("1234567890123456")]
    [InlineData("#123")]
    [InlineData("#123456")]
    [InlineData("#1234567890123456")]
    public void IsValid_AcceptsDocumentedFormats(string code)
    {
        Assert.True(ActivationCodeValidator.IsValid(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("#12")]
    [InlineData("123456789012345")]
    [InlineData("12345678901234567")]
    [InlineData("#12345678901234567")]
    [InlineData("12#34")]
    [InlineData("123#")]
    [InlineData("#12a")]
    [InlineData("1234-5678-9012-3456")]
    [InlineData("١٢٣٤٥٦٧٨٩٠١٢٣٤٥٦")]
    public void IsValid_RejectsEverythingElse(string? code)
    {
        Assert.False(ActivationCodeValidator.IsValid(code));
    }

    [Theory]
    [InlineData("1234567890123456", true)]
    [InlineData("#123456789012345", false)]
    [InlineData("#123", false)]
    [InlineData(null, false)]
    public void IsStandard_OnlyMatches16Digits(string? code, bool expected)
    {
        Assert.Equal(expected, ActivationCodeValidator.IsStandard(code));
    }

    [Theory]
    [InlineData(null, "1")]
    [InlineData("123456789012345", "1234567890123456")]       // 16th digit
    [InlineData(null, "1234-5678-9012-3456")]                   // pasted formatted code
    [InlineData("1234567890123456", "#1234567890123456")]       // # added to a full code
    [InlineData("#123456789012345", "#1234567890123456")]       // 16 digits after #
    [InlineData("1234567890123456", "123456789012345")]         // deleting a digit
    [InlineData("123456789012345678", "12345678901234567")]     // shortening an over-long code
    public void IsEditAllowed_AllowsUpTo16Digits(string? oldText, string newText)
    {
        Assert.True(ActivationCodeValidator.IsEditAllowed(oldText, newText));
    }

    [Theory]
    [InlineData("1234567890123456", "12345678901234567")]       // 17th digit at the end
    [InlineData("1234567890123456", "91234567890123456")]       // digit typed at the start
    [InlineData("#1234567890123456", "#12345678901234567")]     // 17th digit after #
    [InlineData(null, "12345678901234567")]                     // 17 digits pasted
    [InlineData("12345678901234567", "123456789012345678")]     // over-long code getting longer
    public void IsEditAllowed_RejectsMoreThan16Digits(string? oldText, string newText)
    {
        Assert.False(ActivationCodeValidator.IsEditAllowed(oldText, newText));
    }
}
