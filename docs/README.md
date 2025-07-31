---
title: Blogging Like a Hacker
---

# DeviceFX NFC Open Source Mobile Application
Fully documented open source application to leverage Cisco PhoneOS NFC capabilities on Cisco 9800 series IP Phones
* Provision to Webex
* Save Onboarding Mode to Cisco 9800 NFC Chip
* Inventory collection/export of phone details 

The **DeviceFX NFC** app in this repository is deployed to the [Google Play Store](https://play.google.com/store/apps/details?id=com.devicefx.nfc) and [Apple App Store](https://apps.apple.com/us/app/devicefx-nfc/id6749313143)

[<img src="/img/google-play.png" alt="Google Play Store" />](https://play.google.com/store/apps/details?id=com.devicefx.nfc)

[<img src="/img/apple-store.png" alt="Google Play Store" />](https://apps.apple.com/us/app/devicefx-nfc/id6749313143)

For more information on the Cisco 9800 NFC capabilities, refer to the [NFC Code Samples](/docs/nfc.md) documentation.

The NFC Tag memory layout is as follows:

<style>
    table  {border-collapse:collapse;border-spacing:0;}
    tr:first-child td { height: 100px; }
td {
    font-family:Arial, sans-serif;
    text-align: center;
    vertical-align: middle;
    border-color:black;
    border-style:solid;
    border-width:1px;
    color: black;
}
.rw {
    background-color: #00f200;
}
.ro {
  background-color: #f2f2f2;
}
td span {
    writing-mode: sideways-lr;
}
tr:first-child td:nth-child(1) { width: 40px; }
tr:first-child td:nth-child(2) { width: 40px; }
tr:first-child td:nth-child(3) { width: 40px;}
tr:first-child td:nth-child(4) { width: 350px; }
tr:first-child td:nth-child(7) { width: 180px; }
</style>
<table class="tg">
  <tr>
    <td class="ro"><span>Capability Container</span></td>
    <td class="rw"><span>NDEF</span><span>Record 1</span></td>
    <td class="rw"><span>NDEF</span><span>Record 2</span></td>
    <td class="rw">Blank</td>
    <td class="ro">NDEF<br/>Label<br/>Record</td>
    <td class="ro">NDEF<br/>Certificate<br/>Record</td>
    <td class="ro">Blank</td>
    <td class="ro">NFC<br/>Version</td>
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
Once you have added NFC support, you can resolve the `INfcService` in your code
```csharp
var nfcService = MauiApplication.Current.Services.GetRequiredService<INfcService>();
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
```

Note: NDEF records are contained inside NDEF Messages, technically you can have multiple NDEF Messages but most devices only support reading the first message, therefor it is recommended to only write a single message to the NFC Tag containing all NDEF Records.

### Writing to the NFC Tag

Writing to the NFC Tag is similar to reading, you can use the `INfcTagStream` to write data to the tag. Here is an example of how to write a new NDEF message:

```csharp
var nfcService = MauiApplication.Current.Services.GetRequiredService<INfcService>();
nfcService.TagCallback = async (stream, alertMessage, cancellationToken) =>
{
    // Create a new NDEF message with a Text record
    var ndefMessage = new NdefMessage(new TextNdefRecord("New Label"));
    // Write the NDEF message to the NFC tag
}
```