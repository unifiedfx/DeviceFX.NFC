using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DeviceFX.NfcApp.Model;

public partial class SearchResult : ObservableObject, IComparable<SearchResult>
{
    [ObservableProperty]
    private string? name;
    [ObservableProperty]
    private string? number;

    [ObservableProperty]
    private string? type;

    [ObservableProperty]
    private string? id;

    public int CompareTo(SearchResult? other)
    {
        if (other == null) return 1;
        int nameCompare = string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        if (nameCompare != 0) return nameCompare;
        return string.Compare(Number, other.Number, StringComparison.OrdinalIgnoreCase);
    }
    public bool Find(string query)
    {
        if (string.IsNullOrEmpty(query)) return true;
        return (Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (Number?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }
    public override string ToString()
    {
        var sb = new StringBuilder();
        if(Name != null && Number != null) sb.Append($"{Name} ({Number})");
        else if (Number != null) sb.Append(Number);
        else if (Name != null) sb.Append(Name);
        return sb.ToString();
    }
}