using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using UFX.DeviceFX.NFC;
using UFX.DeviceFX.NFC.Ndef;

namespace DeviceFX.NfcApp.Services;

public class DeviceService(IServiceProvider provider, IInventoryService inventoryService, ILocationService locationService, Settings settings) : IDeviceService
{
    public async Task ScanPhoneAsync(Operation operation)
    {
        var tcs = new TaskCompletionSource();
        var nfcTagService = provider.GetRequiredService<INfcTagService>();
        if (!nfcTagService.ReadingAvailable)
        {
#if DEBUG
            var result = await Application.Current?.MainPage?.DisplayAlert("NFC not available", "Emulate a device scan?", "OK", "Cancel")!;
            if (result)
            {
                var rand = new Random().Next(0x0000, 0xFFFF).ToString("X4");
                operation.Phone =  new(
                    $"""
                    PID: DP-9841
                    LAN MAC: 12345678{rand}
                    SN: WZP281{rand}N
                    VID: V01
                    """);
                operation.State = OperationState.Success;
                await SetResult($"Saved to {operation.Phone.Pid}", true);
            }
            else await SetResult($"Cancelled");
#else
            await Application.Current?.MainPage?.DisplayAlert("Scan issue", "NFC is not available on this device", "OK")!;
#endif
            return;
        }
        List<NdefMessage> messages = [];
        nfcTagService.TagCallback = HandleTagCallback;
        nfcTagService.Closed = result =>
        {
            if (result != null) operation.Result = result;
            if (operation.State == OperationState.InProgress) operation.State = OperationState.Idle;
            tcs.TrySetResult();
        };
        try
        {
            await nfcTagService.OpenSessionAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            operation.Result = e.Message;
            operation.State = OperationState.Failure;
        }
        await tcs.Task;

        async Task<string?> HandleTagCallback(INfcTagStream stream, Action<string> alertMessage,
            CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMicroseconds(200), cancellationToken);
            if (cancellationToken.IsCancellationRequested) return null;
            await stream.ResetPosition(false, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return null;
            alertMessage("Reading Phone Details");
            NdefMessage? message = null;
            string? version = null;
            try
            {
                message = await stream.ReadNdefMessageAsync(cancellationToken);
                version = await stream.ReadPhoneVersion(cancellationToken);
            }
            catch (IOException e)
            {
                return await SetResult("Read error, try again", cancellationToken: cancellationToken);
            }
            if (cancellationToken.IsCancellationRequested) return null;
            var label = message?.Records.OfType<TextNdefRecord>().FirstOrDefault()?.Text;
            var certRecord = message?.Records.OfType<MimeNdefRecord>().FirstOrDefault(m => m.MimeType == "application/x-phoneos-cert");
            if (label == null)
            {
                return await SetResult("No label found", cancellationToken: cancellationToken);
            }
            operation.Phone = new PhoneDetails(label)
            {
                TagSerial = BitConverter.ToString(stream.ReadTagSerial()).Replace('-',':'),
                NfcVersion = version
            };
            if(certRecord?.Payload != null) operation.Phone.Certificate = certRecord.Payload;
            // Write onboarding details
            List<NdefRecord> records = [];
            if (operation.Callback != null)
            {
                try
                { 
                    alertMessage("Provisioning Phone");
                    records = await operation.InvokeCallbackAsync();
                }
                catch (Exception e)
                {
                    return await SetResult(e.Message, cancellationToken: cancellationToken);
                }
            }
            if (!operation.Onboarding.Any() && records.Count == 0)
            {
                await SetResult($"{operation.Phone.Pid} details read", true, cancellationToken: cancellationToken);
                return null;
            }
            var config = operation.Phone.CreateConfig(operation.Onboarding);
            if(config != null)
            {
                var payload = operation.Phone.Encrypt(config);
                if (payload != null)
                {
                    alertMessage("Writing Encrypted Onboarding Details");
                    records.Add(new MimeNdefRecord("application/x-phoneos-encrypt", payload));
                }
                else
                {
                    alertMessage("Writing Onboarding Details");
                    records.Add(new TextNdefRecord(config));
                }
            }
            messages.Add(new NdefMessage(records));
            messages.Add(new NdefMessage([]) {IsMessage = false});
            if (cancellationToken.IsCancellationRequested) return null;
            await stream.ResetPosition(true, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return null;
            await stream.WriteNdefMessagesAsync(messages, cancellationToken: cancellationToken);
            await SetResult($"Saved to {operation.Phone.Pid}", true, cancellationToken: cancellationToken);
            return null;
        }
        async ValueTask<string> SetResult(string message, bool result = false, CancellationToken cancellationToken = default)
        {
            if (result)
            {
                if (settings.IncludeLocation)
                {
                    var location = await locationService.GetLocationAsync(cancellationToken);
                    operation.Phone.Longitude = location.lon;
                    operation.Phone.Latitude = location.lat;
                    operation.Phone.Postcode = location.address?.postcode;
                    operation.Phone.Country = location.address?.country;
                    operation.Phone.AssetTag = settings.AsssetTag;
                }
                await inventoryService.AddPhoneAsync(operation.Phone);
            }
            operation.Result = message;
            operation.State = result ? OperationState.Success : OperationState.Failure;
            tcs.TrySetResult();
            return message;
        }
    }
}