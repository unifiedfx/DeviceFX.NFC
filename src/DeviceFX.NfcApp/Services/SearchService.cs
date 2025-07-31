using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Services;

public class SearchService : ISearchService
{
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        List<SearchResult> results = new()
        {
            new SearchResult {Name = "Stephen Welsh", Number = "1000"},
            new SearchResult {Name = "John Doe", Number = "1001"},
            new SearchResult {Name = "Jane Smith", Number = "1002"},
            new SearchResult {Name = "Alice Johnson", Number = "1003"},
            new SearchResult {Name = "Bob Brown", Number = "1004"},
            new SearchResult {Name = "Charlie Black", Number = "1005"},
            new SearchResult {Name = "Diana Prince", Number = "1006"},
            new SearchResult {Name = "Ethan Hunt", Number = "1007"},
            new SearchResult {Name = "Fiona Gallagher", Number = "1008"},
            new SearchResult {Name = "George Costanza", Number = "1009"},
            new SearchResult {Name = "Hannah Montana", Number = "1010"},
            new SearchResult {Name = "Ian Malcolm", Number = "1011"},
            new SearchResult {Name = "Jack Sparrow", Number = "1012"},
            new SearchResult {Name = "Katherine Johnson", Number = "1013"},
            new SearchResult {Name = "Liam Neeson", Number = "1014"},
            new SearchResult {Name = "Mia Wallace", Number = "1015"},
            new SearchResult {Name = "Nina Simone", Number = "1016"},
            new SearchResult {Name = "Oscar Isaac", Number = "1017"},
            new SearchResult {Name = "Paul Atreides", Number = "1018"},
            new SearchResult {Name = "Quentin Tarantino", Number = "1019"},
            new SearchResult {Name = "Rachel Green", Number = "1020"},
            new SearchResult {Name = "Sam Winchester", Number = "1021"},
            new SearchResult {Name = "Tina Fey", Number = "1022"}
        };
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        return results.Where(r => r.Find(query)).ToList();
    }
}