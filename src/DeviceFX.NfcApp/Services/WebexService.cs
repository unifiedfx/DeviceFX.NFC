using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Model.Dto;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace DeviceFX.NfcApp.Services;

public class WebexService(Settings settings, ILogger<WebexService> logger, TelemetryClient? telemetryClient = null) : IWebexService, ISearchService
{
    public Func<Task<bool>>? RetryLogin { get; set; }

    public async Task<bool> LoginAsync(UserProfile user, string? orgId = null, string? email = null)
    {
        if (await UpdateUser(user, orgId)) return true;
        var authRequest = $"{settings.Webex.AuthUrl}?client_id={settings.Webex.ClientId}&response_type=token&redirect_uri={Uri.EscapeDataString(settings.Webex.RedirectUrl)}" + 
                          $"&scope={Uri.EscapeDataString(settings.Webex.Scopes)}";
        if(email != null) authRequest += $"&email={Uri.EscapeDataString(email)}";
        WebAuthenticatorResult? authResult = null;
        try
        {
            authResult = await WebAuthenticator.AuthenticateAsync(
                new Uri(authRequest),
                new Uri(settings.Webex.RedirectUrl));
        }
        catch (TaskCanceledException e)
        {
            Debug.WriteLine(e);
        }
        if (authResult == null) return false;
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
        return await UpdateUser(user, orgId);
    }

    public async Task LogoutAsync() => await settings.Webex.RemoveAsync(nameof(settings.Webex.AccessToken));

    public async Task<bool> UpdateUser(UserProfile user, string? orgId = null, bool retryLogin = false)
    {
        var httpClient = await GetHttpClient(retryLogin);
        if(httpClient == null) return false;
        var identity = await httpClient.GetFromJsonAsync<WebexIdentityUserDto>("identity/scim/v2/Users/me");
        if(identity == null) return false;
        var organizations = await httpClient.GetFromJsonAsync<WebexOrganizationsDto>("v1/organizations");
        if(organizations == null) return false;
        user.Set(identity, organizations, orgId);
        await UpdateOrganization(user);
        return true;
    }

    public async Task UpdateOrganization(UserProfile user, string? orgId = null)
    {
        user.Organization = user.Organizations.FirstOrDefault(o => o.Id == orgId) ?? user.Organization;
        if(user.Organization is not {LicenseIds: null}) return;
        var httpClient = await GetHttpClient();
        if(httpClient == null) return;
        var licenses = await httpClient.GetFromJsonAsync<WebexLicensesDto>($"v1/licenses?orgId={user.Organization.Id}");
        user.Organization.LicenseIds = licenses?.items.Where(i => i.name.Contains("Webex Calling", StringComparison.InvariantCultureIgnoreCase)).Select(l => l.id).ToList() ?? new List<string>();
    }

