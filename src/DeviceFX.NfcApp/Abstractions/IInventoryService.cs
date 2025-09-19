using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface IInventoryService
{
    public Task AddPhoneAsync(PhoneDetails phone, bool merge);
    public Task ClearAsync();
    public Task<IList<PhoneDetails>> GetPhonesAsync();
    Task<string?> ExportAsync(string format = "csv");
}