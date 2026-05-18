# Scoop Submission Guide

This folder documents the maintainer workflow for updating the community Scoop package for `lenovolegiontoolkit`.

The canonical submission target is the upstream [ScoopInstaller/Extras](https://github.com/ScoopInstaller/Extras) bucket. This repository does not publish the authoritative Scoop manifest directly.

## Release Checklist

1. Publish a stable GitHub release with:
   - `LenovoLegionToolkit_vX.Y.Z_Setup.exe`
   - `LenovoLegionToolkit_vX.Y.Z_SHA256.txt`
2. Confirm the installer URL is the final GitHub Release asset URL.
3. Update the `lenovolegiontoolkit.json` manifest in `ScoopInstaller/Extras`:
   - `version`
   - `url`
   - `hash`
   - any release notes or homepage metadata if needed
4. Validate locally with Scoop on a clean machine:
   ```powershell
   scoop bucket add extras
   scoop install extras/lenovolegiontoolkit
   scoop update extras/lenovolegiontoolkit
   scoop uninstall lenovolegiontoolkit
   ```
5. Submit the manifest update to `ScoopInstaller/Extras`.

## Notes

- Use the GitHub Release installer, not local build output.
- Do not invent or prefill the hash before the release asset exists.
- Scoop remains community maintained. Keep the README and release notes aligned so users see the same download and runtime guidance everywhere.
