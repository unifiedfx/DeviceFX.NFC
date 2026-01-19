using Google.Apis.Auth.OAuth2;
using Google.Apis.PlayIntegrity.v1;
using Google.Apis.PlayIntegrity.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace DeviceFX.Proxy.CDA;

public class GoogleAttestService(IOptions<CiscoOptions> options, ILogger<AppleAttestService> logger)
{
    private readonly CiscoOptions options = options.Value;

    public async Task<AttestResult> ValidateAttestationAsync(string attestationToken, string challenge)
    {
        var credential = GoogleCredential.FromJson(options.GoogleServiceJson)
            .CreateScoped(PlayIntegrityService.ScopeConstants.Playintegrity);
        var service = new PlayIntegrityService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = options.GoogleAppName
        });
        var decodeRequest = new DecodeIntegrityTokenRequest
        {
            IntegrityToken = attestationToken
        };
        var decodeResponse =
            await service.V1.DecodeIntegrityToken(decodeRequest, options.GooglePackageName).ExecuteAsync();
        var payload = decodeResponse.TokenPayloadExternal;
        // Step 1: Validate requestDetails (for Classic request)
        if (payload.RequestDetails == null)
            return new AttestResult {IsValid = false, Error = "Invalid request details."};

        // Verify package name matches
        if (payload.RequestDetails.RequestPackageName != options.GooglePackageName)
            return new AttestResult {IsValid = false, Error = "Package name mismatch."};

        // Verify nonce if provided (nonce should be generated server-side, sent to client, and validated here)
        if (!string.IsNullOrEmpty(challenge) && payload.RequestDetails.Nonce != challenge)
            return new AttestResult {IsValid = false, Error = "Nonce mismatch."};

        // Check timestamp freshness (e.g., within 5 minutes)
        var timestampMillis = payload.RequestDetails.TimestampMillis ?? 0;
        var requestTime = DateTimeOffset.FromUnixTimeMilliseconds(timestampMillis);
        if ((DateTimeOffset.UtcNow - requestTime).TotalMinutes > 5)
            return new AttestResult {IsValid = false, Error = "Token is too old."};

        // Step 2: Validate appIntegrity
        if (payload.AppIntegrity == null || payload.AppIntegrity.AppRecognitionVerdict != "PLAY_RECOGNIZED")
            return new AttestResult {IsValid = false, Error = "App not recognized by Play."};

        // Optional: Verify package name, certificate digest, version code
        if (payload.AppIntegrity.PackageName != options.GooglePackageName)
            return new AttestResult {IsValid = false, Error = "AppIntegrity package name mismatch."};

        // Step 3: Validate deviceIntegrity
        if (payload.DeviceIntegrity == null ||
            !payload.DeviceIntegrity.DeviceRecognitionVerdict.Contains("MEETS_DEVICE_INTEGRITY"))
            return new AttestResult {IsValid = false, Error = "Device does not meet integrity requirements."};

        // Step 4: Validate accountDetails (optional)
        if (payload.AccountDetails != null && payload.AccountDetails.AppLicensingVerdict != "LICENSED")
            return new AttestResult {IsValid = false, Error = "App not licensed via Play Store."};

        return new AttestResult {IsValid = true};
    }
}