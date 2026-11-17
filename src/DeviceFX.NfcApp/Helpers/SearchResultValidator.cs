using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Helpers;

/// <summary>
/// Applies calling-license checks to a search hit. Person/workspace JSON often omits
/// <c>licenses</c>; treating that as empty avoids a null dereference after selection.
/// </summary>
public static class SearchResultValidator
{
    public const string UnableToValidate = "Unable to validate";
    public const string NoLicense = "No license";
    public const string NoCallingLicense = "No calling license";

    public static void ApplyPersonLicenses(
        SearchResult result,
        string[]? licenses,
        IReadOnlyCollection<string>? callingLicenseIds)
    {
        if (licenses is not { Length: > 0 })
        {
            result.Checked = true;
            result.Issue = NoLicense;
            return;
        }

        // Org calling-license catalog is loaded separately; if it is missing, a person
        // who already has licenses is still usable (do not surface "Unable to validate").
        if (callingLicenseIds is null)
            return;

        if (!licenses.Any(callingLicenseIds.Contains))
        {
            result.Checked = true;
            result.Issue = NoCallingLicense;
        }
    }

    public static void ApplyWorkspaceCalling(
        SearchResult result,
        string? callingType,
        string[]? workspaceLicenses,
        IReadOnlyCollection<string>? callingLicenseIds)
    {
        result.Checked = true;
        if (callingType?.Contains("free", StringComparison.OrdinalIgnoreCase) == true)
            return;

        if (workspaceLicenses is not { Length: > 0 })
        {
            result.Issue = NoLicense;
            return;
        }

        if (callingLicenseIds is null)
            return;

        if (!workspaceLicenses.Any(callingLicenseIds.Contains))
            result.Issue = NoCallingLicense;
    }
}
