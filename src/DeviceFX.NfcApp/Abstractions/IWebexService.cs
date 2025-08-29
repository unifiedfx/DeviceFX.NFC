using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface IWebexService
{
    Task<WebexAccount?> LoginAsync(string? orgId = null, string? email = null);
    Task LogoutAsync();
    Task<WebexAccount?> GetAccount(string? orgId = null, bool retryLogin = false);
    Task<bool> UpdateOrganization(WebexAccount account, string orgId);
    Task<string?> AddDeviceByMac(WebexAccount account, string mac, string model, string? personId = null, string? workspaceId = null);
    Func<Task<bool>>? RetryLogin { get; set; }
}