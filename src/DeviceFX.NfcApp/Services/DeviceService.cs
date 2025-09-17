using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using UFX.DeviceFX.NFC;
using UFX.DeviceFX.NFC.Ndef;

namespace DeviceFX.NfcApp.Services;

public class DeviceService(IServiceProvider provider, IInventoryService inventoryService, ILocationService locationService, Settings settings, TelemetryClient telemetryClient, ILogger<DeviceService> logger) : IDeviceService
{
    public async Task ScanPhoneAsync(Operation operation)
    {
        using var scanScope = logger.BeginScope("ScanPhoneAsync");
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
                    LAN MAC: 0CD5D39E{rand}
                    SN: WZP281{rand}N
                    VID: V01
                    """);
                operation.State = OperationState.Success;
                operation.Phone.Update(operation);
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
            if (operation.State == OperationState.InProgress) operation.State = OperationState.Idle;
            logger.LogDebug("NFC Session Closed: {Result}", result);
            tcs.TrySetResult();
        };
        try
        {
            logger.LogDebug("Opening NFC Session");
            await nfcTagService.OpenSessionAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "NFC Session Error");
            Console.WriteLine(e);
            operation.Result = e.Message;
            operation.State = OperationState.Failure;
        }
        await tcs.Task;

        async Task<string?> HandleTagCallback(INfcTagStream stream, Action<string> alertMessage,
            CancellationToken cancellationToken)
        {
            using var callbackScope = logger.BeginScope("HandleTagCallback");
            await Task.Delay(TimeSpan.FromMicroseconds(200), cancellationToken);
            if (cancellationToken.IsCancellationRequested) return null;
            await stream.ResetPosition(false, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return null;
            logger.LogDebug("Reading Phone Details");
            alertMessage("Reading Phone Details");
            NdefMessage? message = null;
            string? version = null;
            operation.Phone = new PhoneDetails();
            try
            {
                message = await stream.ReadNdefMessageAsync(cancellationToken);
            }
            catch (IOException e)
            {
                logger.LogError(e, "NDEF Read Error");
                return await SetResult("NDEF Read error, try again", cancellationToken: cancellationToken);
            }
            try
            {
                version = await stream.ReadPhoneVersion(cancellationToken);
            }
            catch (IOException e)
            {
                logger.LogError(e, "Phone Version Read Error");
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
            operation.Phone.Update(operation);
            if(certRecord?.Payload != null) operation.Phone.Certificate = certRecord.Payload;
            // Write onboarding details
            List<NdefRecord> records = [];
            if (operation.Callback != null)
            {
                try
                {
                    logger.LogDebug("Provisioning Phone");
                    alertMessage("Provisioning Phone");
                    var result = await operation.InvokeCallbackAsync();
                    if(result != null && result.Any()) records.AddRange(result);
                    if(operation.State == OperationState.Failure)
                        return await SetResult(operation.Result, cancellationToken: cancellationToken);
                } 
                catch (TaskCanceledException e)
                {
                    logger.LogWarning(e, "Provisioning Canceled");
                    return await SetResult(e.Message, cancellationToken: cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Provisioning Error");
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
                    logger.LogDebug("Writing Encrypted Onboarding Details");
                    alertMessage("Writing Encrypted Onboarding Details");
                    records.Add(new MimeNdefRecord("application/x-phoneos-encrypt", payload));
                }
                else
                {
                    logger.LogDebug("Writing Onboarding Details");
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
            logger.LogDebug("NDEF Message Saved: {Count}", messages.Count);
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
                }
                if(!string.IsNullOrWhiteSpace(settings.AssetTag))
                    operation.Phone.AssetTag = settings.AssetTag;
                await inventoryService.AddPhoneAsync(operation.Phone, operation.Merge);
            }
            operation.Result = message;
            operation.State = result ? OperationState.Success : OperationState.Failure;
            telemetryClient.TrackEvent(operation.Mode, new Dictionary<string, string?>
            {
                {"Result", result ? "Success" : "Failure"},
                {"Message", message},
                {"Model", operation.Phone?.Pid},
                {"Version", operation.Phone?.NfcVersion}
            });
            await telemetryClient.FlushAsync(cancellationToken);
            tcs.TrySetResult();
            return message;
        }
    }
}