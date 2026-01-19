using System.Net.Http.Json;
using System.Text.Json;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using Xamarin.Google.Android.Play.Core.Integrity;

namespace DeviceFX.NfcApp;

public class AndroidAttestationService(Settings settings) : IAttestationService
{
    private static TokenResponse? tokenResponse;

    public async Task<bool> CanGetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (tokenResponse?.IsValid == true) return true;
        await GetTokenAsync(cancellationToken);
        return tokenResponse?.Success == true;
    }

    public string? GetError() => tokenResponse?.Error;

    public async Task<string?> GetAttestationTokenAsync(CancellationToken cancellationToken = default)
    {
        if (tokenResponse?.IsValid == false) await GetTokenAsync(cancellationToken);
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

        try
        {
            var context = Android.App.Application.Context;
            var integrityManager = IntegrityManagerFactory.Create(context);

            var requestBuilder = IntegrityTokenRequest.InvokeBuilder()
                .SetNonce(challenge)
                .Build();

            // var integrityTokenResponse = integrityManager.RequestIntegrityToken(requestBuilder).GetAwaiter().GetResult() as IntegrityTokenResponse;
            var integrityTokenResponse = await integrityManager.RequestIntegrityToken(requestBuilder).ToAwaitableTask() as IntegrityTokenResponse;
            var integrityToken = integrityTokenResponse?.Token();

            var response = await httpClient.PostAsJsonAsync(
                "api/mobile/attest",
                new
                {
                    platform = "Google",
                    attestationToken = integrityToken,
                    deviceId = Android.Provider.Settings.Secure.GetString(context.ContentResolver, Android.Provider.Settings.Secure.AndroidId)
                }, cancellationToken: cancellationToken);

            tokenResponse = response.IsSuccessStatusCode ?
                await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken) :
                new TokenResponse(false, "Failed to obtain token");
        }
        catch (Exception ex)
        {
            tokenResponse = new TokenResponse(false, $"Integrity check failed: {ex.Message}");
        }
    }

    private record TokenResponse(bool Success, string? Error = null, string? Token = null, int? ExpiresIn = null)
    {
        private readonly DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        public bool IsExpired => timestamp.AddSeconds(ExpiresIn ?? 0) < DateTimeOffset.UtcNow.AddSeconds(60);
        public bool IsValid => Success && !string.IsNullOrEmpty(Token) && !IsExpired;
    }
}