---
title: "Development Setup"
layout: default
permalink: /docs/dev-setup/
---

# Development Setup — DeviceFX.NFC (VS Code + .NET MAUI)

This guide walks through how to set up your local development environment using **Visual Studio Code** to build, run, and debug the **DeviceFX.NFC** project.

---

## Prerequisites

Before proceeding, ensure you have:

- A supported operating system:  
  - **Windows** (supports Android)  
  - **macOS** (supports Android + iOS)  
  - **Linux** (only supports Android)  
- **.NET SDK** installed (9.x stable recommended)  
- .NET MAUI workload(s) installed  
- VS Code (latest stable)  
- VS Code extensions: C# Dev Kit, .NET MAUI  
- For Android development: Java JDK, Android SDK, emulator or physical device  
- For iOS targets (on macOS): Xcode + command line tools  

> Microsoft’s official .NET MAUI installation guidance is useful: see “Install Visual Studio / .NET / workloads” sections. ([learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?view=net-maui-9.0&utm_source=chatgpt.com))  
> The .NET MAUI extension for VS Code adds debugging, target switching, etc. ([marketplace.visualstudio.com](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui&utm_source=chatgpt.com))  

---

## 1. Install .NET SDK & Workloads

1. Download and install the latest **.NET SDK** from [https://dotnet.microsoft.com/en-us/download](https://dotnet.microsoft.com/en-us/download).  
2. Verify by running:
   ```bash
   dotnet --version
   ```
3. Install the MAUI workload(s):
   ```bash
   dotnet workload install maui
   ```
   On Linux (only Android supported):
   ```bash
   dotnet workload install maui-android
   ```
4. Verify installation:
   ```bash
   dotnet workload list
   ```

If the MAUI workload isn't present, builds will fail.  
Also you may want to install MAUI project templates (optional):
```bash
dotnet new install Microsoft.Maui.Templates
```

---

## 2. Clone & Open Project

1. Clone the repository:
   ```bash
   git clone https://github.com/unifiedfx/DeviceFX.NFC.git
   cd DeviceFX.NFC
   ```
2. Confirm that the solution file `DeviceFX.NfcApp.sln` exists.  
3. Launch VS Code at the repository root:
   ```bash
   code .
   ```

---

## 3. Install VS Code Extensions

In VS Code, install:

- **C# Dev Kit** (required) ([code.visualstudio.com](https://code.visualstudio.com/docs/csharp/get-started?utm_source=chatgpt.com))  
- **.NET MAUI** extension (which depends on C# Dev Kit) ([marketplace.visualstudio.com](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui&utm_source=chatgpt.com))  

These provide tooling support: build, debugging, project explorer, etc.

After installation, you may see a .NET MAUI “walkthrough” or setup prompts. Follow them to connect your Microsoft account (for Dev Kit) and configure environment. ([learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?view=net-maui-9.0&utm_source=chatgpt.com))

---

## 4. Configure Android (if targeting Android)

To build or debug on Android:

1. Ensure Java JDK is installed (e.g. OpenJDK 17).  
2. Have the Android SDK installed (via Android Studio or command line).  
3. Optionally, install an Android emulator system image (e.g. API 34 or similar).  
4. Set environment variables (on Windows / macOS / Linux) like:

   ```bash
   export ANDROID_HOME=/path/to/android/sdk
   export ANDROID_SDK_ROOT=$ANDROID_HOME
   export JAVA_HOME=/path/to/java/jdk
   export PATH=$PATH:$ANDROID_HOME/emulator:$ANDROID_HOME/platform-tools
   ```

5. Use the MAUI target `InstallAndroidDependencies` if needed:
   ```bash
   dotnet build -t:InstallAndroidDependencies -f:net8.0-android      -p:AndroidSdkDirectory="$ANDROID_HOME"      -p:JavaSdkDirectory="$JAVA_HOME"      -p:AcceptAndroidSDKLicenses=true
   ```

6. In VS Code, you may run the command palette (Ctrl+Shift+P / Cmd+Shift+P) → `​.NET MAUI: Configure Android` → and set / refresh the Android environment. ([learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?view=net-maui-9.0&utm_source=chatgpt.com))  

---

## 5. Configure iOS (if on macOS)

If you're on macOS and want to build for iOS:

1. Install Xcode from the App Store.  
2. Install Xcode command line tools:
   ```bash
   xcode-select --install
   ```
3. Accept license agreements, open Xcode at least once.  
4. Ensure simulator runtimes are installed (Xcode → Preferences → Components).  
5. In VS Code, run `​.NET MAUI: Configure Apple` → Refresh Apple environment. ([learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?view=net-maui-9.0&utm_source=chatgpt.com))  

---

## 6. Select Startup Project & Debug Target in VS Code

- In the status bar, click the `{ }` icon next to file language to select the debugging target (e.g. Android emulator, iOS Simulator, etc.). ([marketplace.visualstudio.com](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui&utm_source=chatgpt.com))  
- Ensure the correct **startup project** (in the solution) is selected (e.g. the MAUI app project).  
- Use `F5` or the Run toolbar button to build & launch.

---

## 7. Build / Run from CLI (optional)

You can also build and run via CLI to check for compile errors before using VS Code:

```bash
# e.g. build Android
dotnet build -f:net8.0-android

# or run
dotnet run -f:net8.0-android

# similarly for iOS (on macOS)
dotnet build -f:net8.0-ios
```

If build fails, examine the error messages, ensure workloads and SDKs are installed and paths configured.

---

## 8. Troubleshooting Tips & Notes

- Make sure your **.NET MAUI extension** is up to date. ([marketplace.visualstudio.com](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui&utm_source=chatgpt.com))  
- If you see errors about missing Android SDK / Java, double-check your environment variable paths.  
- Use VS Code’s **Output** pane / **Problems** tab to inspect build issues.  
- On macOS, if iOS build fails, ensure Xcode installation and device/simulator support are correct.  
- Hot reload and rapid iteration: the extension supports hot reload (enable in settings) for changes without full redeploy. ([marketplace.visualstudio.com](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui&utm_source=chatgpt.com))  
- If targeting multiple platforms in the same project, ensure all the workloads are installed.

---

## 9. Project-Specific Considerations (DeviceFX.NFC)

While this setup guide covers generic MAUI tooling, here are a few things to check specifically for **DeviceFX.NFC**:

- Make sure the solution builds cleanly before trying to run.  
- Inspect the `NuGet.config`/`NuGetSource` configuration in the repo: there may be custom package sources or versions.  
- Verify any platform-specific permissions or entitlements (e.g. NFC capabilities in Android manifest or iOS entitlements).  
- Test on physical devices when NFC hardware features are needed (emulators often lack NFC). 

---

## 10. Summary / Checklist

| Step | Description |
|---|---|
| 1 | Install .NET SDK & MAUI workload |
| 2 | Clone repository, open in VS Code |
| 3 | Install C# Dev Kit + .NET MAUI extension |
| 4 | Configure Android dependencies & SDK |
| 5 | (macOS only) Configure iOS / Xcode environment |
| 6 | Select startup project / target in VS Code |
| 7 | Build / run from CLI or via VS Code |
| 8 | Troubleshoot errors as needed |
| 9 | Validate NFC, permissions, platform-specific settings |
| 10 | Use physical devices for NFC testing |

---
