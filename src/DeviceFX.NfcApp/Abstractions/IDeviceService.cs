using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Abstractions;

public interface IDeviceService
{
    public Task ScanPhoneAsync(Operation operation);
}