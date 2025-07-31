using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface IWebexService
{
    Task<List<SearchResult>> Search(string query);
    Task AssignAsync(string id, PhoneDetails phone);
}