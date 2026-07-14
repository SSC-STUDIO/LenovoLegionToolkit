# Universal Device Toolkit v4.1.0 Draft Release Notes

> **Historical maintainer draft.** v4.1.0 has shipped. For current release notes see [CHANGELOG.md](../CHANGELOG.md) and [GitHub Releases](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases).

This file kept pre-release verification notes for the `4.1.0` release. The GitHub Release body is generated from `CHANGELOG.md` by the release workflow.

## Highlights

- Fixed God Mode preset management so create, rename, delete, and preset switching refresh both the visible picker and the stored state correctly.
- Fixed optimization-only plugins so installed local plugins can contribute System Optimization child actions even when they do not expose a standalone page.
- Expanded sensor coverage and fallback handling with better VRAM, GPU hot-spot, memory temperature, SSD temperature, voltage, and shared-memory GPU readings.
- Fixed dashboard loading timing so skeletons remain visible until the first real content refresh is ready, and removed the battery detailed average temperature field.
- Continued separating hardware-validation and smoke-only behavior out of the shipping app and into standalone tools.

## Detailed Changes

### Fixed

- God Mode preset management now persists and reloads state consistently after create, rename, delete, and preset switching operations.
- Duplicate preset names now resolve to unique names automatically instead of creating misleading no-op looking entries.
- Input dialogs used by preset management no longer depend on the unstable in-message-box input path.
- Plugin optimization extenders now scan installed local plugin directories, allowing optimization-only plugins to surface their child actions correctly.
- Dashboard loading skeletons no longer disappear before `Power`, `Graphics`, and sensor cards finish their first refresh.
- The battery detailed average temperature field has been removed from the dashboard card.
- GPU hot-spot temperature and VRAM temperature are now treated as separate readings instead of being conflated.

### Improved

- Sensor fallback coverage now includes broader VRAM used/total/free aliases, iGPU shared-memory VRAM metrics, motherboard-backed memory temperatures, SSD temperatures, VRAM temperatures, GPU hot-spot temperature, CPU/GPU voltage, and improved fallback ordering for several GPU readings.
- Dashboard detailed sensor panels now expose more of the values the backend can already read, including memory usage, memory temperature, SSD temperature, VRAM usage, VRAM temperature, and GPU hot-spot temperature where available.
- Added a standalone `HardwareValidation` tool and elevated wrapper script for real God Mode hardware verification outside the shipping app entry path.
- Added a standalone `SensorInventoryDump` tool to capture the current machine's `LibreHardwareMonitor` inventory for targeted sensor support work.
- Prepared the `winget`/`scoop` manifest helper script for generating release-time package metadata from final published assets.

## Packaging Notes

- Repository version is set to `4.1.0`.
- Package manifests should be finalized after the `v4.1.0` tag workflow publishes the final GitHub Release assets:
  - `Packaging/winget/manifests/s/SSC-STUDIO/UniversalDeviceToolkit/4.1.0`
  - `Packaging/scoop/lenovolegiontoolkit.4.1.0.draft.json`
- Local release assets were generated on 2026-05-31 and local hashes were verified against `release-assets\UniversalDeviceToolkit_v4.1.0_SHA256.txt`.
- The release workflow will rebuild assets from the tagged commit and publish the final GitHub Release plus GitHub Pages resources. Use the workflow-generated compatibility installer SHA256 when finalizing winget/Scoop submissions.

## Validation Status

- `Make.bat 4.1.0` completed successfully in the current worktree.
- `winget validate Packaging\winget\manifests\s\SSC-STUDIO\UniversalDeviceToolkit\4.1.0` passed.
- Local release asset hashes match `release-assets\UniversalDeviceToolkit_v4.1.0_SHA256.txt`.
- Standalone hardware validation and preset UI CRUD validation passed through the elevated wrapper path in the current desktop session.
