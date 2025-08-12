using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Model.Dto;

namespace DeviceFX.NfcApp.Abstractions;

public interface IWebexService
{
    Task AssignAsync(string id, PhoneDetails phone);
    Task<WebexIdentityUserDto?> LoginAsync(string? email = null);
    Task LogoutAsync();
    Task<WebexIdentityUserDto?> GetUser(bool retryLogin = false);
    Func<Task<bool>>? RetryLogin { get; set; }
}