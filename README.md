# DLSS Feeder Manager

A portable Windows manager for installing, validating, and safely removing [DLSS5-Feeder](https://github.com/jlrouzies-fr/DLSS5-Feeder) setups.

> [!IMPORTANT]
> This project is an early alpha. Use it only in single-player games and keep backups of saves and modified game files.

## What it does

- checks the selected game executable and ReShade installation;
- supports verified game profiles and an experimental generic mode;
- carries a known-good DLSS5-Feeder pair inside the application and verifies its SHA-256 before installation;
- installs user-supplied iMMERSE, RenoDX, and NVIDIA runtime files;
- preserves selected filenames, including names such as `renodx-dlss5 (1).addon64`;
- backs up every overwritten file;
- validates the installed layout and runtime logs;
- removes managed files and restores verified backups.

The current manager build installs only the tested 64-bit Direct3D 11 and Direct3D 12 path.

| Graphics API | Current manager |
| --- | --- |
| Direct3D 11 | Supported |
| Direct3D 12 | Supported |
| Native Direct3D 9 | Not supported |
| Direct3D 10 | Not supported |
| Vulkan | Not supported |

The latest upstream DLSS5-Feeder has separate beta paths for D3D9 through dgVoodoo2, Vulkan, and 32-bit games. This manager does not configure those newer paths yet, so their presence upstream must not be interpreted as support here.

The upstream v0.6 beta and the required per-API installation designs are tracked in [compatibility expansion](docs/COMPATIBILITY_EXPANSION.md). The current x64 manager path intentionally stays on the exact LaunchPad-based pair already confirmed working with MGS4 until a newer combination passes the same validation.

## Before you start

- [ ] Windows 10 or 11
- [ ] NVIDIA RTX 50 Series GPU
- [ ] 64-bit Direct3D 11 or Direct3D 12 game
- [ ] Single-player game without active anti-cheat
- [ ] ReShade 6.8 or newer with add-on support
- [ ] Working ReShade depth buffer
- [ ] MSAA and SSAA disabled in the game

Do not use ReShade add-ons in online or anti-cheat protected games unless the game explicitly permits them.

## Required components

| Component | Where to get it | What to select in the manager |
| --- | --- | --- |
| ReShade | [reshade.me](https://reshade.me) | Install manually with add-on support. The manager checks for a ReShade proxy DLL and `reshade-shaders`. |
| DLSS5-Feeder | [Official repository](https://github.com/jlrouzies-fr/DLSS5-Feeder) | Nothing. `dlss5-feed.addon64` and `DLSS5_Feed.fx` are embedded in the manager and verified before use. |
| iMMERSE LaunchPad | [martymcmodding/iMMERSE](https://github.com/martymcmodding/iMMERSE) | The downloaded ZIP or `iMMERSE-main/Shaders/MartysMods_LAUNCHPAD.fx`. |
| RenoDX DLSS 5 add-on | [RHI releases](https://github.com/RankFTW/RHI/releases) | Your local `renodx-dlss5*.addon64`. Its filename is preserved. |
| DLSS Neural Rendering runtime | RHI | `nvngx_dlssnr.dll` |
| DLSS Super Resolution runtime | An existing DLSS game or [DLSS Swapper](https://github.com/beeradmoore/dlss-swapper) | `nvngx_dlss.dll` |

The embedded DLSS5-Feeder pair is MIT-licensed. iMMERSE, RenoDX, NVIDIA runtimes, ReShade, and game files are not bundled. See [component sources and redistribution rules](docs/SOURCES.md).

## Embedded known-good Feeder pair

The application carries these exact files:

- `dlss5-feed.addon64` — SHA-256 `6ea59b3237ed9f1e2bdc6e258518347ccb7e03dfdc2f96fc08addc8974527dad`;
- `DLSS5_Feed.fx` — SHA-256 `2dca9659c9e44ab05d29b2ebf20c5d6414c6de8b57f506bf4d52fa9c556a8943`.

They are the pair used by the current LaunchPad-based path and confirmed in the working MGS4 setup. The installer no longer depends on an upstream GitHub release URL for these two files. It reconstructs/extracts them from the application, verifies the hashes above, and only then installs them.

## Install ReShade

- [ ] Close the game.
- [ ] Run the ReShade installer and select the game's real executable.
- [ ] Select Direct3D 10/11/12.
- [ ] Use a ReShade build with add-on support and enable loading of add-ons.
- [ ] Confirm that the ReShade proxy DLL is next to the game executable.
- [ ] Confirm that the `reshade-shaders` folder exists.
- [ ] Launch the game once and verify that the ReShade overlay opens with Home.

## Use the manager

- [ ] Open `DLSSFeederManager.exe`.
- [ ] Select the game's executable.
- [ ] Select the RenoDX DLSS 5 `.addon64` file.
- [ ] Select `nvngx_dlss.dll`.
- [ ] Select `nvngx_dlssnr.dll`.
- [ ] Select the iMMERSE ZIP or `MartysMods_LAUNCHPAD.fx`.
- [ ] Click Check and resolve every reported requirement.
- [ ] Close the game if it is running.
- [ ] Click Install.

The manager stores settings and the extracted/verified feeder cache in `%LocalAppData%/DLSS Feeder Manager`. Backups and the installation manifest are stored in `<game>/.dlss-feeder-manager`.

## Enable it in ReShade

- [ ] Launch the game and press Home.
- [ ] On Home, enable `iMMERSE: Launchpad`.
- [ ] Move `iMMERSE: Launchpad` above `DLSS 5 Feed`.
- [ ] Enable `DLSS 5 Feed`.
- [ ] On Add-ons, enable `DLSS 5 Feed`.
- [ ] Enable `DLSS 5 Neural Rendering`.
- [ ] In its panel, enable `Enable DLSS Neural Rendering`.
- [ ] Enable `Enable Upscaling`.
- [ ] Enter gameplay for several frames.
- [ ] Return to the manager and click Validate.

The confirmed ReShade layout and screenshots are in the [MGS4 setup guide](docs/games/mgs4.md#reshade-settings).

## Remove and restore

- [ ] Close the game.
- [ ] Select the same game executable in the manager.
- [ ] Click Remove and confirm.
- [ ] Check that the original files were restored.

New installations record hashes for managed files and backups. Removal stops before changing anything if a managed file was modified after installation or a backup fails verification.

## Permissions and safety

- The application runs as the current Windows user and does not request administrator privileges.
- Game changes are limited to the directory containing the selected executable.
- Settings and the extracted feeder cache are limited to the application's LocalAppData directory.
- Embedded DLSS5-Feeder files are accepted only after SHA-256 verification.
- Do not grant `Everyone` full control over a game directory. If Windows denies access, fix the specific library or game-folder permission instead.

## Compatibility

| Game | Result | Manager status |
| --- | --- | --- |
| Metal Gear Solid 4 | Runtime, removal, and reinstallation reported working | Profile pinned to the embedded known-good pair |
| Dishonored: Death of the Outsider | D3D11 runtime, removal, and reinstallation reported working | Profile targets `Dishonored_DO.exe`; revalidation required after the embedded pin |

Unlisted compatible games can use the generic mode. A game is marked verified only after installation, runtime-log validation, removal, and backup restoration all pass.

## Troubleshooting

### This game already has a managed installation

Close the game and click Remove again. Older builds could restore the original files while leaving `<game>/.dlss-feeder-manager/install.json` open, which prevented the state directory from being deleted.

The corrected build closes the manifest before cleanup and clears `install.json` separately. If Windows still blocks cleanup, keep the backup folder and rename `.dlss-feeder-manager` to `.dlss-feeder-manager.old` only after confirming that the original game files were restored.

### Access denied

Close the game, its launcher, ReShade tools, and any Explorer preview using the directory. Fix access only for the affected game library or folder. Do not grant `Everyone` full control and do not configure the manager to run permanently as administrator.

If the denied path is `System Volume Information` or `Application Data`, keep the complete iMMERSE package in a normal folder such as Downloads and select its untouched ZIP. Do not place `MartysMods_LAUNCHPAD.fx` directly in a drive root or user-profile root. The manager does not need access to protected Windows folders.

### iMMERSE package is incomplete

Select the untouched ZIP downloaded from the official iMMERSE repository, or keep the extracted package structure unchanged. `MartysMods_LAUNCHPAD.fx` must remain inside `Shaders`, alongside `Shaders/MartysMods`, with `Textures/iMMERSE_bluenoise_opt.png` in the same package root.

### Validation is pending

Validation does not configure ReShade. Confirm that `iMMERSE: Launchpad` and `DLSS 5 Feed` appear on Home, keep Launchpad above DLSS 5 Feed, enable the required add-ons and options, and enter gameplay for several frames. Close the game before clicking Validate. File presence alone does not confirm that neural rendering ran.

If `DLSS 5 Feed` does not appear on Home, confirm that ReShade Effect Search Paths includes `.\\reshade-shaders\\Shaders\\**`, click Reload, and inspect the ReShade Log tab for shader errors. Preserve `ReShade.log` and `dlss5-feed.log` when requesting support.

## Updating

- [ ] Close the game before updating the manager.
- [ ] Click **Check for updates** in the lower-right corner.
- [ ] Read the release notes and confirm the update.
- [ ] Wait while the new executable and its SHA-256 file are downloaded from the official GitHub Release.
- [ ] Allow the manager to restart.

The updater is manual and never installs silently. It verifies the release SHA-256 before closing the current version, keeps the previous executable during the first launch, and restores it if the new build cannot finish starting.

The Feeder pair is embedded inside `DLSSFeederManager.exe`, so updating the manager automatically brings the exact Feeder payload shipped with that version and the installer points directly to that embedded payload. Updating the manager does **not** silently rewrite an already-installed game; the new embedded files are applied on the next managed install/reinstall.

If the executable is in a folder your Windows account cannot modify, move it to a user-owned folder before updating. The manager does not request administrator rights.

## Project status

- [x] Generic Windows x64 installation core
- [x] Known-good DLSS5-Feeder pair embedded and SHA-256 verified
- [x] No runtime upstream download dependency for the pinned Feeder files
- [x] Backup, rollback, validation, removal, and restoration
- [x] MGS4 data-driven profile
- [x] Death of the Outsider profile targeting `Dishonored_DO.exe`
- [x] Verified, user-confirmed portable updater
- [x] Complete removal and immediate reinstallation tests on both recorded games
- [ ] Complete MGS4 manager test for the current build
- [ ] Improve guided onboarding and diagnostics
- [ ] Publish the next versioned GitHub Release

See the [v0.1.0 checklist](https://github.com/felipelacerda717/dlss-feeder-manager/issues/1), [v0.2.0 checklist](https://github.com/felipelacerda717/dlss-feeder-manager/issues/5), [maintainer safety checklist](docs/MAINTAINING.md), and [roadmap](docs/ROADMAP.md).

## License

MIT. See [LICENSE](LICENSE). The embedded DLSS5-Feeder files retain the upstream MIT notice documented in [component sources](docs/SOURCES.md).

## Disclaimer

This is an independent community project. It is not affiliated with or endorsed by NVIDIA, ReShade, RenoDX, Konami, Arkane Studios, Bethesda, or the developers and publishers of supported games.
