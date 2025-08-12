using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface ISearchService
{
    Task<List<SearchResult>> SearchAsync(string query, string orgId, CancellationToken cancellationToken = default);
}