    public async Task<string?> AddDeviceByMac(string orgId, string mac, string model, string? personId = null, string? workspaceId = null)
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
        var startTime = DateTime.UtcNow;
        var response = await httpClient.PostAsJsonAsync($"v1/devices?orgId={orgId}", data);
        var duration = DateTime.UtcNow - startTime;
        string? result = null;
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var trackingId = content.GetProperty("trackingId").GetString();
            result =  content.GetProperty("errors")[0].GetProperty("description").GetString();
            logger.LogWarning("AddDeviceByMac error: {Result}, trackingId: {TrackingId}", result, trackingId);
        }

        if (telemetryClient != null)
        {
            telemetryClient.TrackDependency(dependencyTypeName: "HTTP", dependencyName: "WebexAddDeviceByMac",
                target: httpClient.BaseAddress?.ToString(), data: $"v1/devices?orgId={orgId}", success: response.IsSuccessStatusCode,
                startTime: startTime, duration: duration, resultCode: response.StatusCode.ToString());
            await telemetryClient.FlushAsync(CancellationToken.None);
        }
        return result;
    }

    public async Task<ActivationResult> AddDeviceByActivationCode(string orgId, string model, string? personId = null,
        string? workspaceId = null)
    {
        if(personId == null && workspaceId == null) throw new ArgumentNullException(nameof(personId));
        var newModel = "Cisco " + Regex.Match(model, @"\d{4}").Value;
        var data = new Dictionary<string, object>
        {
            { "model", newModel }
        };
        if(personId != null) data.Add("personId", personId);
        if(workspaceId != null) data.Add("workspaceId", workspaceId);
        var httpClient = await GetHttpClient(true);
        if (httpClient == null) return null;
        var startTime = DateTime.UtcNow;
        var response = await httpClient.PostAsJsonAsync($"v1/devices/activationCode?orgId={orgId}", data);
        var duration = DateTime.UtcNow - startTime;
        string? result = null;
        if (!response.IsSuccessStatusCode)
        {
            string? trackingId = null;
            if (response.Headers.TryGetValues("trackingId", out var values))
            {
                trackingId = values.FirstOrDefault();
            }
            logger.LogWarning("AddDeviceByMac error: {Result}, trackingId: {TrackingId}", response.ReasonPhrase, trackingId);
            return new ActivationResult(Error: response.ReasonPhrase);
        }
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (telemetryClient != null)
        {
            telemetryClient.TrackDependency(dependencyTypeName: "HTTP", dependencyName: "AddDeviceByActivationCode",
                target: httpClient.BaseAddress?.ToString(), data: $"v1/devices/activationCode?orgId={orgId}", success: response.IsSuccessStatusCode,
                startTime: startTime, duration: duration, resultCode: response.StatusCode.ToString());
            await telemetryClient.FlushAsync(CancellationToken.None);
        }
        var code = content.GetProperty("code").GetString();
        var expiry = content.GetProperty("expiryTime").GetDateTime();
        return new ActivationResult(code, expiry);
    }

    public record ActivationResult(string? Code = null, DateTime? Expiry = null, string? Error = null);
    
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
        if (string.IsNullOrWhiteSpace(query)) return [];
        var httpClient = await GetHttpClient(true);
        if (httpClient == null) return new();
        var path = $"v1/telephony/config/numbers?orgId={orgId}";
        var encodedQuery = WebUtility.UrlEncode(query);
        if (long.TryParse(query, out var number))
            path += $"&extension={encodedQuery}&numberType=EXTENSION";
        else
            path += $"&ownerName={encodedQuery}&numberType=EXTENSION";
        var types = new Dictionary<string, string> {{"PEOPLE", "User"},{"PLACE", "Workspace"}};
        WebexPhoneNumbersDto? numbers = null;
        try
        {
            numbers = await httpClient.GetFromJsonAsync<WebexPhoneNumbersDto>(path, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "SearchAsync");
            return [];
        }
        return numbers?.phoneNumbers.Where(n=> types.ContainsKey(n.owner.type)).Select(n => new SearchResult
        {
            Id = n.owner.id,
            Type = types[n.owner.type],
            Name = n.owner.type == "PEOPLE" ? $"{n.owner.firstName} {n.owner.lastName}" : n.owner.firstName,
            Number = n.extension
        }).ToList() ?? [];
    }

    public async Task CheckResult(SearchResult result, string orgId, List<string> LicenseIds)
    {
        if(result.Checked) return;
        var httpClient = await GetHttpClient(true);
        if (httpClient == null)
        {
            result.Issue = "Unable to validate";
            return;
        }
        if (result.Type == "User") await CheckPerson();
        else await CheckWorkspace();
        result.Checked = true;

        async Task CheckPerson()
        {
            WebexPersonDto? person = null;
            try
            {
                person = await httpClient.GetFromJsonAsync<WebexPersonDto>($"v1/people/{result.Id}?orgId={orgId}&callingData=true");
            }
            catch (Exception e)
            {
                result.Issue = e.Message;
                logger.LogError(e, "CheckResult");
            }
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
            var license = LicenseIds.FirstOrDefault(l => person.licenses.Contains(l));
            if(license == null)
            {
                result.Checked = true;
                result.Issue = "No calling license";
            }
        }
        async Task CheckWorkspace()
        {
            WebexWorkspaceDto? workspace = null;
            try
            {
                workspace = await httpClient.GetFromJsonAsync<WebexWorkspaceDto>($"v1/workspaces/{result.Id}?orgId={orgId}&includeDevices=true");
            }
            catch (Exception e)
            {
                result.Issue = e.Message;
                logger.LogError(e, "CheckResult");
            }
            if (workspace == null)
            {
                result.Issue = "Unable to validate";
                return;
            }
            if(workspace.calling.webexCalling.licenses.Length == 0)
            {
                result.Checked = true;
                result.Issue = "No license";
                return;
            }
            var license = LicenseIds.FirstOrDefault(l => workspace.calling.webexCalling.licenses.Contains(l));
            if(license == null)
            {
                result.Checked = true;
                result.Issue = "No calling license";
            }
        }
    }
}