 ---
title: "Badge Function (GetScanCount)"
layout: default
permalink: /docs/badge-function/
---

# Azure Function: GetScanCount (NFC Scan Badge)

This Azure Function powers the "Total NFC Scans" badge shown in the [README](../README.md):

![Endpoint Badge](https://img.shields.io/endpoint?url=https%3A%2F%2Ffunc-devicefx-nfc-badge-f2hwdqhrgzfjbqdc.eastus2-01.azurewebsites.net%2Fapi%2FGetScanCount)

It queries Application Insights using the [App Insights REST API](https://api.applicationinsights.io) (read-only) and returns [shields.io](https://shields.io) compatible JSON.

## Why a separate function?

- Keeps badge logic isolated from the mobile app and proxy services.
- Allows independent deployment/scaling of the public badge endpoint (anonymous).
- The count is derived from `customEvents` telemetry emitted by the MAUI app during NFC scans, provisioning, and inventory operations (see `DeviceService.ScanPhoneAsync` + `TelemetryClient.TrackEvent`).

## Required App Settings

| Name                    | Required | Description |
|-------------------------|----------|-------------|
| `APP_INSIGHTS_APP_ID`   | Yes      | Application Insights application ID (from the resource used by the mobile app). |
| `APP_INSIGHTS_API_KEY`  | Yes      | API key with **read** permissions for the App Insights resource (create via Azure Portal > API Access). |

## Optional App Settings

| Name                    | Default                     | Description |
|-------------------------|-----------------------------|-------------|
| `APP_INSIGHTS_KQL_QUERY` | `customEvents \| count`    | Full KQL query that must return a single numeric value in the first cell of the first row. You can embed constants (baselines), filters, time ranges, etc. directly in the query. Example: `print 1234 + toscalar(customEvents \| count)`. |

## Error / Configuration States

The endpoint **always returns HTTP 200 + valid shields.io JSON** so the badge renders reliably in GitHub READMEs.

Visible `message` values in the badge:
- Normal count (e.g. `1247`)
- `unconfigured` (light grey) — missing `APP_INSIGHTS_APP_ID` / `APP_INSIGHTS_API_KEY`
- `error` (red) — query failure, bad API key, malformed result shape, parse error, etc.

Logs in the Function App will contain details for troubleshooting.

## Deployment Notes

- The Function App is a .NET 9 isolated Azure Functions app (`src/DeviceFX.Function`).
- It has no dependency on the main mobile app build (excluded from `azure-pipelines.yml`).
- Deploy via:
  - Azure Functions Core Tools (`func azure functionapp publish ...`)
  - Azure Portal / GitHub Actions / Azure DevOps (separate pipeline recommended)
  - VS Code Azure Functions extension
- Requires the two App Insights read settings above (plus standard `AzureWebJobsStorage` and `FUNCTIONS_WORKER_RUNTIME`).

## Local Development

1. Copy `src/DeviceFX.Function/local.settings.json` and fill real values (or keep placeholders to test the "unconfigured" path).
2. Run the function project (F5 in VS Code or `func start` from the `src/DeviceFX.Function` dir after `dotnet build`).
3. Hit `http://localhost:PORT/api/GetScanCount` (port shown in launch profile or output).

## Updating the Badge Query Without Code Changes

Update the `APP_INSIGHTS_KQL_QUERY` Application Setting. The query itself can include any baseline or calculation (e.g. `print 1234 + toscalar(customEvents | count)`). No code change or mobile app rebuild is required.

## References

- Source: [src/DeviceFX.Function/GetScanCount.cs](../src/DeviceFX.Function/GetScanCount.cs)
- Telemetry source: [src/DeviceFX.NfcApp/Services/DeviceService.cs](../src/DeviceFX.NfcApp/Services/DeviceService.cs) (TrackEvent calls)
- Badge URL in README: `https://img.shields.io/endpoint?url=.../api/GetScanCount`
- Issue: [#14](https://github.com/unifiedfx/DeviceFX.NFC/issues/14) — Extend scan badge to preserve lifetime total beyond App Insights retention.