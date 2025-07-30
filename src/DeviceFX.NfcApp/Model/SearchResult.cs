using CommunityToolkit.Mvvm.ComponentModel;

namespace DeviceFX.NfcApp.Model;

public partial class SearchResult : ObservableObject, IComparable<SearchResult>
{
    [ObservableProperty] 
    private string? name;

    public int CompareTo(SearchResult? other) => other == null ? 1 : string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Name ?? string.Empty;
}