using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface ILocationService
{
    public Task<GeocodeResponse?> GetLocationAsync(CancellationToken cancellationToken = default);
}

