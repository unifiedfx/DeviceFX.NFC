using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Services;

/// <summary>
/// Resolves Direct Webex vs DeviceFX implementations from the Settings integration toggle.
/// </summary>
public class IntegrationWebexService(
    Settings settings,
    WebexService webexService,
    DeviceFxService deviceFxService) : IWebexService, ISearchService
{
    private IWebexService CurrentWebex => settings.IsDeviceFx ? deviceFxService : webexService;
    private ISearchService CurrentSearch => settings.IsDeviceFx ? deviceFxService : webexService;

    public Func<Task<bool>>? RetryLogin
    {
        get => CurrentWebex.RetryLogin;
        set
        {
            webexService.RetryLogin = value;
            deviceFxService.RetryLogin = value;
        }
    }

    public Task<bool> LoginAsync(UserProfile user, string? orgId = null, string? email = null) =>
        CurrentWebex.LoginAsync(user, orgId, email);

    public Task LogoutAsync() => CurrentWebex.LogoutAsync();

    public Task<bool> UpdateUser(UserProfile user, string? orgId = null, bool retryLogin = false) =>
        CurrentWebex.UpdateUser(user, orgId, retryLogin);

    public Task UpdateOrganization(UserProfile user, string? orgId = null) =>
        CurrentWebex.UpdateOrganization(user, orgId);

    public Task<bool> SwitchOrganizationAsync(UserProfile user, string? orgId) =>
        CurrentWebex.SwitchOrganizationAsync(user, orgId);

    public Task<string?> AddDeviceByMac(string orgId, string mac, string model, string type, string? id = null) =>
        CurrentWebex.AddDeviceByMac(orgId, mac, model, type, id);

    public Task<ActivationResult> AddDeviceByActivationCode(string orgId, string model, string type, string? id = null) =>
        CurrentWebex.AddDeviceByActivationCode(orgId, model, type, id);

    public Task<List<SearchResult>> SearchAsync(string query, string orgId, CancellationToken cancellationToken = default) =>
        CurrentSearch.SearchAsync(query, orgId, cancellationToken);

    public Task CheckResult(SearchResult result, string? orgId, IReadOnlyList<string>? licenseIds) =>
        CurrentSearch.CheckResult(result, orgId, licenseIds);
}
