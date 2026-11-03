# SoundGuard

Tray app for Windows 11 that stops call audio from leaking to your speakers when a headset disconnects.

## How it works

- SoundGuard remembers your current default playback device as the **guarded device** (stored in `%LOCALAPPDATA%\SoundGuard\guarded-device.txt`).
- If you switch the default device yourself, the new device becomes the guarded one. Nothing is muted.
- If the guarded device disappears (unplugged, Bluetooth lost) and Windows falls back to **speaker-like** outputs (Speakers, HDMI/display audio, SPDIF, Line Level), those outputs are **muted**.
- If Windows falls back to another headset, headphones or handset, nothing is muted and that device becomes the guarded one. Multiple headsets work without configuration.
- When the guarded device returns, the previous mute state is restored and the device is made the default again (if `RestoreDefaultOnReconnect` is `true`).
- It is event-driven (Windows Core Audio notifications), with no polling, so CPU use is about zero.

Devices are classified by their Windows endpoint form factor, not by name.

## Install

1. Unzip `SoundGuard-self-contained.zip` (no prerequisites) or `SoundGuard-framework-dependent.zip` (needs the .NET 10 Desktop Runtime) into a **permanent** folder, e.g. `C:\Tools\SoundGuard`. Keep `SoundGuard.exe` and `appsettings.json` together.
2. Run `SoundGuard.exe`. A shield icon appears in the tray.
3. Right-click the icon and tick **Start with Windows**. This stores the exe's current path in `HKCU\...\Run`. If you move the exe later, untick and tick it again.

The exe is not code-signed, so SmartScreen may warn on first start. Choose "More info", then "Run anyway".

## Tray menu

| Entry | Purpose |
|---|---|
| Pause / Resume | Stop guarding and unmute. Use it when you want to listen on the speakers on purpose. |
| Reload config | Re-read `appsettings.json`. |
| Show devices... | List active playback devices, their form factor, and whether they are muted on loss. |
| Start with Windows | Toggle autostart for the current user. |
| Exit | Quit and restore the mute state. |

## Configuration (`appsettings.json`)

```json
{
  "MuteDeviceNames": [],
  "NeverMuteDeviceNames": [],
  "RestoreDefaultOnReconnect": true
}
```

- `MuteDeviceNames`: device names (substring, case-insensitive) that are always muted on loss, whatever their form factor.
- `NeverMuteDeviceNames`: names that are never muted. Use this if a headset reports itself as "Speakers" (check **Show devices...**).
- `RestoreDefaultOnReconnect`: set the guarded device as default again when it returns.

## Limitations

- If the app crashes while the speakers are muted, they stay muted until you restart it and the guarded device is back (or you unmute manually).
- Setting the default device uses the undocumented `IPolicyConfig` COM interface. A future Windows update could break that step. The muting itself uses documented APIs and keeps working.
- There is a few-millisecond gap between the disconnect and the mute while Windows reroutes.

## Build from source

Requires the .NET 10 SDK.

```powershell
dotnet test SoundGuard.sln
./scripts/publish.ps1        # tests, then builds both zips into ./dist
```

`scripts/publish.ps1` produces `SoundGuard-self-contained.zip` and `SoundGuard-framework-dependent.zip`.

## Releases

Pushing a tag like `v1.0.0` runs the GitHub Actions workflow (`.github/workflows/build.yml`), which tests, builds both zips and attaches them to a GitHub release. Pushes to `main` and pull requests build and upload the zips as workflow artifacts.

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## Layout

- `scripts/publish.ps1`, `.github/workflows/build.yml`: packaging and CI.
- `src/SoundGuard/GuardLogic.cs`: decision logic (idempotent `Evaluate`).
- `src/SoundGuard/AudioSupervisor.cs`: Core Audio notification listener.
- `src/SoundGuard/SpeakerMuter.cs`, `DeviceClassifier.cs`: muting and form-factor classification.
- `src/SoundGuard/PolicyConfigDefaultDeviceSetter.cs`: default-device switching.
- `tests/SoundGuard.Tests`: unit tests for the logic.
