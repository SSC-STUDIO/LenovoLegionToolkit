# Scoop Bucket Guide

This folder documents the maintainer workflow for updating the SSC-STUDIO Scoop bucket package for `lenovolegiontoolkit`.

The authoritative Scoop manifest is published in the custom bucket repository: [SSC-STUDIO/scoop-bucket](https://github.com/SSC-STUDIO/scoop-bucket).

This repo may also contain a versioned draft manifest such as `lenovolegiontoolkit.4.1.0.draft.json` for release preparation. Draft manifests must not replace the published bucket manifest until the final GitHub Release URL and SHA256 are available.

## Release Checklist

1. Publish a stable GitHub release with:
   - `UniversalDeviceToolkit_vX.Y.Z_Full_Setup.exe`
   - `UniversalDeviceToolkit_vX.Y.Z_Online_Setup.exe`
   - `UniversalDeviceToolkit_vX.Y.Z_Setup.exe`
   - `UniversalDeviceToolkit_vX.Y.Z_SHA256.txt`
2. Confirm the installer URL is the final GitHub Release asset URL.
3. Update the `lenovolegiontoolkit.json` manifest in `SSC-STUDIO/scoop-bucket`:
   - `version`
   - `url`
   - `hash`
   - `homepage`, `checkver`, and `autoupdate` repository URLs during the Universal Device Toolkit transition
   - `shortcuts` so the executable is `Universal Device Toolkit.exe` and the shortcut name is `Universal Device Toolkit`
   - any release notes or homepage metadata if needed
   You can generate the repo draft manifest, and optionally refresh the published manifest copy in this repo, with:
   ```powershell
   .\Packaging\Prepare-PackageManifests.ps1 -Version X.Y.Z -ReleaseDate YYYY-MM-DD -InstallerSha256 <SHA256> -UpdatePublishedScoopManifest
   ```
4. Validate the repo copy against the release checksum manifest:
   ```powershell
   .\Packaging\Test-PackageManifests.ps1 -Version X.Y.Z -HashManifestPath path\to\UniversalDeviceToolkit_vX.Y.Z_SHA256.txt
   ```
5. Validate locally with Scoop on a clean machine:
   ```powershell
   scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket
   scoop install ssc-studio/lenovolegiontoolkit
   scoop update ssc-studio/lenovolegiontoolkit
   scoop uninstall lenovolegiontoolkit
   ```
6. Push the manifest update to `SSC-STUDIO/scoop-bucket`.

## Notes

- Use the GitHub Release installer, not local build output.
- Do not invent or prefill the hash before the release asset exists.
- Keep the bucket README, project README, and release notes aligned so users see the same download and runtime guidance everywhere.
