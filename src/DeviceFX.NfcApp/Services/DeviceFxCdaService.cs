using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model;
using IHttpClientFactory = DeviceFX.NfcApp.Abstractions.IHttpClientFactory;

namespace DeviceFX.NfcApp.Services;

public class DeviceFxCdaService(
    Settings settings,
    DeviceFxService deviceFxService,
    IHttpClientFactory httpClientFactory) : ICdaService
{
    public async Task<bool> CanSignData(CancellationToken cancellationToken = default) =>
        await deviceFxService.GetAccessTokenAsync(retryLogin: true) != null;

    public string? GetError() => "Not signed in";

    public async Task<byte[]> SignData(string data, string macAddress, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetHttpClient();
        if (httpClient == null) return [];
        var payload = Encoding.UTF8.GetBytes(data);
        var response = await httpClient.PostAsJsonAsync(
            "api/mobile/cdasvcs/rc/v1/sign-data",
            new
            {
                payload,
                macAddress
            }, cancellationToken: cancellationToken);
        return await CdaSignHttp.ReadSignedDataAsync(response, cancellationToken);
    }

    private async Task<HttpClient?> GetHttpClient()
    {
        var accessToken = await deviceFxService.GetAccessTokenAsync(retryLogin: true);
        if (accessToken == null) return null;
        var httpClient = await httpClientFactory.Create();
        httpClient.BaseAddress = new Uri((settings.Webex.ApiHost ?? "https://app.devicefx.com").TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return httpClient;
    }
}
