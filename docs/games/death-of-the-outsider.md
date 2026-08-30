# Dishonored: Death of the Outsider

## Status

The manager installation and D3D11 runtime were reported working on August 30, 2026. Removal must be retested with the stale-state fix before the profile is marked fully verified.

- Executable: `Dishonored_DO.exe`
- Architecture: 64-bit
- Graphics API tested: Direct3D 11
- Profile status: runtime confirmed, removal pending
- Feeder version: v0.2.0

The executable name matches the standard Steam launch configuration. Other store builds must use the same executable name to load this profile; otherwise the manager uses generic experimental mode.

## Test checklist

- [x] Select the game executable
- [x] Pass manager preflight
- [x] Install the required files
- [x] Enable LaunchPad, DLSS 5 Feed, and Neural Rendering
- [x] Confirm neural rendering in gameplay
- [ ] Save the manager validation result and runtime markers
- [ ] Remove the managed installation with the corrected build
- [ ] Confirm that original files were restored
- [ ] Reinstall immediately after removal

## Sources

- [Bethesda system requirements](https://help.bethesda.net/app/answers/detail/a_id/39704/)
- [Steam launch configuration](https://steamdb.info/app/614570/config/)
- [DLSS5-Feeder v0.2.0](https://github.com/jlrouzies-fr/DLSS5-Feeder/releases/tag/v0.2.0)
