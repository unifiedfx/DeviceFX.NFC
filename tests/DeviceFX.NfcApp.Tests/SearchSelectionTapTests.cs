using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model;
using Xunit;

namespace DeviceFX.NfcApp.Tests;

public class SearchSelectionTapTests
{
    [Fact]
    public void Resolve_NullTap_KeepsCurrentAndIgnores()
    {
        var current = Person("a");
        current.Checked = true;

        var result = SearchSelectionTap.Resolve(null, current);

        Assert.True(result.Ignore);
        Assert.False(result.NeedsValidation);
        Assert.Same(current, result.Selection);
    }

    [Fact]
    public void Resolve_CheckedValidWhenCurrentCleared_RestoresSelection()
    {
        var tapped = Person("a");
        tapped.Checked = true;

        var result = SearchSelectionTap.Resolve(tapped, current: null);

        Assert.False(result.Ignore);
        Assert.False(result.NeedsValidation);
        Assert.Same(tapped, result.Selection);
    }

    [Fact]
    public void Resolve_CheckedOtherEntry_SwitchesSelectionWithoutRevalidating()
    {
        var current = Person("a");
        current.Checked = true;
        var tapped = Person("b");
        tapped.Checked = true;

        var result = SearchSelectionTap.Resolve(tapped, current);

        Assert.False(result.Ignore);
        Assert.False(result.NeedsValidation);
        Assert.Same(tapped, result.Selection);
    }

    [Fact]
    public void Resolve_CheckedWithIssue_ClearsSelection()
    {
        var tapped = Person("a");
        tapped.Checked = true;
        tapped.Issue = SearchResultValidator.NoLicense;

        var result = SearchSelectionTap.Resolve(tapped, current: tapped);

        Assert.False(result.Ignore);
        Assert.False(result.NeedsValidation);
        Assert.Null(result.Selection);
    }

    [Fact]
    public void Resolve_UncheckedSameId_UsesCurrentInstanceAndValidates()
    {
        var current = Person("a");
        var tapped = Person("a");

        var result = SearchSelectionTap.Resolve(tapped, current);

        Assert.False(result.Ignore);
        Assert.True(result.NeedsValidation);
        Assert.Same(current, result.Selection);
    }

    [Fact]
    public void Resolve_UncheckedOtherEntry_ValidatesTapped()
    {
        var current = Person("a");
        current.Checked = true;
        var tapped = Person("b");

        var result = SearchSelectionTap.Resolve(tapped, current);

        Assert.False(result.Ignore);
        Assert.True(result.NeedsValidation);
        Assert.Same(tapped, result.Selection);
    }

    private static SearchResult Person(string id) => new() { Id = id, Name = id, Type = "User" };
}
