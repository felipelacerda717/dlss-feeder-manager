# Compatibility expansion

The current manager remains on the tested x64 D3D11/D3D12 installation path. Existing game profiles stay pinned to their validated DLSS5-Feeder releases.

## Upstream evaluation

DLSS5-Feeder v0.6.0-beta.1 adds a substantially different setup: selectable motion-vector providers, LumeniteFX Kernel as the recommended provider, validation masks, 32-bit transport through a 64-bit host, D3D9 through dgVoodoo2, and Vulkan interop with a fallback layer.

The manager must not replace the pinned files automatically. Each path needs its own manifest, preflight rules, installed-file list, validation markers, and removal test.

## 64-bit D3D11/D3D12 upgrade

- [ ] Add a separate v0.6 beta source entry with pinned hashes.
- [ ] Support LumeniteFX as a user-supplied motion-vector package.
- [ ] Let the profile select the motion-vector provider and required ReShade definition.
- [ ] Validate the provider name, enabled state, compile state, and non-zero MV probe.
- [ ] Retest MGS4 and Death of the Outsider before changing their pinned releases.

## 32-bit path

- [ ] Detect executable architecture before selecting a layout.
- [ ] Install `dlss5-feed.addon32` beside the game executable.
- [ ] Create `host64` and install `dlss5-feed-host64.exe` there.
- [ ] Require separate user-supplied x64 ReShade, RenoDX, and NVIDIA runtime files for the host.
- [ ] Validate both game-side and host-side logs.
- [ ] Stop the host before removal and restore both layouts atomically.

## D3D9 path

- [ ] Detect D3D9 from `ReShade.log`; executable imports alone are not definitive.
- [ ] Require a user-supplied dgVoodoo2 package and preserve its license boundary.
- [ ] Select the correct x86 or x64 `D3D9.dll`.
- [ ] Validate `DisableAndPassThru=false`, a safe VRAM value, and D3D11 output.
- [ ] Require ReShade as `dxgi.dll`, never `d3d9.dll`.
- [ ] Confirm the dgVoodoo watermark and D3D11 runtime before installing feeder files.
- [ ] Apply the matching 32-bit or 64-bit feeder layout only after translation is confirmed.

## Vulkan path

- [ ] Detect Vulkan from `ReShade.log` and verify the global ReShade layer is enabled for the game.
- [ ] Set or validate `AddonPath=.\\` in the game's `ReShade.ini`.
- [ ] Install the normal x64 feeder and user-supplied components beside the executable.
- [ ] Validate the Vulkan interop-extension diagnostics.
- [ ] Offer `feed-vk-layer.zip` only when the in-process hook reports missing interop entry points.
- [ ] Treat the fallback launcher and layer files as a separate reversible installation.

## Release rule

No new API path is marked supported until installation, runtime validation, removal, restoration, and immediate reinstallation pass on at least one recorded game.
