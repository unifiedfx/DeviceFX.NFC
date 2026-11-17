namespace DeviceFX.NfcApp.Abstractions;

public interface ICdaService
{
    Task<bool> CanSignData(CancellationToken cancellationToken = default);
    string? GetError();
    Task<byte[]> SignData(string data, string macAddress, CancellationToken cancellationToken = default);
}
