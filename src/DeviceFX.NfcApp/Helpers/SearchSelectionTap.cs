using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Helpers;

/// <summary>
/// Resolves a CollectionView tap against the current search pick.
/// Android can fire a null selection when the list detaches, and going Back from
/// Provision can leave rows <see cref="SearchResult.Checked"/> after the VM pick is cleared.
/// </summary>
public static class SearchSelectionTap
{
    public readonly record struct Result(bool Ignore, bool NeedsValidation, SearchResult? Selection);

    public static Result Resolve(SearchResult? tapped, SearchResult? current)
    {
        if (tapped == null)
            return new Result(Ignore: true, NeedsValidation: false, Selection: current);

        var selected = current != null && tapped.Id == current.Id ? current : tapped;
        if (selected.Checked)
            return new Result(
                Ignore: false,
                NeedsValidation: false,
                Selection: string.IsNullOrEmpty(selected.Issue) ? selected : null);

        return new Result(Ignore: false, NeedsValidation: true, Selection: selected);
    }
}
