using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DeviceFX.Proxy.CDA;

[Route("api/[controller]/[action]")]
[ApiController]
public class MobileController(
    AppleAttestService appleAttestService,
    GoogleAttestService googleAttestService,
    IJwtTokenService jwtTokenService,
    IOptions<CiscoOptions> options,
    ILogger<MobileController> logger) : ControllerBase
{
    private const string AuthCookieName = "AuthChallenge";
    private readonly CiscoOptions options = options.Value;

    public async Task<IActionResult> AttestChallenge()
    {
        var challenge = Guid.NewGuid().ToString();
        Response.Cookies.Append(AuthCookieName, challenge, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddMinutes(5)
        });
        return Ok(new {challenge});
    }

    /// <summary>
    /// Exchange an iOS Device Attestation for a JWT token.
    /// This endpoint validates the attestation from Apple's App Attest service
    /// and returns a signed JWT if valid.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Attest([FromBody] AttestationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AttestationResponse
            {
                Success = false,
                Error = "Invalid request"
            });
        }

        logger.LogInformation("Processing attestation exchange for KeyId: {KeyId}", request.KeyId);
        Request.Cookies.TryGetValue(AuthCookieName, out var challenge);
        if (challenge == null) return BadRequest(new AttestationResponse { Success = false, Error = "Missing challenge" });

        var result = request.Platform switch
        {
            "Google" => await googleAttestService.ValidateAttestationAsync(request.AttestationToken, challenge),
            _ => await appleAttestService.ValidateAttestationAsync(request.AttestationToken, request.KeyId, challenge)
        };

        if (!result.IsValid)
        {
            logger.LogWarning("Attestation validation failed: {Error}", result.Error);
            return BadRequest(new AttestationResponse
            {
                Success = false,
                Error = result.Error ?? "Attestation validation failed"
            });
        }
        Response.Cookies.Delete(AuthCookieName);

        // Generate JWT token for the validated device
        var jwtToken = jwtTokenService.GenerateToken(request.KeyId, options.TokenExpirationSeconds, request.DeviceId);

        logger.LogInformation("Attestation validated successfully, JWT issued for KeyId: {KeyId}", request.KeyId);

        return Ok(new AttestationResponse
        {
            Success = true,
            Token = jwtToken,
            ExpiresIn = options.TokenExpirationSeconds
        });
    }
    

    [HttpGet]
    public IActionResult Test()
    {
        return Ok(new { message = "Hello World", timestamp = DateTime.UtcNow });
    }
}

