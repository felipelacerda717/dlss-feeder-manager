# Component sources

The manager keeps restricted third-party components user-supplied. The only third-party runtime files bundled by the manager are the two known-good DLSS5-Feeder files that were validated with the current LaunchPad-based install path. DLSS5-Feeder is MIT-licensed and its license notice is included with the embedded payload.

| Component | Upstream | Handling |
| --- | --- | --- |
| DLSS5-Feeder | [jlrouzies-fr/DLSS5-Feeder](https://github.com/jlrouzies-fr/DLSS5-Feeder) | A known-good `dlss5-feed.addon64` + `DLSS5_Feed.fx` pair is embedded in `DLSSFeederManager.exe`. The manager extracts it locally and verifies SHA-256; it does not fetch these files from GitHub during installation. |
| iMMERSE LaunchPad | [martymcmodding/iMMERSE](https://github.com/martymcmodding/iMMERSE) | Do not bundle or rehost. Let the user select the official downloaded ZIP or folder. |
| RenoDX DLSS 5 add-on | [RankFTW/RHI](https://github.com/RankFTW/RHI/releases) or the RenoDX distribution | Do not bundle. Let the user select the local add-on and preserve its filename. |
| NVIDIA DLSS runtimes | Existing game installation, RHI, or [DLSS Swapper](https://github.com/beeradmoore/dlss-swapper) | Do not bundle. Let the user select local copies of `nvngx_dlss.dll` and `nvngx_dlssnr.dll`. |
| ReShade | [reshade.me](https://reshade.me) | Keep installation manual and verify the existing installation. |

## Embedded DLSS5-Feeder pin

The current x64 D3D11/D3D12 manager path is pinned to the exact pair that was used in the working MGS4 setup:

- `dlss5-feed.addon64` — SHA-256 `6ea59b3237ed9f1e2bdc6e258518347ccb7e03dfdc2f96fc08addc8974527dad`;
- `DLSS5_Feed.fx` — SHA-256 `2dca9659c9e44ab05d29b2ebf20c5d6414c6de8b57f506bf4d52fa9c556a8943`.

The shader is the LaunchPad-specific version: it reads iMMERSE LaunchPad motion vectors and must run below `MartysMods_Launchpad` in the ReShade technique order.

The add-on binary is stored inside the application as a compressed embedded payload. On install, the manager reconstructs it into its LocalAppData cache, verifies the exact SHA-256 above, then copies it to the game. The shader follows the same hash-verification path. There is no runtime HTTP dependency for these two files.

Because these files are embedded resources, installing a newer `DLSSFeederManager.exe` automatically brings the feeder pair shipped with that manager version. Updating the manager does not silently rewrite an existing game installation; the new embedded pair is used on the next managed install/reinstall.

The upstream DLSS5-Feeder MIT license is stored at `src/DLSSFeederManager/Assets/EmbeddedFeeder/DLSS5-Feeder-LICENSE.txt` and embedded into the application alongside the payload.

## iMMERSE

The iMMERSE license forbids independent redistribution. The required files must remain sourced from the original repository:

- `Shaders/MartysMods_LAUNCHPAD.fx`;
- the complete `Shaders/MartysMods` folder;
- `Textures/iMMERSE_bluenoise_opt.png`.

After using Code > Download ZIP and extracting it, the LaunchPad shader is usually at `iMMERSE-main/Shaders/MartysMods_LAUNCHPAD.fx`. The manager accepts either the downloaded ZIP or that shader file and locates the accompanying folder and texture from the same extraction.

The manager must not copy these files into its own repository or release package.

## Version changes

A newer upstream DLSS5-Feeder release is not automatically considered compatible. The embedded pin changes only after the replacement pair passes the same install and runtime validation expected from a supported manager path.
