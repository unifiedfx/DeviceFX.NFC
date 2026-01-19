namespace DeviceFX.NfcApp.Abstractions;

public interface IAttestationService
{
    Task<bool> CanGetTokenAsync(CancellationToken cancellationToken = default);
    string? GetError();
    Task<string?> GetAttestationTokenAsync(CancellationToken cancellationToken = default);
}