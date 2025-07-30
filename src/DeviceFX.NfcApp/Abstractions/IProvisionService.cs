using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface IProvisionService
{
    public Task ProvisionAsync(SearchResult result, CancellationToken cancellationToken = default);
}