using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Model.Dto;
using DeviceFX.NfcApp.Model.Dto.Identity;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using IHttpClientFactory = DeviceFX.NfcApp.Abstractions.IHttpClientFactory;

namespace DeviceFX.NfcApp.Services;

public class DeviceFxService(
    Settings settings,
    ILogger<DeviceFxService> logger,
    IHttpClientFactory httpClientFactory,
    TelemetryClient? telemetryClient = null) : IWebexService, ISearchService
{
    public const string UserType = "User";
    public const string WorkspaceType = "Workspace";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly SemaphoreSlim tokenLock = new(1, 1);

    public Func<Task<bool>>? RetryLogin { get; set; }

    public async Task<bool> LoginAsync(UserProfile user, string? orgId = null, string? email = null)
    {
        if (await UpdateUser(user, orgId)) return true;
        if (!await AuthenticateInteractiveAsync(orgId, email)) return false;
        return await UpdateUserCore(user, orgId, retryLogin: false, throwOnError: true);
    }

    public async Task<bool> SwitchOrganizationAsync(UserProfile user, string? orgId)
    {
        if (string.IsNullOrWhiteSpace(orgId)) return false;
        if (!await RefreshTokensAsync(orgId) && !await AuthenticateInteractiveAsync(orgId, settings.Webex.Email))
            return false;
        return await UpdateUser(user, orgId);
    }

    public async Task LogoutAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(settings.Webex.IdToken))
                await RefreshTokensAsync();
            await EndBrowserSessionAsync();
        }
        catch (Exception e)
        {
            logger.LogDebug(e, "Logout end-session failed");
        }

        try
        {
            await RevokeTokensAsync();
        }
        catch (Exception e)
        {
            logger.LogDebug(e, "Logout revoke failed");
        }

        await settings.Webex.RemoveAsync(nameof(settings.Webex.AccessToken));
        await settings.Webex.RemoveAsync(nameof(settings.Webex.RefreshToken));
        await settings.Webex.RemoveAsync(nameof(settings.Webex.IdToken));
        await settings.Webex.RemoveAsync(nameof(settings.Webex.TokenExpires));
    }

    public async Task<string?> GetAccessTokenAsync(bool retryLogin = false)
    {
        await tokenLock.WaitAsync();
        try
        {
            if (!IsAccessTokenExpired && !string.IsNullOrEmpty(settings.Webex.AccessToken))
                return settings.Webex.AccessToken;
            if (await RefreshTokensAsync())
                return settings.Webex.AccessToken;
            if (!retryLogin || RetryLogin == null) return null;
        }
        finally
        {
            tokenLock.Release();
        }

        if (!await RetryLogin()) return null;
        return settings.Webex.AccessToken;
    }

    public Task<bool> UpdateUser(UserProfile user, string? orgId = null, bool retryLogin = false) =>
        UpdateUserCore(user, orgId, retryLogin, throwOnError: false);

    private async Task<bool> UpdateUserCore(UserProfile user, string? orgId, bool retryLogin, bool throwOnError)
    {
        var httpClient = await GetIdentityClient(retryLogin);
        if (httpClient == null) return false;
        try
        {
            using var accountResponse = await httpClient.GetAsync("account");
            var accountBody = await accountResponse.Content.ReadAsStringAsync();
            if (!accountResponse.IsSuccessStatusCode)
            {
                var message =
                    $"Account {(int)accountResponse.StatusCode}: {(string.IsNullOrWhiteSpace(accountBody) ? accountResponse.ReasonPhrase : accountBody)}";
                if (throwOnError) throw new HttpRequestException(message);
                logger.LogWarning("UpdateUser account {Status}: {Body}", (int)accountResponse.StatusCode, accountBody);
                return false;
            }

            var account = JsonSerializer.Deserialize<AccountDto>(accountBody, JsonOptions);
            using var orgsResponse = await httpClient.GetAsync("account/organizations");
            var orgsBody = await orgsResponse.Content.ReadAsStringAsync();
            if (!orgsResponse.IsSuccessStatusCode)
            {
                var message =
                    $"Organizations {(int)orgsResponse.StatusCode}: {(string.IsNullOrWhiteSpace(orgsBody) ? orgsResponse.ReasonPhrase : orgsBody)}";
                if (throwOnError) throw new HttpRequestException(message);
                logger.LogWarning("UpdateUser organizations {Status}: {Body}", (int)orgsResponse.StatusCode, orgsBody);
                return false;
            }

            var organizations = JsonSerializer.Deserialize<OrganizationDto[]>(orgsBody, JsonOptions);
            if (account == null || organizations == null) return false;

            string[] roles = [];
            Dictionary<string, JsonElement>? features = null;
            try
            {
                using var userInfoResponse = await httpClient.GetAsync("connect/userinfo");
                if (userInfoResponse.IsSuccessStatusCode)
                {
                    await using var stream = await userInfoResponse.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);
                    roles = ReadRoles(doc.RootElement);
                    if (doc.RootElement.TryGetProperty("features", out var featuresElement) &&
                        featuresElement.ValueKind == JsonValueKind.Object)
                    {
                        features = featuresElement.EnumerateObject()
                            .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal);
                    }

                    if (string.IsNullOrWhiteSpace(orgId) &&
                        doc.RootElement.TryGetProperty("org_id", out var orgIdElement))
                    {
                        orgId = orgIdElement.GetString();
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogDebug(e, "UserInfo");
            }

            user.Set(account, organizations, orgId, roles, features);
            await UpdateOrganization(user);
            return true;
        }
        catch (Exception e) when (e is not HttpRequestException || !throwOnError)
        {
            logger.LogWarning(e, "UpdateUser");
            if (throwOnError) throw;
            return false;
        }
    }

    public async Task UpdateOrganization(UserProfile user, string? orgId = null)
    {
        user.Organization = user.Organizations.FirstOrDefault(o => IdsEqual(o.Id, orgId)) ?? user.Organization;
        if (user.Organization is not { LicenseIds: null }) return;
        try
        {
            var httpClient = await GetApiClient(true);
            if (httpClient == null) return;
            var licenses = await httpClient.GetFromJsonAsync<WebexLicensesDto>("api/mobile/licenses");
            var filters = settings.Webex.LicenseFilters ?? [];
            user.Organization.LicenseIds =
                licenses?.items?
                    .Where(i => i.name != null && filters.Any(f =>
                        i.name.Contains(f, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(l => l.id)
                    .ToList() ??
                [];
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "UpdateOrganization");
        }
    }

    public async Task<string?> AddDeviceByMac(string orgId, string mac, string model, string type, string? id = null)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        var newModel = "Cisco " + Regex.Match(model, @"\d{4}").Value;
        var data = new Dictionary<string, object>
        {
            { "mac", mac },
            { "model", newModel }
        };
        data.Add(type == UserType ? "personId" : "workspaceId", id);
        var httpClient = await GetApiClient(true);
        if (httpClient == null) return null;
        var startTime = DateTime.UtcNow;
        const string devicesPath = "api/mobile/devices";
        var response = await httpClient.PostAsJsonAsync(devicesPath, data);
        var duration = DateTime.UtcNow - startTime;
        string? result = null;
        if (!response.IsSuccessStatusCode)
        {
            result = await ReadErrorAsync(response);
            logger.LogWarning("AddDeviceByMac error: {Result}", result);
        }

        if (telemetryClient != null)
        {
            telemetryClient.TrackDependency(dependencyTypeName: "HTTP", dependencyName: "WebexAddDeviceByMac",
                target: httpClient.BaseAddress?.ToString(), data: devicesPath, success: response.IsSuccessStatusCode,
                startTime: startTime, duration: duration, resultCode: response.StatusCode.ToString());
            await telemetryClient.FlushAsync(CancellationToken.None);
        }
        return result;
    }

    public async Task<ActivationResult> AddDeviceByActivationCode(string orgId, string model, string type, string? id = null)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        var newModel = "Cisco " + Regex.Match(model, @"\d{4}").Value;
        var data = new Dictionary<string, object>
        {
            { "model", newModel }
        };
        data.Add(type == UserType ? "personId" : "workspaceId", id);
        var httpClient = await GetApiClient(true);
        if (httpClient == null) return new ActivationResult(Error: "Not signed in");
        var startTime = DateTime.UtcNow;
        const string activationPath = "api/mobile/devices/activationCode";
        var response = await httpClient.PostAsJsonAsync(activationPath, data);
        var duration = DateTime.UtcNow - startTime;
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ReadErrorAsync(response);
            logger.LogWarning("AddDeviceByActivationCode error: {Result}", errorMessage);
            return new ActivationResult(Error: errorMessage ?? response.ReasonPhrase);
        }
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (telemetryClient != null)
        {
            telemetryClient.TrackDependency(dependencyTypeName: "HTTP", dependencyName: "AddDeviceByActivationCode",
                target: httpClient.BaseAddress?.ToString(), data: activationPath, success: response.IsSuccessStatusCode,
                startTime: startTime, duration: duration, resultCode: response.StatusCode.ToString());
            await telemetryClient.FlushAsync(CancellationToken.None);
        }
        var code = content.GetProperty("code").GetString();
        var expiry = content.GetProperty("expiryTime").GetDateTime();
        return new ActivationResult(code, expiry);
    }

    public async Task<List<SearchResult>> SearchAsync(string query, string orgId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var httpClient = await GetApiClient(true);
        if (httpClient == null) return [];
        var path = "api/mobile/numbers?";
        var encodedQuery = WebUtility.UrlEncode(query);
        if (long.TryParse(query, out var number))
            path += $"extension={encodedQuery}&phoneNumber={encodedQuery}&";
        path += $"ownerName={encodedQuery}";
        var types = new Dictionary<string, string> { { "PEOPLE", UserType }, { "PLACE", WorkspaceType } };
        WebexPhoneNumbersDto? numbers = null;
        try
        {
            numbers = await httpClient.GetFromJsonAsync<WebexPhoneNumbersDto>(path, cancellationToken: cancellationToken);
        }
        catch (TaskCanceledException e)
        {
            logger.LogTrace(e, "SearchAsync");
            return [];
        }
        catch (Exception e)
        {
            logger.LogError(e, "SearchAsync");
            return [];
        }
        return numbers?.phoneNumbers.Where(n => n.owner != null && types.ContainsKey(n.owner?.type!)).Select(n => new SearchResult
        {
            Id = n.owner?.id,
            Type = types[n.owner?.type!],
            Name = n.owner?.type == "PEOPLE" ? $"{n.owner.firstName} {n.owner.lastName}" : n.owner?.firstName,
            Number = number != 0 && n.extension != null && !n.extension.Contains(query) ? n.phoneNumber ?? n.extension : n.extension ?? n.phoneNumber
        }).ToList() ?? [];
    }

    public async Task CheckResult(SearchResult result, string? orgId, IReadOnlyList<string>? licenseIds)
    {
        if (result.Checked) return;
        var httpClient = await GetApiClient(true);
        if (httpClient == null)
        {
            result.Issue = SearchResultValidator.UnableToValidate;
            return;
        }
        if (result.Type == UserType) await CheckPerson();
        else await CheckWorkspace();
        result.Checked = true;

        async Task CheckPerson()
        {
            WebexPersonDto? person = null;
            try
            {
                person = await httpClient.GetFromJsonAsync<WebexPersonDto>($"api/mobile/people/{result.Id}?callingData=true");
            }
            catch (Exception e)
            {
                result.Issue = e.Message;
                logger.LogError(e, "CheckResult");
            }
            if (person == null)
            {
                result.Issue ??= SearchResultValidator.UnableToValidate;
                return;
            }
            result.Picture = person.avatar;
            SearchResultValidator.ApplyPersonLicenses(result, person.licenses, licenseIds);
        }
        async Task CheckWorkspace()
        {
            WebexWorkspaceDto? workspace = null;
            try
            {
                workspace = await httpClient.GetFromJsonAsync<WebexWorkspaceDto>($"api/mobile/workspaces/{result.Id}?includeDevices=true");
            }
            catch (Exception e)
            {
                result.Issue = e.Message;
                logger.LogError(e, "CheckResult");
            }
            if (workspace == null)
            {
                result.Issue ??= SearchResultValidator.UnableToValidate;
                return;
            }
            SearchResultValidator.ApplyWorkspaceCalling(
                result,
                workspace.calling?.type,
                workspace.calling?.webexCalling?.licenses,
                licenseIds);
        }
    }

    private Task<HttpClient?> GetIdentityClient(bool retryLogin = false) =>
        CreateClient(AuthorityBase, retryLogin);

    private Task<HttpClient?> GetApiClient(bool retryLogin = false) =>
        CreateClient(ApiHostBase, retryLogin);

    private async Task<HttpClient?> CreateClient(string baseAddress, bool retryLogin)
    {
        var token = await GetAccessTokenAsync(retryLogin);
        if (token == null) return null;
        var httpClient = await httpClientFactory.Create();
        httpClient.BaseAddress = new Uri(baseAddress);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return httpClient;
    }

    private bool IsAccessTokenExpired
    {
        get
        {
            if (string.IsNullOrEmpty(settings.Webex.AccessToken)) return true;
            if (settings.Webex.TokenExpires <= 0) return true;
            return new DateTime(settings.Webex.TokenExpires, DateTimeKind.Utc) < DateTime.UtcNow.AddMinutes(1);
        }
    }

    private string AuthorityBase =>
        (settings.Webex.Authority ?? "https://loginfx.com/identity").TrimEnd('/') + "/";

    private string ApiHostBase =>
        (settings.Webex.ApiHost ?? "https://app.devicefx.com").TrimEnd('/') + "/";

    private async Task<bool> AuthenticateInteractiveAsync(string? orgId, string? email)
    {
        var (verifier, challenge, state) = OidcPkce.Create();
        var authRequest =
            $"{AuthorityBase}connect/authorize?client_id={Uri.EscapeDataString(settings.Webex.ClientId)}" +
            $"&response_type=code&redirect_uri={Uri.EscapeDataString(settings.Webex.RedirectUrl)}" +
            $"&scope={Uri.EscapeDataString(settings.Webex.Scopes)}" +
            $"&code_challenge={challenge}&code_challenge_method=S256&state={Uri.EscapeDataString(state)}" +
            "&identity_provider=Webex";
        if (!string.IsNullOrWhiteSpace(email))
            authRequest += $"&login_hint={Uri.EscapeDataString(email)}";
        if (!string.IsNullOrWhiteSpace(orgId))
            authRequest += $"&org_id={Uri.EscapeDataString(orgId)}";

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
        if (authResult.Properties.TryGetValue("error_description", out var errorDescription) &&
            !string.IsNullOrEmpty(errorDescription))
            throw new HttpRequestException(errorDescription);
        if (authResult.Properties.TryGetValue("error", out var error) && !string.IsNullOrEmpty(error))
            throw new HttpRequestException(error);
        if (authResult.Properties.TryGetValue("state", out var returnedState) &&
            !string.Equals(returnedState, state, StringComparison.Ordinal))
            throw new HttpRequestException("Invalid login state");
        if (!authResult.Properties.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
            return false;

        var tokens = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = settings.Webex.RedirectUrl,
            ["client_id"] = settings.Webex.ClientId,
            ["code_verifier"] = verifier
        });
        return tokens != null;
    }

    private async Task<bool> RefreshTokensAsync(string? orgId = null)
    {
        var refreshToken = settings.Webex.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken)) return false;
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = settings.Webex.ClientId
        };
        if (!string.IsNullOrWhiteSpace(orgId))
            form["org_id"] = orgId;
        try
        {
            return await RequestTokenAsync(form) != null;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "RefreshTokensAsync");
            return false;
        }
    }

    private async Task<TokenResponse?> RequestTokenAsync(Dictionary<string, string> form)
    {
        using var httpClient = await httpClientFactory.Create();
        using var content = new FormUrlEncodedContent(form);
        using var response = await httpClient.PostAsync($"{AuthorityBase}connect/token", content);
        var body = await response.Content.ReadAsStringAsync();
        TokenResponse? tokens = null;
        try
        {
            tokens = JsonSerializer.Deserialize<TokenResponse>(body);
        }
        catch (JsonException)
        {
            // non-JSON error body
        }

        if (!response.IsSuccessStatusCode)
        {
            var description = tokens?.ErrorDescription ?? tokens?.Error ?? body;
            throw new HttpRequestException(string.IsNullOrWhiteSpace(description) ? response.ReasonPhrase : description);
        }

        if (tokens?.AccessToken is null) return null;
        settings.Webex.AccessToken = tokens.AccessToken;
        await settings.Webex.SaveAsync(nameof(settings.Webex.AccessToken));
        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            settings.Webex.RefreshToken = tokens.RefreshToken;
            await settings.Webex.SaveAsync(nameof(settings.Webex.RefreshToken));
        }
        if (!string.IsNullOrEmpty(tokens.IdToken))
        {
            settings.Webex.IdToken = tokens.IdToken;
            await settings.Webex.SaveAsync(nameof(settings.Webex.IdToken));
        }

        var expiresIn = tokens.ExpiresIn > 0 ? tokens.ExpiresIn : 3600;
        settings.Webex.TokenExpires = DateTime.UtcNow.AddSeconds(expiresIn).Ticks;
        await settings.Webex.SaveAsync(nameof(settings.Webex.TokenExpires));
        return tokens;
    }

    private async Task EndBrowserSessionAsync()
    {
        var logoutRequest =
            $"{AuthorityBase}connect/logout" +
            $"?client_id={Uri.EscapeDataString(settings.Webex.ClientId)}" +
            $"&post_logout_redirect_uri={Uri.EscapeDataString(settings.Webex.RedirectUrl)}";
        if (!string.IsNullOrEmpty(settings.Webex.IdToken))
            logoutRequest += $"&id_token_hint={Uri.EscapeDataString(settings.Webex.IdToken)}";

        try
        {
            if (!MainThread.IsMainThread)
            {
                await MainThread.InvokeOnMainThreadAsync(EndBrowserSessionAsync);
                return;
            }

            await WebAuthenticator.AuthenticateAsync(
                new Uri(logoutRequest),
                new Uri(settings.Webex.RedirectUrl));
        }
        catch (TaskCanceledException e)
        {
            logger.LogDebug(e, "Logout end-session canceled");
        }
    }

    private async Task RevokeTokensAsync()
    {
        var token = settings.Webex.RefreshToken ?? settings.Webex.AccessToken;
        if (string.IsNullOrEmpty(token)) return;
        using var httpClient = await httpClientFactory.Create();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = token,
            ["token_type_hint"] = settings.Webex.RefreshToken != null ? "refresh_token" : "access_token",
            ["client_id"] = settings.Webex.ClientId
        });
        await httpClient.PostAsync($"{AuthorityBase}connect/revocation", content);
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (content.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array &&
                errors.GetArrayLength() > 0 &&
                errors[0].TryGetProperty("description", out var description))
            {
                return description.GetString();
            }

            if (content.TryGetProperty("message", out var message))
                return message.GetString();
        }
        catch (Exception)
        {
            // fall through
        }

        return response.ReasonPhrase;
    }

    private static string[] ReadRoles(JsonElement root)
    {
        if (!root.TryGetProperty("role", out var role)) return [];
        if (role.ValueKind == JsonValueKind.String)
        {
            var value = role.GetString();
            return string.IsNullOrWhiteSpace(value) ? [] : [value];
        }

        if (role.ValueKind != JsonValueKind.Array) return [];
        return role.EnumerateArray()
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static bool IdsEqual(string? left, string? right) =>
        Guid.TryParse(left, out var a) && Guid.TryParse(right, out var b)
            ? a == b
            : string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}
