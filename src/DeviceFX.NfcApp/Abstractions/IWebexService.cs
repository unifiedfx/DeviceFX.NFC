using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface IWebexService
{
    Task<bool> LoginAsync(UserProfile user, string? orgId = null, string? email = null);
    Task LogoutAsync();
    Task<bool> UpdateUser(UserProfile user, string? orgId = null, bool retryLogin = false);
    Task UpdateOrganization(UserProfile user, string? orgId = null);
    Task<bool> SwitchOrganizationAsync(UserProfile user, string? orgId);
    Task<string?> AddDeviceByMac(string orgId, string mac, string model, string type, string? id = null);
    Func<Task<bool>>? RetryLogin { get; set; }
    Task<ActivationResult> AddDeviceByActivationCode(string orgId, string model, string type, string? id = null);
}
