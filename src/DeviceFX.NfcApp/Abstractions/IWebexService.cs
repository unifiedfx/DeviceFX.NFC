using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Model.Dto;

namespace DeviceFX.NfcApp.Abstractions;

public interface IWebexService
{
    Task<List<SearchResult>> Search(string query);
    Task AssignAsync(string id, PhoneDetails phone);
    Task<WebexIdentityUserDto?> LoginAsync();
    Task LogoutAsync();
    Task<WebexIdentityUserDto?> GetUser();
}