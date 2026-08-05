# Plugin Release and Repository Migration

This document is the operational entry point for the plugin monorepo. The source code, SDK, tooling, tests, and release workflow now live in the main `UniversalDeviceToolkit` repository.

## Canonical Layout

| Concern | Canonical location |
| --- | --- |
| Official plugin source | `Plugins/Official/<Plugin>/` |
| SDK and host contracts | `Plugins/SDK/` |
| Shared plugin code | `Plugins/Shared/` |
| CLI and workbench | `Plugins/Tooling/` |
| Host compatibility manifest | `Plugins/HostBaseline/host-release.json` |
| Downloaded host binaries | `Plugins/.host/<host-version>/` (ignored) |
| Generated packages and catalog | `Plugins/.build/` (ignored) |
| Author and maintainer docs | `Docs/Plugins/` |
| Plugin CI and release workflows | `.github/workflows/plugins-*.yml` |

`UniversalDeviceToolkit-Plugins` is no longer a source location for new plugin work. Its history is preserved in the main repository; its remote repository is retained only for the retirement notice and the legacy-client transition window.

## GitHub Releases

The Releases page has two deliberately separate roles:

1. Versioned application releases use tags such as `v5.0.0` and contain host installers, application archives, and checksums.
2. The fixed `plugin-catalog` release is a managed rolling release with `latest=false`. It contains only:
   - `store.json`, the single catalog asset consumed by the application;
   - one package ZIP per published plugin, named `<plugin-id>-v<version>.zip`.

The application reads the catalog from:

`https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/store.json`

Do not publish plugin packages as separate release tags, upload generated logs or build folders, or mark `plugin-catalog` as the latest application release. The release workflow uploads package ZIPs first, publishes `store.json` last, and removes stale versions for the selected plugin. If catalog publication fails, it restores the previous catalog before failing the job.

## Publish Sequence

1. Update `Plugins/Official/<Plugin>/plugin.manifest.json` and that plugin's changelog.
2. Run `udt-plugin.cmd validate` and the relevant tests.
3. Build and package the selected plugin ZIPs.
4. Generate `Plugins/.build/catalog/store.json` with `generate-store --merge-existing --require-assets`.
5. Run the staged ZIP/hash/content checks.
6. Upload package assets, then upload `store.json` as the last catalog mutation.
7. Verify that the rolling release is public, not latest, contains exactly one `store.json`, and has no stale package for the updated plugin.

## Legacy Client Upgrade

The catalog release must never be used as an application update. The main application's update checker excludes the `plugin-catalog` tag, so an old client can only upgrade through a normal versioned application release.

For a legacy client whose update feed still points at the old `LenovoLegionToolkit` repository, use this one-time bridge sequence:

1. Publish the new main application release under the main repository's normal `vX.Y.Z` tag, including the compatibility installer asset name expected by the legacy updater (`LenovoLegionToolkit_vX.Y.Z_Setup.exe`) and its SHA256 file.
2. Publish one final compatibility release in the legacy application repository, using the same installer payload and an explicit note directing users to `UniversalDeviceToolkit`. Do not publish plugin packages there.
3. Test the upgrade from a clean legacy installation, including install path, settings migration, first launch, plugin discovery, and rollback on a failed download.
4. After the transition window, archive the legacy application/plugin repositories and leave a permanent README redirect to the main repository. Do not delete historical releases needed by existing clients.
5. The upgraded client then reads `plugin-catalog/store.json` from the main repository and installs packages from the same rolling release.

This keeps the old client useful as the transport into the new host while keeping application releases and plugin assets visually separate on GitHub.

## Retirement Checklist

- [ ] Main repository contains the complete plugin history and current source.
- [ ] `store.json` and all package ZIPs are published from the fixed `plugin-catalog` release.
- [ ] Old plugin repository README points to `Plugins/Official` in the main repository.
- [ ] Legacy application bridge release has been tested from a clean install.
- [ ] Existing plugin installations load or migrate without a host DLL bundled in new ZIPs.
- [ ] Old repository is archived only after the agreed client upgrade window.
