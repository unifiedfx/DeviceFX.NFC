using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DeviceFX.Proxy.CDA;

public interface IJwtTokenService
{
    string GenerateToken(string keyId, int expires, string? deviceId = null);
}

public class JwtTokenService(IOptions<CiscoOptions> options) : IJwtTokenService
{
    private readonly CiscoOptions options = options.Value;

    public string GenerateToken(string keyId, int expires, string? deviceId = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("attested", "true")
        };
        if (!string.IsNullOrEmpty(keyId)) claims.Add(new Claim("key_id", keyId));
        if (!string.IsNullOrEmpty(deviceId)) claims.Add(new Claim("name", deviceId));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(expires),
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = credentials
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

