using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Model.Dto;

namespace DeviceFX.NfcApp.Services;

public class WebexService(Settings settings) : IWebexService
{
    public async Task<WebexIdentityUserDto?> LoginAsync()
    {
        var user = await GetUser();
        if (user != null) return user;
        var authRequest = $"{settings.Webex.AuthUrl}?client_id={settings.Webex.ClientId}&response_type=token&redirect_uri={Uri.EscapeDataString(settings.Webex.RedirectUrl)}" + 
                          $"&scope={Uri.EscapeDataString(settings.Webex.Scopes)}";
        var authResult = await WebAuthenticator.AuthenticateAsync(
            new Uri(authRequest),
            new Uri(settings.Webex.RedirectUrl));
        if (authResult.Properties.TryGetValue("access_token", out var accessToken) &&
            !string.IsNullOrEmpty(accessToken))
        {
            settings.Webex.AccessToken = accessToken;
            await settings.Webex.SaveAsync(nameof(settings.Webex.AccessToken));
        }
        if (authResult.Properties.TryGetValue("expires_in", out var expiresInText) &&
            int.TryParse(expiresInText, out var expiresIn))
        {
            settings.Webex.TokenExpires = DateTime.UtcNow.AddMilliseconds(expiresIn).Ticks;
            await settings.Webex.SaveAsync(nameof(settings.Webex.TokenExpires));
        }
        return await GetUser();
    }

    public async Task LogoutAsync() => await settings.Webex.ClearAsync(nameof(settings.Webex.AccessToken));

    public async Task<List<SearchResult>> Search(string query)
    {
        throw new NotImplementedException();
    }

    public async Task AssignAsync(string id, PhoneDetails phone)
    {
        throw new NotImplementedException();
    }
    
    public async Task<WebexIdentityUserDto?> GetUser()
    {
        var httpClient = await GetHttpClient();
        if(httpClient == null) return null;
        var user = await httpClient.GetFromJsonAsync<WebexIdentityUserDto>("identity/scim/v2/Users/me");
        if(user == null) return null;
        var organizations = await httpClient.GetFromJsonAsync<WebexOrganizationsDto>("v1/organizations");
        if (organizations != null)
        {
            //TODO: Save list of orgs to use for drop down list
            var orgId = user.webex.organization.OrgId;
            var organization = organizations.organizations.FirstOrDefault(o => o.id == orgId);
            if (organization != null) user.webex.organization.name = organization.displayName;
        }
        else
        {
            var organization = await httpClient.GetFromJsonAsync<WebexIdentityOrganizationDto>($"/v1/identity/organizations/{user.webex.organization.organizationId}");
            if(organization != null) user.webex.organization.name = organization.displayName;
        }
        return user;
    }
    private async Task<HttpClient?> GetHttpClient()
    {
        if(new DateTime(settings.Webex.TokenExpires) < DateTime.UtcNow) return null;
        var token = settings.Webex.AccessToken;
        if(token == null) return null;
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://webexapis.com");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return httpClient;
    }
}