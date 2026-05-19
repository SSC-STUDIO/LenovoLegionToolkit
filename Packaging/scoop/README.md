# Scoop Bucket Guide

This folder documents the maintainer workflow for updating the SSC-STUDIO Scoop bucket package for `lenovolegiontoolkit`.

The authoritative Scoop manifest is published in the custom bucket repository: [SSC-STUDIO/scoop-bucket](https://github.com/SSC-STUDIO/scoop-bucket).

## Release Checklist

1. Publish a stable GitHub release with:
   - `LenovoLegionToolkit_vX.Y.Z_Setup.exe`
   - `LenovoLegionToolkit_vX.Y.Z_SHA256.txt`
2. Confirm the installer URL is the final GitHub Release asset URL.
3. Update the `lenovolegiontoolkit.json` manifest in `SSC-STUDIO/scoop-bucket`:
   - `version`
   - `url`
   - `hash`
   - any release notes or homepage metadata if needed
4. Validate locally with Scoop on a clean machine:
   ```powershell
   scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket
   scoop install ssc-studio/lenovolegiontoolkit
   scoop update ssc-studio/lenovolegiontoolkit
   scoop uninstall lenovolegiontoolkit
   ```
5. Push the manifest update to `SSC-STUDIO/scoop-bucket`.

## Notes

- Use the GitHub Release installer, not local build output.
- Do not invent or prefill the hash before the release asset exists.
- Keep the bucket README, project README, and release notes aligned so users see the same download and runtime guidance everywhere.
