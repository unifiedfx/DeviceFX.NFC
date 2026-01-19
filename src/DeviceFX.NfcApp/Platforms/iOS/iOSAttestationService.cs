using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DeviceCheck;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using Foundation;
using UIKit;

namespace DeviceFX.NfcApp;

public class iOSAttestationService(Settings settings) : IAttestationService
{
    private static TokenResponse? tokenResponse;
    public async Task<bool> CanGetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!DCAppAttestService.SharedService.Supported)
        {
            tokenResponse = new TokenResponse(false, "CDA service not supported on this device");
            return false;
        }
        if(tokenResponse?.IsValid == true) return true;
        await GetTokenAsync(cancellationToken);
        return tokenResponse?.Success == true;
    }
    public string? GetError() => tokenResponse?.Error;

    public async Task<string?> GetAttestationTokenAsync(CancellationToken cancellationToken = default)
    {
        if(tokenResponse?.IsValid == false) await GetTokenAsync(cancellationToken);
        return tokenResponse?.Token;
    }
    public async Task GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(settings.Webex.CdaServiceUrl);
        var challengeResult = await httpClient.GetFromJsonAsync<JsonElement>("api/mobile/attestChallenge", cancellationToken: cancellationToken);
        string? challenge = challengeResult.TryGetProperty("challenge", out var challengeJson) && challengeJson.ValueKind == JsonValueKind.String ? challengeJson.GetString() : null;
        if (challenge == null)
        {
            tokenResponse = new TokenResponse(false, "Failed to obtain challenge");
            return;
        }
        var service = DCAppAttestService.SharedService;
        var keyId = await service.GenerateKeyAsync();
        var clientDataHash = SHA256.HashData(Encoding.UTF8.GetBytes(challenge));
        var attestation = await service.AttestKeyAsync(keyId, NSData.FromArray(clientDataHash));
        var attestationToken = Convert.ToBase64String(attestation.ToArray());
        var response = await httpClient.PostAsJsonAsync(
            "api/mobile/attest",
            new
            {
                platform = "Apple",
                attestationToken,
                keyId,
                deviceId = UIDevice.CurrentDevice.IdentifierForVendor?.AsString()
            }, cancellationToken: cancellationToken);
        tokenResponse = response.IsSuccessStatusCode ? 
            await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken) :  
            new TokenResponse(false, "Failed to obtain token");
    }

    private record TokenResponse(bool Success, string? Error = null, string? Token = null, int? ExpiresIn = null)
    {
        private readonly DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        public bool IsExpired => timestamp.AddSeconds(ExpiresIn ?? 0) < DateTimeOffset.UtcNow.AddSeconds(60);
        public bool IsValid => Success && !string.IsNullOrEmpty(Token) && !IsExpired;
    }
}