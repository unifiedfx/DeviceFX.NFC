using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Services;

public class CdaService(Settings settings, IAttestationService attestationService)
{
    public async Task<bool> CanSignData(CancellationToken cancellationToken = default) =>
        await attestationService.CanGetTokenAsync(cancellationToken);
    public string? GetError() => attestationService.GetError();

    public async Task<byte[]> SignData(string data, string macAddress, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetHttpClient(cancellationToken);
        if(httpClient == null) return [];
        var payload = Encoding.UTF8.GetBytes(data);
        var response = await httpClient.PostAsJsonAsync(
            "api/mobile/cdasvcs/rc/v1/sign-data",
            new
            {
                payload,
                macAddress
            }, cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode) return [];
        var result = await response.Content.ReadFromJsonAsync<CdaSignResponse>(cancellationToken: cancellationToken);
        if(result?.IsSuccess == true) return result.SignedDataBytes;
        return [];
    }

    private async Task<HttpClient?> GetHttpClient(CancellationToken cancellationToken = default)
    {
        var token = await attestationService.GetAttestationTokenAsync(cancellationToken);
        if(token == null) return null;
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(settings.Webex.CdaServiceUrl);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return httpClient;
    }

    public record CdaSignResponse(string Status, string StatusMessage, string SignedData)
    {
        public bool IsSuccess => Status == "SUCCESS";
        public string ErrorMessage => $"{Status}: {StatusMessage}";
        public byte[] SignedDataBytes => Convert.FromBase64String(SignedData);
    }
}