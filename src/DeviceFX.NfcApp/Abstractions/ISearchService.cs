using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface ISearchService
{
    Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
}