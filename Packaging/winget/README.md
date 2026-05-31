# Winget Submission Guide

This folder keeps the maintainer-side winget manifest draft for `SSC-STUDIO.LenovoLegionToolkit`. The canonical submission target remains the upstream [`microsoft/winget-pkgs`](https://github.com/microsoft/winget-pkgs) repository.

The public product name is Universal Device Toolkit. The winget package identity intentionally remains under the old Lenovo Legion Toolkit identifier for now so existing installs can upgrade in place.

The repo may contain a versioned draft folder such as `manifests/s/SSC-STUDIO/LenovoLegionToolkit/4.1.0` before release assets exist. Those files are preparation only and are not submission-ready until the final GitHub Release URL, release date, and SHA256 are filled in from the published release.

## Package Identity

- PackageIdentifier: `SSC-STUDIO.LenovoLegionToolkit`
- PackageName: `Universal Device Toolkit`
- Previous public name: `Lenovo Legion Toolkit`
- Publisher: `SSC-STUDIO`
- License: `GPL-3.0`
- Installer type: `inno`
- Architecture: `x64`

## Release Checklist

1. Create a stable GitHub release with Full and Online Universal Device Toolkit assets plus the winget compatibility installer alias:
   `UniversalDeviceToolkit_vX.Y.Z_Full_Setup.exe`
   `UniversalDeviceToolkit_vX.Y.Z_Online_Setup.exe`
   `LenovoLegionToolkit_vX.Y.Z_Setup.exe`
2. Confirm the release also includes the checksum manifest:
   `UniversalDeviceToolkit_vX.Y.Z_SHA256.txt`
3. Do not create or submit a new version manifest until the final release asset URL and SHA256 are available.
4. Copy the installer SHA256 from the release checksum file into the winget installer manifest.
   A helper script can populate the versioned winget folder and the Scoop draft manifest from the final release metadata:
   ```powershell
   .\Packaging\Prepare-PackageManifests.ps1 -Version X.Y.Z -ReleaseDate YYYY-MM-DD -InstallerSha256 <SHA256>
   ```
5. Validate the manifest locally:
   ```powershell
   winget validate manifests\s\SSC-STUDIO\LenovoLegionToolkit\X.Y.Z
   ```
6. Test install and uninstall from the manifest on a clean Windows machine:
   ```powershell
   winget install --manifest manifests\s\SSC-STUDIO\LenovoLegionToolkit\X.Y.Z
   winget uninstall SSC-STUDIO.LenovoLegionToolkit
   ```
7. Submit the manifest folder to `microsoft/winget-pkgs`.

## Notes

- Use the GitHub Release installer, not local build output.
- Do not point winget at mirror URLs. Mirrors can be listed in community posts, but winget should use the authoritative GitHub Release URL.
- Keep the PackageIdentifier stable after acceptance. Do not rename it just for the Universal Device Toolkit branding change.
- If winget review requires the installer publisher to match more closely, update `MakeInstaller.iss` and the manifest together in a normal release commit.
