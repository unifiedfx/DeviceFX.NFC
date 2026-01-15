using Duende.IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DeviceFX.Proxy.CDA;

public interface ITokenService
{
    Task<string> GetAccessTokenAsync();
}

public class TokenService(IHttpClientFactory httpClientFactory, IOptions<CiscoOptions> options, IMemoryCache cache)
    : ITokenService
{
    private readonly object @lock = new object();

    public async Task<string> GetAccessTokenAsync()
    {
        if (cache.TryGetValue("CiscoAccessToken", out string cachedToken))
        {
            return cachedToken;
        }

        lock (@lock)
        {
            if (cache.TryGetValue("CiscoAccessToken", out cachedToken))
            {
                return cachedToken;
            }
            return FetchAndCacheTokenAsync().GetAwaiter().GetResult();
        }
    }

    private async Task<string> FetchAndCacheTokenAsync()
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = "https://id.cisco.com/oauth2/default/v1/token",
            ClientId = options.Value.ClientId,
            ClientSecret = options.Value.ClientSecret,
        });

        if (response.IsError)
        {
            throw new Exception($"Token request failed: {response.Error}");
        }

        // Cache the token with expiration (subtract 1 minute for safety)
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(response.ExpiresIn - 60));

        cache.Set("CiscoAccessToken", response.AccessToken, cacheEntryOptions);

        return response.AccessToken;
    }
}