# Component sources

The manager uses official upstream sources and never rehosts restricted third-party files.

| Component | Upstream | Handling |
| --- | --- | --- |
| DLSS5-Feeder | [jlrouzies-fr/DLSS5-Feeder](https://github.com/jlrouzies-fr/DLSS5-Feeder) | Download the pinned release from GitHub and verify its SHA-256. |
| iMMERSE LaunchPad | [martymcmodding/iMMERSE](https://github.com/martymcmodding/iMMERSE) | Do not bundle or rehost. Open the official repository and let the user select the downloaded ZIP or folder. |
| RenoDX DLSS 5 add-on | [RankFTW/RHI](https://github.com/RankFTW/RHI) or the RenoDX distribution | Do not bundle. Let the user select the local add-on and preserve its filename. |
| NVIDIA DLSS runtimes | Existing game installation, RHI, or [DLSS Swapper](https://github.com/beeradmoore/dlss-swapper) | Do not bundle. Let the user select local copies of `nvngx_dlss.dll` and `nvngx_dlssnr.dll`. |
| ReShade | [reshade.me](https://reshade.me) | Keep installation manual and verify the existing installation. |

## Source registry

Each downloadable source handled by the manager must define:

- upstream repository and release;
- expected asset name;
- pinned version;
- SHA-256;
- destination relative to the selected game executable;
- acquisition mode: automatic, guided, or local selection.

Automatic downloads are limited to files whose upstream distribution permits it. Guided acquisition opens the official source but leaves the download and selection to the user.

## iMMERSE

The iMMERSE license forbids independent redistribution. The required files must remain sourced from the original repository:

- `Shaders/MartysMods_LAUNCHPAD.fx`;
- the complete `Shaders/MartysMods` folder;
- `Textures/iMMERSE_bluenoise_opt.png`.

The manager must not copy these files into its own repository or release package.

## Version changes

A newer upstream release is not automatically considered compatible. Verified profiles keep their tested component versions until the newer combination passes installation and runtime-log validation.
