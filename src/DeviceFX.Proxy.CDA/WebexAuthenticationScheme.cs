using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace DeviceFX.Proxy.CDA;

public static class WebexBearerExtensions
{
    public static AuthenticationBuilder AddWebex(this AuthenticationBuilder builder) => 
        builder.AddScheme<WebexAuthenticationOptions, WebexAuthenticationScheme>(WebexAuthenticationScheme.AuthenticationScheme, _ => { });
}

public class WebexAuthenticationOptions : AuthenticationSchemeOptions { }

public class WebexAuthenticationScheme : AuthenticationHandler<WebexAuthenticationOptions>
{
    public const string AuthenticationScheme = "Webex";
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<WebexAuthenticationScheme> logger;

    public WebexAuthenticationScheme(
        IOptionsMonitor<WebexAuthenticationOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory)
        : base(options, loggerFactory, encoder)
    {
        this.httpClientFactory = httpClientFactory;
        logger = loggerFactory.CreateLogger<WebexAuthenticationScheme>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var cookieResult = await Context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (cookieResult.Succeeded) return cookieResult;

        logger.LogInformation("Handling Webex authentication request");
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader) ||
            !AuthenticationHeaderValue.TryParse(authHeader, out var authValue) ||
            !string.Equals(authValue.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(authValue.Parameter))
        {
            return AuthenticateResult.Fail("Missing or invalid Authorization header.");
        }

        var token = authValue.Parameter;

        // Call Webex API to validate token and get user details
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        WebexPersonDto? user = null;
        try
        {
            user = await client.GetFromJsonAsync<WebexPersonDto>("https://webexapis.com/v1/people/me");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error validating token");
        }

        if (user == null)
        {
            return AuthenticateResult.Fail("Invalid token or API error.");
        }

        // Map to claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.id),
            new(ClaimTypes.Name, user.displayName),
            new(ClaimTypes.Email, user.emails.FirstOrDefault() ?? string.Empty),
            new(ClaimTypes.GivenName, user.firstName),
            new(ClaimTypes.Surname, user.lastName)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        await Context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}