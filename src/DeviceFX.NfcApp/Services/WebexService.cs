using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Services;

public class WebexService : IWebexService
{
    public async Task<List<SearchResult>> Search(string query)
    {
        throw new NotImplementedException();
    }

    public async Task AssignAsync(string id, PhoneDetails phone)
    {
        throw new NotImplementedException();
    }
}