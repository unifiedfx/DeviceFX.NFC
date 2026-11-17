using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Services;

/// <summary>
/// DeviceFX + logged in uses ApiHost CDA; otherwise the existing attestation CDA path.
/// </summary>
public class IntegrationCdaService(
    Settings settings,
    CdaService cdaService,
    DeviceFxCdaService deviceFxCdaService) : ICdaService
{
    private ICdaService Current =>
        settings.IsDeviceFx && settings.User.IsLoggedIn ? deviceFxCdaService : cdaService;

    public Task<bool> CanSignData(CancellationToken cancellationToken = default) =>
        Current.CanSignData(cancellationToken);

    public string? GetError() => Current.GetError();

    public Task<byte[]> SignData(string data, string macAddress, CancellationToken cancellationToken = default) =>
        Current.SignData(data, macAddress, cancellationToken);
}
