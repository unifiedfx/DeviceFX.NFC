using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Model.Dto;

namespace DeviceFX.NfcApp.Services;

public class WebexService(Settings settings) : IWebexService, ISearchService
{
    public Func<Task<bool>>? RetryLogin { get; set; }

    public async Task<WebexAccount?> LoginAsync(string? orgId = null, string? email = null)
    {
        var account = await GetAccount(orgId);
        if (account != null) return account;
        var authRequest = $"{settings.Webex.AuthUrl}?client_id={settings.Webex.ClientId}&response_type=token&redirect_uri={Uri.EscapeDataString(settings.Webex.RedirectUrl)}" + 
                          $"&scope={Uri.EscapeDataString(settings.Webex.Scopes)}";
        if(email != null) authRequest += $"&email={Uri.EscapeDataString(email)}";
        var authResult = await WebAuthenticator.AuthenticateAsync(
            new Uri(authRequest),
            new Uri(settings.Webex.RedirectUrl));
        if(authResult.Properties.TryGetValue("error_description", out var property)) throw new HttpRequestException(property);
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
        return await GetAccount(orgId);
    }

    public async Task LogoutAsync() => await settings.Webex.RemoveAsync(nameof(settings.Webex.AccessToken));
    
    public async Task<WebexAccount?> GetAccount(string? orgId = null, bool retryLogin = false)
    {
        var httpClient = await GetHttpClient(retryLogin);
        if(httpClient == null) return null;
        var user = await httpClient.GetFromJsonAsync<WebexIdentityUserDto>("identity/scim/v2/Users/me");
        if(user == null) return null;
        var organizations = await httpClient.GetFromJsonAsync<WebexOrganizationsDto>("v1/organizations");
        if(organizations == null) return null;
        var currentOrgId = organizations.organizations.All(o => o.id != orgId) ? orgId : WebexIDTypes.Organization.ConvertToBase64Id(user.webex.organization.organizationId);
        var licenses = await httpClient.GetFromJsonAsync<WebexLicensesDto>($"v1/licenses?orgId={currentOrgId}");
        return licenses == null ? null : new WebexAccount(user, organizations, licenses);
    }
    public async Task<bool> UpdateOrganization(WebexAccount account, string orgId)
    {
        if(account.Organizations.All(o => o.Id != orgId)) return false;
        var httpClient = await GetHttpClient(false);
        if(httpClient == null) return false;
        var licenses = await httpClient.GetFromJsonAsync<WebexLicensesDto>($"v1/licenses?orgId={orgId}");
        if(licenses == null) return false;
        account.Update(orgId, licenses);
        return true;
    }
    
    public async Task<string?> AddDeviceByMac(WebexAccount account, string mac, string model, string? personId = null, string? workspaceId = null)
    {
        if(personId == null && workspaceId == null) throw new ArgumentNullException(nameof(personId));
        var newModel = "Cisco " + Regex.Match(model, @"\d{4}").Value;
        var data = new Dictionary<string, object>
        {
            { "mac", mac },
            { "model", newModel }
        };
        if(personId != null) data.Add("personId", personId);
        if(workspaceId != null) data.Add("workspaceId", workspaceId);
        var httpClient = await GetHttpClient(true);
        if (httpClient == null) return null;
        var response = await httpClient.PostAsJsonAsync($"v1/devices?orgId={account.CurrentOrgId}", data);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            return content.GetProperty("errors")[0].GetProperty("description").GetString();
        }
        return null;
    }
    
    private async Task<HttpClient?> GetHttpClient(bool retryLogin = false)
    {
        if (new DateTime(settings.Webex.TokenExpires) < DateTime.UtcNow)
        {
            if (!retryLogin || RetryLogin == null) return null;
            if (!await RetryLogin()) return null;
        }
        var token = settings.Webex.AccessToken;
        if(token == null) return null;
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://webexapis.com");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return httpClient;
    }

    public async Task<List<SearchResult>> SearchAsync(string query, string orgId, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetHttpClient(true);
        if (httpClient == null) return new();
        var filter = $"(active eq true) and (displayName sw \"{query}\" or userName sw \"{query}\" or name.givenName sw \"{query}\" or name.familyName sw \"{query}\")";
        var encoded = WebUtility.UrlEncode(filter);
        var guid = WebexIDTypes.Organization.ConvertToGuid(orgId);
        var users = await httpClient.GetFromJsonAsync<WebexIdentityUsersDto>($"identity/scim/{guid}/v2/Users?filter={encoded}", cancellationToken: cancellationToken);
        return users?.Resources.Where(r => r.phoneNumbers?.Length > 0).Select(r => new SearchResult
        {
            Id = WebexIDTypes.People.ConvertToBase64Id(r.id),
            Type = "User",
            Name = r.displayName,
            Number = r.phoneNumbers?.Where(n => n.primary)
                .Select(n => n.value).FirstOrDefault()
        }).ToList() ?? new();
    }

    public async Task CheckResult(SearchResult result, WebexAccount account)
    {
        if(result.Checked) return;
        var httpClient = await GetHttpClient(true);
        if (httpClient == null)
        {
            result.Issue = "Unable to validate";
            return;
        }
        var person =  await httpClient.GetFromJsonAsync<WebexPersonDto>($"v1/people/{result.Id}?orgId={account.CurrentOrgId}&callingData=true");
        if (person == null)
        {
            result.Issue = "Unable to validate";
            return;
        }
        result.Picture = person.avatar;
        if(person.licenses?.Length == 0)
        {
            result.Checked = true;
            result.Issue = "No license";
            return;
        }
        var license = account.CallingLicenses.FirstOrDefault(l => person.licenses.Contains(l.Id));
        if(license == null)
        {
            result.Checked = true;
            result.Issue = "No calling license";
            return;
        }
        result.Checked = true;
    }
}