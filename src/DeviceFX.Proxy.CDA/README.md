# DeviceFX.Proxy.CDA

This project provides a C# implementation of a proxy service that forwards requests to the [Cisco Device Access (CDA) API](https://it-developer.cisco.com/auth/customer-assets/device/Device%20Management%20Services/latest/).

The main purpose is to enable the DeviceFX.NFCPApp mobile application to sign NFC payload data using the CDA Sign Data operation.

The proxy service authorizes requests using a Webex access token that the mobile app obtained with the user logs in to Webex. The proxy service forwards the request to the CDA API using the provided ClientId and ClientSecret Environment Variables and returns the response.

