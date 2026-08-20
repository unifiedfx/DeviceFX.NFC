# DeviceFX.Proxy.CDA

This project provides a C# implementation of a proxy service that forwards requests to the [Cisco Device Access (CDA) API](https://it-developer.cisco.com/auth/customer-assets/device/Device%20Management%20Services/latest/).

The main purpose is to enable the DeviceFX.NfcApp mobile application to sign NFC payload data using the CDA Sign Data operation.

The mobile app does not send a Webex token to this proxy. It first completes **device attestation** (Apple App Attest or Google Play Integrity) and receives a short-lived JWT. The proxy requires that JWT on CDA routes, then forwards the request to Cisco using the `ClientId` / `ClientSecret` client-credentials grant and returns the response.

## Device attestation (shared flow)

Attestation is required before the app can call CDA sign-data (onboarding writes and provision Wi‑Fi). The sequence is the same on both platforms:

1. The app `GET`s `{CdaServiceUrl}/api/mobile/attestChallenge` and reads the `challenge` string from the JSON body. The proxy stores the same value in an `AuthChallenge` cookie (HttpOnly, Secure, 5 minutes) and reads it back on the subsequent POST.
2. The platform attestation API binds that challenge (App Attest `clientDataHash`, Play Integrity nonce) and returns a token.
3. The app `POST`s `{CdaServiceUrl}/api/mobile/attest` with `{ platform, attestationToken, keyId?, deviceId? }`. The challenge is not repeated in the JSON body.
4. The proxy validates the token with Apple or Google, then issues a JWT (`Issuer` = `DeviceFX.Proxy.CDA`, `Audience` = `DeviceFX.NfcApp`, HMAC-SHA256 `SigningKey`). Default lifetime is `TokenExpirationSeconds` (3600).
5. `CdaService` sends that JWT as `Authorization: Bearer` to `/api/mobile/cdasvcs/rc/v1/sign-data`. YARP requires a valid JWT, then injects a Cisco access token before forwarding to CDA.

Client wiring:

| Role | Location |
|---|---|
| Interface | `DeviceFX.NfcApp/Abstractions/IAttestationService.cs` |
| iOS implementation | `DeviceFX.NfcApp/Platforms/iOS/iOSAttestationService.cs` |
| Android implementation | `DeviceFX.NfcApp/Platforms/Android/AndroidAttestationService.cs` |
| DI registration | `MauiProgram.cs` (`#if IOS` / `#elif ANDROID`) |
| CDA caller | `DeviceFX.NfcApp/Services/CDAService.cs` |
| Proxy endpoints | `MobileController` (`AttestChallenge`, `Attest`) |
| App CDA base URL | `DeviceFX.NfcApp/Resources/Raw/appsettings.json` → `AppSettings.CDAServiceUrl` |

Proxy options bind from configuration / environment variables / user secrets onto `CiscoOptions` (validated at startup). Production values must not stay as the `appsettings.json` placeholders.

---

## Apple App Attest

The iOS app proves it is the genuine `com.devicefx.nfc` binary on a real device with a Secure Enclave. The proxy verifies Apple’s attestation object and then issues the JWT used for CDA.

### Apple Developer portal

