using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model;
using Xunit;

namespace DeviceFX.NfcApp.Tests;

public class SearchResultValidatorTests
{
    [Fact]
    public void ApplyPersonLicenses_NullLicenses_SetsNoLicense()
    {
        var result = new SearchResult { Id = "user-1", Type = "User" };

        SearchResultValidator.ApplyPersonLicenses(result, licenses: null, callingLicenseIds: ["lic-1"]);

        Assert.True(result.Checked);
        Assert.Equal(SearchResultValidator.NoLicense, result.Issue);
    }

    [Fact]
    public void ApplyPersonLicenses_EmptyLicenses_SetsNoLicense()
    {
        var result = new SearchResult { Id = "user-1", Type = "User" };

        SearchResultValidator.ApplyPersonLicenses(result, licenses: [], callingLicenseIds: ["lic-1"]);

        Assert.True(result.Checked);
        Assert.Equal(SearchResultValidator.NoLicense, result.Issue);
    }

    [Fact]
    public void ApplyPersonLicenses_NullCallingLicenseIds_AllowsLicensedPerson()
    {
        var result = new SearchResult { Id = "user-1", Type = "User" };

        SearchResultValidator.ApplyPersonLicenses(result, licenses: ["lic-1"], callingLicenseIds: null);

        Assert.False(result.Checked);
        Assert.Null(result.Issue);
    }

    [Fact]
    public void ApplyPersonLicenses_NoOverlap_SetsNoCallingLicense()
    {
        var result = new SearchResult { Id = "user-1", Type = "User" };

        SearchResultValidator.ApplyPersonLicenses(result, licenses: ["other"], callingLicenseIds: ["lic-1"]);

        Assert.True(result.Checked);
        Assert.Equal(SearchResultValidator.NoCallingLicense, result.Issue);
    }

    [Fact]
    public void ApplyPersonLicenses_MatchingLicense_LeavesIssueUnset()
    {
        var result = new SearchResult { Id = "user-1", Type = "User" };

        SearchResultValidator.ApplyPersonLicenses(result, licenses: ["lic-1", "other"], callingLicenseIds: ["lic-1"]);

        Assert.False(result.Checked);
        Assert.Null(result.Issue);
    }

    [Fact]
    public void ApplyWorkspaceCalling_NullCalling_SetsNoLicense()
    {
        var result = new SearchResult { Id = "ws-1", Type = "Workspace" };

        SearchResultValidator.ApplyWorkspaceCalling(result, callingType: null, workspaceLicenses: null, callingLicenseIds: ["lic-1"]);

        Assert.True(result.Checked);
        Assert.Equal(SearchResultValidator.NoLicense, result.Issue);
    }

    [Fact]
    public void ApplyWorkspaceCalling_FreeCalling_AllowsWithoutLicense()
    {
        var result = new SearchResult { Id = "ws-1", Type = "Workspace" };

        SearchResultValidator.ApplyWorkspaceCalling(result, callingType: "freeCalling", workspaceLicenses: null, callingLicenseIds: ["lic-1"]);

        Assert.True(result.Checked);
        Assert.Null(result.Issue);
    }

    [Fact]
    public void ApplyWorkspaceCalling_NullCallingLicenseIds_AllowsLicensedWorkspace()
    {
        var result = new SearchResult { Id = "ws-1", Type = "Workspace" };

        SearchResultValidator.ApplyWorkspaceCalling(
            result,
            callingType: "webexCalling",
            workspaceLicenses: ["lic-1"],
            callingLicenseIds: null);

        Assert.True(result.Checked);
        Assert.Null(result.Issue);
    }

    [Fact]
    public void ApplyWorkspaceCalling_MatchingLicense_LeavesIssueUnset()
    {
        var result = new SearchResult { Id = "ws-1", Type = "Workspace" };

        SearchResultValidator.ApplyWorkspaceCalling(
            result,
            callingType: "webexCalling",
            workspaceLicenses: ["lic-1"],
            callingLicenseIds: ["lic-1"]);

        Assert.True(result.Checked);
        Assert.Null(result.Issue);
    }
}
