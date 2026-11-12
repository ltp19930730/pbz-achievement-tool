# PBZ Achievement Tool

[简体中文](docs/README.zh-CN.md) · [繁體中文](docs/README.zh-TW.md)

A small Windows desktop utility for **Phantom Blade Zero** Steam achievements (App ID `4115450`). English, Simplified Chinese and Traditional Chinese interfaces are included in one executable.

![Windows interface](docs/preview.png)

## Features

- Read all 66 achievements and show the connected Steam account.
- Unlock checked achievements or unlock all remaining achievements with one click.
- Wait for Steam's successful `UserStatsStored_t` callback, then read back each submitted achievement.
- No save editing, password collection, telemetry, or bundled game binaries.

## Run

1. Download the Windows x64 executable from Releases, or build it below.
2. Start Steam and sign into an account with access to the game.
3. Run `PBZAchievementTool.exe` normally, without administrator privileges.
4. Select **Browse**, then choose `steam_api64.dll` from your Phantom Blade Zero installation. For example: `PBZ/steam_api64.dll` or `PBZ/platform/steam/steam_api64.dll`. Only select a library from your trusted game installation: this loads native code.
5. Click **Connect**, check the displayed Steam account, then use **Unlock selected** or **Unlock all**.

The game itself does not need to be running. Unlock buttons change the connected account immediately; the tool does not provide achievement reset. Already unlocked entries are skipped. If Steam does not confirm the save, the application reports an error rather than claiming success. After a timeout, reconnect before retrying.

The interface language selector translates the application controls. Achievement names come from Steam and follow Steam's game language, which may differ from the interface language.

## Build

Requires Windows x64 and .NET Framework 4.5 or newer (4.8 recommended). No NuGet packages are required.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output: `dist/PBZAchievementTool.exe`. An optional GitHub Actions template is included at `docs/build-workflow.yml`; copy it to `.github/workflows/build.yml` to enable automated builds.

## Verification and limitations

The original single-achievement repair was verified with Steam's successful storage callback and readback. This desktop application's read-only integration check does not modify achievements. Bulk unlocking cannot be meaningfully retested on an already completed account without resetting achievements, which this project deliberately does not do.

Developer checks:

```powershell
# Read-only integration check; writes a local result file. Does not unlock anything.
.\dist\PBZAchievementTool.exe --check "C:\path\to\PBZ\steam_api64.dll" "$PWD\check.txt"
# Render the disconnected UI without contacting Steam.
.\dist\PBZAchievementTool.exe --preview "$PWD\preview.png"
```

Only App ID `4115450`, achievement IDs `ACH_001`–`ACH_066`, SteamUserStats v013 and SteamUser v023 are supported. Future game or Steam API changes may require updates. A locally set flag without a successful storage callback is not treated as verified completion. On submission failure, some achievements may already have been accepted; reconnect and inspect before retrying. The DLL selection is session-scoped; restart the tool to switch game libraries after connecting.

## License

MIT for this project's source. Steam and Phantom Blade Zero belong to their respective owners. This is an independent community utility, not an official product. Game DLLs are supplied by the user's own installation and are not redistributed.

