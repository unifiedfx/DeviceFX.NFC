# DeviceFX NFC App
Fully documented open source application to leverage Cisco PhoneOS NFC capabilities on Cisco 9800 series IP Phones
* Provision to Webex
* Save Onboarding Mode to Cisco 9800 NFC Chip
* Inventory collection/export of phone details 

The **DeviceFX NFC** app in this repository is deployed to the [Google Play Store](https://play.google.com/store/apps/details?id=com.devicefx.nfc) and [Apple App Store](https://apps.apple.com/us/app/devicefx-nfc/id6749313143)

# NFC Code Samples

For more information on the Cisco 9800 NFC capabilities and how to read/write refer to the [NFC Code Samples](nfc.html) documentation.

## Provision to Webex

This provides a simple way to provision Cisco 9800 series IP Phones to Webex Calling by searching for a user or workspace, scanning the phone NFC tag and then provisioning the phone to Webex via the Webex API.

## Save Onboarding Mode

The onboarding mode of the phone can be set to one of the following modes:

* Activation Code
* Cloud Priority
* CUCM Priority

### Activation Code
Enter and save an Activation Code to the phone NFC tag. This code can then be used to register the phone with a calling service.

### Cloud Priority
This mode adjusts the PhoneOS boot order to prioritize cloud-based services.

### CUCM Priority
This mode adjusts the PhoneOS boot order to prioritize Cisco Unified Communications Manager (CUCM) registration.

## Inventory Collection/Export

The DeviceFX NFC app can collect and export inventory details of Cisco 9800 series IP Phones. This inludes the model, serial number, MAC address, and other relevant information.
The collected data can be exported in various formats such as CSV or XLS for further analysis or to provision on other systems e.g. CUCM via BAT File.
