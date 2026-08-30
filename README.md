# DLSS Feeder Manager

A Windows manager for applying, validating, and removing DLSS5-Feeder setups.

> [!IMPORTANT]
> This project is in early development. No usable release is available yet.

## Scope

The core is game-agnostic. It targets 64-bit Direct3D 11 and Direct3D 12 games that expose a usable ReShade depth buffer and iMMERSE LaunchPad motion vectors.

The manager will:

- validate the selected game executable and directory;
- support verified game profiles and an experimental generic mode;
- obtain DLSS5-Feeder from its official release;
- use external components supplied by the user;
- preserve selected filenames;
- remember source paths;
- back up files before changing the game directory;
- install and validate the setup;
- remove it and restore the backup;
- report clear installation errors.

ReShade installation remains manual in the first release. Game files, RenoDX, NVIDIA runtimes, and iMMERSE are not bundled.

See the [component acquisition policy](docs/SOURCES.md) for upstream repositories and download rules.

## Requirements

- Windows;
- 64-bit game;
- Direct3D 11 or Direct3D 12;
- ReShade 6.8 or newer with add-on support;
- working ReShade depth buffer;
- iMMERSE LaunchPad motion vectors.

DirectX 9, DirectX 10, Vulkan, and 32-bit games are outside the supported scope of DLSS5-Feeder.

## Compatibility

| Game | Manual setup | Manager profile |
| --- | --- | --- |
| Metal Gear Solid 4 | Confirmed | In progress |

Unlisted games can use the generic mode, but remain experimental until their complete flow is tested.

See the [roadmap](docs/ROADMAP.md) for the current plan.

## Status

The first milestone is a generic Windows x64 manager with Metal Gear Solid 4 as its first verified profile.

## License

MIT. See [LICENSE](LICENSE).

## Disclaimer

This is an independent community project. It is not affiliated with or endorsed by NVIDIA, ReShade, RenoDX, Konami, or the developers and publishers of supported games.
