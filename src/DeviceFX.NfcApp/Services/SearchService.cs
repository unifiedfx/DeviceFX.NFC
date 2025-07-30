using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Services;

public class SearchService : ISearchService
{
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        List<SearchResult> results = new()
        {
            new SearchResult {Name = "Stephen Welsh"},
            new SearchResult {Name = "John Doe"},
            new SearchResult {Name = "Jane Smith"},
            new SearchResult {Name = "Alice Johnson"},
            new SearchResult {Name = "Bob Brown"},
            new SearchResult {Name = "Charlie Davis"},
            new SearchResult {Name = "Lucy Van Pelt"},
            new SearchResult {Name = "Linus Van Pelt"},
            new SearchResult {Name = "Sally Brown"},
            new SearchResult {Name = "Peppermint Patty"},
            new SearchResult {Name = "Marcie Johnson"},
            new SearchResult {Name = "Franklin Armstrong"},
        };
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        return results
            .Where(d => d.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}