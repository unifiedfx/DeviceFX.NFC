---
name: android-emulator
description: >
  Launch and drive a local Android emulator from the CLI for MAUI/Android UI checks:
  visible emulator window, adb serial selection, Debug APK install, accessibility-tree
  taps, bounds dumps, and screenshots. USE FOR: "run the emulator", "Pixel emulator",
  "open the emulator so I can see it", "adb tap", "uiautomator dump", "install APK on
  emulator", "screenshot the emulator", "scan on inventory in the emulator",
  "/android-emulator". DO NOT USE FOR: iOS Simulator, physical-device debugging only,
  or Android Studio AVD Manager UI.
---

# Android emulator (CLI)

Drive an AVD with the Android SDK emulator + `adb`. Do not use Android Studio. Prefer a **visible** window unless the user asks for headless.

## SDK paths (macOS)

`adb` and `emulator` are often **not** on `PATH`.

```bash
SDK="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-$HOME/Library/Android/sdk}}"
EMU="$SDK/emulator/emulator"
ADB="$SDK/platform-tools/adb"
```

List AVDs: `"$EMU" -list-avds`. Use the AVD the user named; otherwise pick a listed device (e.g. `Pixel_10_Pro` if present).

## Launch (visible)

Start in the background and leave it running. **Do not** pass `-qt-hide-window` or `-no-window` unless the user wants it hidden.

```bash
export ANDROID_SDK_ROOT="$SDK"
"$EMU" -avd <AVD_NAME> -netdelay none -netspeed full
```

Wait until booted:

```bash
"$ADB" -s <SERIAL> wait-for-device
while [ "$("$ADB" -s <SERIAL> shell getprop sys.boot_completed | tr -d '\r')" != "1" ]; do sleep 2; done
```

Bring the window forward:

```bash
osascript -e 'tell application "System Events" to set frontmost of first process whose name contains "qemu-system" to true'
```

Serial: `"$ADB" devices -l`. Always pass `-s <SERIAL>` when more than one device is attached (emulator + physical phone is common). Emulator serials look like `emulator-5554`.

Stop that emulator with `"$ADB" -s <SERIAL> emu kill`. **Never** `pkill -f <AVD_NAME>` — the kill command can match itself.

## Install a Debug APK so layout changes land

Incremental FastDev installs often keep old XAML/handlers. For UI verification:

```bash
dotnet build <csproj> -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true
"$ADB" -s <SERIAL> uninstall <APPLICATION_ID> || true
"$ADB" -s <SERIAL> install -r -t <apk>
```

APK path is typically `bin/Debug/net10.0-android/<applicationId>-Signed.apk`. Read `<ApplicationId>` from the `.csproj`.

Launch without hard-coding MAUI's hashed activity name (it can change):

```bash
"$ADB" -s <SERIAL> shell monkey -p <APPLICATION_ID> -c android.intent.category.LAUNCHER 1
```

## Tap, dump, screenshot

Use `scripts/emu-tap.py` in this skill directory (not a throwaway `/tmp` copy).

```bash
python3 <skill>/scripts/emu-tap.py -s <SERIAL> texts
python3 <skill>/scripts/emu-tap.py -s <SERIAL> tap Inventory
python3 <skill>/scripts/emu-tap.py -s <SERIAL> tap OK
python3 <skill>/scripts/emu-tap.py -s <SERIAL> dump
"$ADB" -s <SERIAL> exec-out screencap -p > /tmp/emu.png
```

`tap` matches `text=` substring (case-insensitive), retries while the tree settles, and taps the node center. Optional index after the needle (default last match). `dump` prints `width x height [x1,y1][x2,y2] text` for nodes.

Bounds are **px**. Convert with density: `dp = px / (density/160)`. `wm density` / `wm size` on the device; 480 dpi is 3× (490dp = 1470px).

If the app shows a hardware-unavailable alert (e.g. NFC “Emulate a device scan?”), tap `OK` to continue the in-app fake path.

## Checklist

- [ ] Visible window unless the user asked to hide it
- [ ] `-s <SERIAL>` when multiple devices
- [ ] Boot completed before install/tap
- [ ] `EmbedAssembliesIntoApk=true` + uninstall before layout checks
- [ ] Launch via `monkey` / launcher, not a hardcoded `crc64…MainActivity`
- [ ] Taps via uiautomator text, not guessed coordinates