1. [Certificates, Identifiers & Profiles](https://developer.apple.com/account/resources/identifiers/list) → **Identifiers** → App ID `com.devicefx.nfc` (must match `ApplicationId` in `DeviceFX.NfcApp.csproj` and `AppleAppId` on the proxy).
2. Enable the **App Attest** capability on that App ID and save.
3. Regenerate the iOS Development / Distribution / App Store provisioning profiles so they include App Attest.
4. Note the 10-character **Team ID** (Membership). The proxy hashes `{TeamID}.{BundleID}` as the App Attest relying-party ID.

App Attest requires a physical iPhone with a Secure Enclave. The Simulator typically reports `DCAppAttestService.SharedService.Supported == false`, and the app then fails CDA with “CDA service not supported on this device”.

### Mobile app

1. Register `iOSAttestationService` as `IAttestationService` (already done in `MauiProgram.cs` under `#if IOS`).
2. Do **not** set `com.apple.developer.devicecheck.appattest-environment` in `Platforms/iOS/Entitlements.plist`. When the key is omitted, App Attest uses the **sandbox** (development) environment. App Store, TestFlight, and enterprise-distributed builds **always** use Apple’s **production** environment, regardless of any local entitlement. The proxy accepts both AAGUIDs (`appattestdevelop` and `appattest`).
3. Keep `AppSettings.CDAServiceUrl` pointed at this proxy (default `https://cda.devicefx.com`).
4. Runtime behaviour (`iOSAttestationService`):
   - Abort if App Attest is unsupported.
   - `GET api/mobile/attestChallenge`.
   - `DCAppAttestService.GenerateKeyAsync()` then `AttestKeyAsync(keyId, SHA256(challenge))`.
   - `POST api/mobile/attest` with `platform: "Apple"`, base64 attestation object, `keyId`, and `deviceId` = `UIDevice.IdentifierForVendor`. The server-issued challenge is bound via the `AuthChallenge` cookie, not the JSON body.

The JWT is cached in-process until ~60 seconds before expiry, then attestation is repeated.

### CDA proxy

Set these `CiscoOptions` values (appsettings, environment variables, or user secrets):

| Setting | Purpose |
|---|---|
| `AppleAppId` | iOS bundle ID, e.g. `com.devicefx.nfc` |
| `AppleTeamId` | 10-character Apple Team ID |
| `AppleAppAttestRootCertPem` | PEM for [Apple App Attestation Root CA](https://www.apple.com/certificateauthority/). A current copy is already in `appsettings.json`. |
| `SigningKey` | HMAC secret for the JWT issued after a successful attest (min 32 characters). Shared with JWT Bearer validation. |

`AppleAttestService` then:

1. Parses the CBOR attestation object and requires an `x5c` chain of at least two certificates.
2. Verifies leaf → intermediate → Apple App Attestation Root CA.
3. Recomputes `SHA256(authData || SHA256(challenge))` and checks it against certificate extension OID `1.2.840.113635.100.8.2`.
4. Checks RP ID hash = `SHA256("{AppleTeamId}.{AppleAppId}")`.
5. Requires sign counter `0`, a known App Attest AAGUID, and `credentialId` matching `keyId`.

A mismatch on Team ID or bundle ID surfaces as `App ID verification failed`.

---

## Android Play Integrity

The Android app proves it is the Play-distributed `com.devicefx.nfc` binary on a genuine Android device. The proxy decodes the classic Integrity API token with Google’s servers (not local decryption).

### Google Cloud and Play Console

1. In [Google Cloud Console](https://console.cloud.google.com/), create or pick the Cloud project that will own Play Integrity usage.
2. **APIs & services** → **Enable APIs and services** → enable **[Play Integrity API](https://console.cloud.google.com/marketplace/product/google/playintegrity.googleapis.com)**.
3. Create a **service account** in that project. Grant it access to call Play Integrity (for example the *Play Integrity API* role, or equivalent). Create a JSON key.
4. In [Google Play Console](https://play.google.com/console) → app `com.devicefx.nfc` → **Protected with Play** → **Play Integrity API** → **Link Cloud project**. Select the same Cloud project. Linking also enables the API if it was not already on. Unlinked projects cannot decode tokens reliably and are not eligible for quota increases.
5. Leave **classic request response encryption** on **Let Google manage my response encryption**. This proxy calls `playintegrity.googleapis.com` `decodeIntegrityToken`; it does not decrypt tokens with locally managed keys.
6. Upload the app to a Play testing track (internal / closed / open) or production using the **same package name and signing certificate** Play has on file. Sideloaded or locally `dotnet build` APKs typically get `appRecognitionVerdict=UNRECOGNIZED_VERSION` and are rejected.
7. Add tester Google accounts to the testing track. Those accounts must be signed into Play on the device.

Default Cloud quota is 10,000 classic token requests and 10,000 server decodes per day. Request an increase from Play Console after the project is linked if needed.

### Mobile app

1. `ApplicationId` in `DeviceFX.NfcApp.csproj` must stay `com.devicefx.nfc` (must match Play and `GooglePackageName`).
2. The Android TFM already references `Xamarin.Google.Android.Play.Integrity` (classic Integrity Manager, not the Standard Integrity API).
3. Register `AndroidAttestationService` as `IAttestationService` (already done in `MauiProgram.cs` under `#elif ANDROID`).
4. Keep `AppSettings.CDAServiceUrl` pointed at this proxy.
5. Runtime behaviour (`AndroidAttestationService`):
   - `GET api/mobile/attestChallenge`.
   - Build a Play Integrity **classic nonce**: URL-safe Base64 of the UTF-8 challenge, no padding (`+`/`/` → `-`/`_`). Google requires 16–500 characters; the proxy’s 32-character hex GUID satisfies that after encoding.
   - `IntegrityManagerFactory.Create(context).RequestIntegrityToken(...)`.
   - `POST api/mobile/attest` with `platform: "Google"`, the Integrity token, and `deviceId` = Android ID. `keyId` is omitted on Android. The server-issued challenge is bound via the `AuthChallenge` cookie.

The device needs Google Play Store / Play services. Emulators and unlocked/rooted devices usually fail `MEETS_DEVICE_INTEGRITY`.

### CDA proxy

Paste the **entire** service-account JSON into `GoogleServiceJson` (escaped string in `appsettings.json`, or an environment variable / user secret). Set:

| Setting | Purpose |
|---|---|
| `GoogleServiceJson` | Service account JSON used as `GoogleCredential` with scope `https://www.googleapis.com/auth/playintegrity` |
| `GooglePackageName` | Play application ID, e.g. `com.devicefx.nfc` |
| `GoogleAppName` | Google API client application name, e.g. `DeviceFX NFC` |
| `SigningKey` | Same JWT HMAC secret as Apple |

`GoogleAttestService` then:

1. Calls `DecodeIntegrityToken` for `GooglePackageName`.
2. Requires `RequestDetails.RequestPackageName` and `AppIntegrity.PackageName` to match `GooglePackageName`.
3. Requires the payload nonce to equal the challenge **or** the URL-safe Base64 form of the challenge (what the app actually sent to Play).
4. Rejects tokens older than 5 minutes.
5. Requires `appRecognitionVerdict == PLAY_RECOGNIZED`.
6. Requires `deviceRecognitionVerdict` to contain `MEETS_DEVICE_INTEGRITY`.
7. Rejects `appLicensingVerdict == UNLICENSED`. `LICENSED` is accepted; `UNEVALUATED` is allowed with a warning because Play often has not indexed a brand-new testing-track `versionCode` yet, and `PLAY_RECOGNIZED` already proves the binary.

A `GoogleApiException` on decode usually means the Play Integrity API is not enabled, the service account cannot call it, or the Cloud project is not linked in Play Console.

---

## Proxy configuration reference

Required for CDA forwarding and JWT issuance, in addition to the platform settings above:

| Setting | Purpose |
|---|---|
| `ClientId` / `ClientSecret` | Cisco CDA OAuth client credentials (`https://id.cisco.com/oauth2/default/v1/token`) |
| `SigningKey` | JWT HMAC key (min 32 characters) |
| `Issuer` / `Audience` | Defaults `DeviceFX.Proxy.CDA` / `DeviceFX.NfcApp` |
| `TokenExpirationSeconds` | JWT lifetime; default `3600` |
| `ReverseProxy` | YARP route `/api/mobile/cdasvcs/rc/v1/sign-data` → `https://commerce.cisco.com/api/v2/ccs` |
| `ApplicationInsights:ConnectionString` | Application Insights connection string. The committed default is `__InstrumentationKey__` (telemetry is disabled until you replace it with your resource’s connection string). Production can also set `APPLICATIONINSIGHTS_CONNECTION_STRING`. |

Do not commit real `ClientSecret`, `SigningKey`, `GoogleServiceJson`, or Application Insights connection string values. Use environment variables or the project user secrets id in `DeviceFX.Proxy.CDA.csproj`.

---

## Application Insights

This proxy sends request, trace, exception, and dependency telemetry to the same Application Insights resource the mobile app uses (`ai-devicefx-beta-eastus2` in this deployment). `cloud_RoleName` is `DeviceFX.Proxy.CDA` so proxy traffic can be filtered separately from the MAUI app.

`appsettings.json` ships with:

```json
"ApplicationInsights": {
  "ConnectionString": "__InstrumentationKey__"
}
```

Replace `__InstrumentationKey__` with **your** Application Insights connection string (Azure Portal → Application Insights resource → Overview → Connection String). The host skips the SDK while the value is empty or still contains `__`.

For this repo’s production App Service, set `APPLICATIONINSIGHTS_CONNECTION_STRING` on `app-devicefx-proxy-cda` instead of committing a real value. Locally:

```bash
dotnet user-secrets set "ApplicationInsights:ConnectionString" "<your connection string>"
```

Telemetry lands in `requests`, `traces`, `exceptions`, and `dependencies`. The proxy does not emit `customEvents`; the README scan badge counts every custom event from the mobile app.

Custom Docker App Service images are not instrumented by the portal Application Insights agent. The SDK in this project is required.
