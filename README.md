# DLSS Feeder Manager

A Windows tool for applying, validating, and removing game-specific DLSS Feeder setups.

> [!IMPORTANT]
> This project is in early development. No usable release is available yet.

## Scope

The manager is intended to:

- validate the selected game directory;
- use files supplied by the user;
- remember previously selected source paths;
- back up files before changing the game directory;
- install and validate a supported game profile;
- remove the setup and restore the backup;
- report clear installation errors.

It will not bundle game files or third-party binaries. ReShade installation remains manual in the first release.

## Compatibility

| Game | Manual setup | Manager support |
| --- | --- | --- |
| Metal Gear Solid 4 | Confirmed | In progress |

Compatibility is profile-based. A game is only marked as supported after its profile has been tested.

See the [roadmap](docs/ROADMAP.md) for the current plan.

## Status

The first milestone is a testable Windows x64 executable for Metal Gear Solid 4.

## Disclaimer

This is an independent community project. It is not affiliated with or endorsed by NVIDIA, ReShade, RenoDX, Konami, or the developers and publishers of supported games.
