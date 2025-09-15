using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface IWebexService
{
    Task<bool> LoginAsync(UserProfile user, string? orgId = null, string? email = null);
    Task LogoutAsync();
    Task<bool> UpdateUser(UserProfile user, string? orgId = null, bool retryLogin = false);
    Task UpdateOrganization(UserProfile user, string? orgId = null);
    Task<string?> AddDeviceByMac(string orgId, string mac, string model, string? personId = null, string? workspaceId = null);
    Func<Task<bool>>? RetryLogin { get; set; }
}