# NFC Code Samples

The Cisco 9800 series IP Phones run PhoneOS and support NFC (Near Field Communication) technology, which allows for quick and easy onboarding and provisioning of the devices even when in the box.

## Overview

NFC technology enables the Cisco Desk Phone 9800 to be configured and provisioned by simply tapping it against a compatible NFC-enabled device such as Android or Apple mobile phone.
The NFC Chip on the Cisco Desk Phone 9800 can be used for the following purposes:
- **Reading**: Read phone label and device certificate
- **Onboarding**: Set the onboarding mode including writing an Activation Code or the Profile URL to register with a calling service
- **Configuration**: Configure key settings such as Wifi SSID, Password, and Custom CA Rule
- **NFC Version**: Read the version of Cisco NFC Chip capabilities

Refer to [Cisco NFC Onboarding Data](https://help.webex.com/en-us/article/5eomso/Prepare-NFC-onboarding-data-for-Desk-Phone-9800-Series) for details on the data format supported by the Cisco Desk Phone 9800.

## NFC Tag Memory Structure

The NFC Tag memory layout is as follows (green = Read/Write, grey = Read Only):

<table class="nfc">
  <tr>
    <td class="ro side"><span>Capability Container</span></td>
    <td class="rw side"><span>NDEF<br/>Record 1</span></td>
    <td class="rw side"><span>NDEF<br/>Record 2</span></td>
    <td class="rw blank1">Blank</td>
    <td class="ro">NDEF<br/>Label<br/>Record</td>
    <td class="ro">NDEF<br/>Certificate<br/>Record</td>
    <td class="ro blank2">Blank</td>
    <td class="ro">NFC<br />Version</td>
    </tr>
  <tr>
    <td colspan="4">6656 Bytes</td>
    <td colspan="3">1520 Bytes</td>
    <td>16 Bytes</td>
  </tr>
</table>

NDEF Records are written to the beginning of the NFC Tag after the Capability Container. Cisco manufacturer data is read from the end of the NFC Tag after the length defined in the Capability Container.
Cisco manufacturer data is stored in two NDEF Records, one for the label and one for the phone certificate. The last 16 bytes of the NFC Tag are reserved for the NFC Version, this is a string value that represents the version of the Cisco NFC Tag format.

## Using the DeviceFX NFC Library

The DeviceFX NFC Library allows reading and writing to any memory area of the Cisco 9800 NFC Tag and supports NDEF standard records such as Text, URI and MIME types.

* Reference the [DeviceFX.NFC](https://www.nuget.org/packages/DeviceFX.NFC/) NuGet package in your project.
* For .NET MAUI projects, you can use the `AddNfc` extension method
* Resolve the `INfcService` interface in your code to access NFC functionality.
* Register a 'TagCallback' on the `INfcService` to handle NFC tag events such as reading and writing.
* The 'TagCallback' will be passed an instance of 'INfcTagStream' which provides methods to read and write data to the NFC tag.

### Adding NFC Support in .NET MAUI
The following example demonstrates how to use the DeviceFX NFC Library in a .NET MAUI application:

```csharp
var builder = MauiApp.CreateBuilder();
builder.AddNfc();
```

### Using the INfcService
Once you have added NFC support, you can resolve the `INfcService` register a callback to handle NFC tag events then open a session to start listening.

```csharp
var nfcService = MauiApplication.Current.Services.GetRequiredService<INfcService>();
// Register a callback to handle NFC tag events
nfcTagService.TagCallback = async Task<string?> HandleTagCallback(INfcTagStream stream, Action<string> alertMessage, CancellationToken cancellationToken){
    // Reset the stream position to the end of the tag
    await stream.ResetPosition(false, cancellationToken);
    // Read the NDEF message from the Manufacuring Data area
    var message = await stream.ReadNdefMessageAsync(cancellationToken);
    // Extract the label record text from the NDEF message
    var label = message?.Records.OfType<TextNdefRecord>().FirstOrDefault()?.Text;
    // Extract the MIME type record for the certificate
    var certRecord = message?.Records.OfType<MimeNdefRecord>().FirstOrDefault(m => m.MimeType == "application/x-phoneos-cert");
    // Optionally update the message shown on the NFC Scan dialog UI
    alertMessage("Read Phone Details");
}
// Open the NFC session to start listening for NFC tags
await nfcTagService.OpenSessionAsync();
```

Note: NDEF records are contained inside NDEF Messages, technically you can have multiple NDEF Messages but most devices only support reading the first message, therefor it is recommended to only write a single message to the NFC Tag containing all NDEF Records.

The 'INfcTagStream' has a 'Position' property that can be used to read/write from any memory position (if the tag supports it), you can use this to read or write data at specific offsets. The 'ResetPosition' method can be used to reset the stream position to the beginning or end of the tag memory based on the size contained in the Capability Container.

### Reading from the NFC Tag

Use the 'ReadNdefMessageAsync' method to read a single NDEF Message form the NFC Tag from the current stream position (default is the start of the tag), the 'Position' will be incremented to the end of the message. Call multiple times to read additional messages.
A 'NdefMessage' contains a list of 'NdefRecord' instances, you can use the LINQ 'OfType<>' method to filter the records based on their type, the following types are supported:
- `TextNdefRecord`: For text records
- `UriNdefRecord`: For URI records
- `MimeNdefRecord`: For MIME type records


```csharp
var nfcService = MauiApplication.Current.Services.GetRequiredService<INfcService>();
nfcService.TagCallback = async (stream, alertMessage, cancellationToken) =>
{
    // Reset the stream position to the end of the tag
    await stream.ResetPosition(false, cancellationToken);
    // Read the NDEF message from the NFC tag
    var ndefMessage = await stream.ReadNdefMessageAsync(cancellationToken);
    // query for a MimeNdefRecord of type "application/x-phoneos-cert"
    var certRecord = ndefMessage?.Records.OfType<MimeNdefRecord>().FirstOrDefault(m => m.MimeType == "application/x-phoneos-cert");
    if(certRecord == null)
    {
        alertMessage("No certificate record found");
        return;
    }
    // create a new X509Certificate2 from the payload of the MimeNdefRecord
    var cert = new X509Certificate2(certRecord.Payload);
    // You can now use the certificate, for example to get the public key for encryting the onboarding record to wirte to the NFC Tag
    var key = cert.GetRSAPublicKey();
}
```


### Writing to the NFC Tag

Writing to the NFC Tag is similar to reading, you can use the `INfcTagStream` to write data to the tag. Here is an example of how to write a new NDEF message:

```csharp
var nfcService = MauiApplication.Current.Services.GetRequiredService<INfcService>();
nfcService.TagCallback = async (stream, alertMessage, cancellationToken) =>
{
    // Create a new NDEF message with a Uri record
    var ndefMessage = new NdefMessage(new UriNdefRecord("http://unifiedfx.com"));
    // Write the NDEF message to the NFC tag
    await stream.WriteNdefMessagesAsync([ndefMessage], cancellationToken: cancellationToken);
}
```

The onboarding payload is written to the NFC tag on the 9800 as a NDEF record in one of the following formats:
- `Plain Text`
- `Encrypted`
- `Signed`
