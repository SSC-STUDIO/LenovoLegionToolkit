# Scoop Bucket Guide

This folder documents the maintainer workflow for the SSC-STUDIO Scoop bucket package `universaldevicetoolkit`.

The authoritative manifest is published in [SSC-STUDIO/scoop-bucket](https://github.com/SSC-STUDIO/scoop-bucket). This repository keeps a draft only until a real 6.x release URL and SHA256 exist.

## Release Checklist

1. Publish a stable GitHub release with:
   - `UniversalDeviceToolkit_vX.Y.Z_Full_Setup.exe`
   - `UniversalDeviceToolkit_vX.Y.Z_Online_Setup.exe`
   - `UniversalDeviceToolkit_vX.Y.Z_Full_win-x64.zip`
   - `UniversalDeviceToolkit_vX.Y.Z_Online_win-x64.zip`
   - `UniversalDeviceToolkit_vX.Y.Z_SHA256.txt`
2. Generate the draft manifest from the final hashes:
   ```powershell
   .\Packaging\Prepare-PackageManifests.ps1 -Version X.Y.Z -ReleaseDate YYYY-MM-DD -HashManifestPath path\to\UniversalDeviceToolkit_vX.Y.Z_SHA256.txt -UpdatePublishedScoopManifest
   ```
3. Validate the repository copy:
   ```powershell
   .\Packaging\Test-PackageManifests.ps1 -Version X.Y.Z -HashManifestPath path\to\UniversalDeviceToolkit_vX.Y.Z_SHA256.txt
   ```
4. Validate locally with Scoop:
   ```powershell
   scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket
   scoop install ssc-studio/universaldevicetoolkit
   scoop update ssc-studio/universaldevicetoolkit
   scoop uninstall universaldevicetoolkit
   ```
5. Push the manifest update to `SSC-STUDIO/scoop-bucket`.

## Notes

- The manifest consumes the Full Electron ZIP, uses `innosetup: false`, and targets `UniversalDeviceToolkit.exe`.
- 6.x is a package-manager breaking change; the legacy package ID is not upgraded in place.
- Do not invent or prefill a SHA256 before the release asset exists.
