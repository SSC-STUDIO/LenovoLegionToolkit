# Plugin Release and Repository Migration

This document is the operational entry point for the plugin monorepo. The source code, SDK, tooling, tests, and release workflow now live in the main `UniversalDeviceToolkit` repository.

As of 2026-08-05, the migration is live: the main repository's `v5.0.2` release is the application Latest release, `plugin-catalog` is the managed catalog release, and `UniversalDeviceToolkit-Plugins` is archived. The remaining checklist items below are validation gates, not instructions to recreate the retired repository layout.

## Migration Provenance

The migration boundary is recorded against these immutable source refs:

- Main repository before the history import: `a18b5c1e140ce7cab11b53d3a5f85c6a3ce2b6ce`.
- Legacy plugin repository source snapshot: `b5cb7df11a8a8f6fc448b635b85dd06bca6bb4b5`.
- History import commit: `f6266d0344af37f803cbab8740b27d00fffeaf16`.
- Canonical topology commit: `9d4633a636227343c0975d42efc02927c1fdff70`.

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

The Releases page has three deliberately separate roles:

1. Versioned application releases use tags such as `v5.0.2` or `v6.0.0-preview.1` and contain host installers, application archives, and checksums. A hyphen in the tag (`Release.yml`) marks a GitHub prerelease and skips winget.
2. The fixed `plugin-catalog` release is a managed rolling release with `latest=false` and **must not** be a GitHub prerelease. It contains only stable 1.x packages for **v5.0.2** clients:
   - `store.json`, the catalog asset consumed by stable hosts;
   - one package ZIP per published plugin, named `<plugin-id>-v<version>.zip`.
3. The fixed `plugin-catalog-preview` release is a managed rolling **prerelease** (`prerelease=true`, `latest=false`) for **v6.0.0-preview.N** hosts. Official 2.x packages (`2.0.0-preview.1`, `minHostVersion` 6.0.0) publish here only. Do not upload 2.x to `plugin-catalog`.

Stable hosts read:

`https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/store.json`

Preview hosts (InformationalVersion contains `-`, same rule as `Release.yml`) read:

`https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog-preview/store.json`

`IncludePrereleaseUpdates` in Settings only controls application updates. It does not switch the plugin catalog, so a v5.0.2 user cannot opt into 2.x.

Dispatch `plugins-release.yml` with `catalog_channel=stable` or `catalog_channel=preview`. `generate-store --catalog-channel` refuses to mix the two stores.

Do not publish plugin packages as separate release tags, upload generated logs or build folders, or mark either catalog tag as the latest application release. The release workflow uploads package ZIPs first, publishes `store.json` last, and removes stale versions for the selected plugin. If catalog publication fails, it restores the previous catalog before failing the job.

Vendored plugin compile baseline (`Plugins/HostBaseline/host-release.json`) stays **5.0.2** until the first `v6.0.0-preview.1` application ZIP exists; then refresh the baseline in a follow-up change. Runtime `minHostVersion` 6.0.0 already blocks 5.x Hosts from loading 2.x plugins.

## Publish Sequence

1. Update `Plugins/Official/<Plugin>/plugin.manifest.json` and that plugin's changelog.
2. Run `udt-plugin.cmd validate` and the relevant tests.
3. Build and package the selected plugin ZIPs.
4. Generate `Plugins/.build/catalog/store.json` with `generate-store --merge-existing --require-assets`.
5. Run the staged ZIP/hash/content checks.
6. Upload package assets, then upload `store.json` as the last catalog mutation.
7. Verify that the rolling release is public, not latest, contains exactly one `store.json`, and has no stale package for the updated plugin.

## Legacy Client Upgrade

The catalog releases must never be used as an application update. The main application's update checker excludes both `plugin-catalog` and `plugin-catalog-preview`, so an old client can only upgrade through a normal versioned application release.

For a legacy client whose update feed still points at the old `LenovoLegionToolkit` repository, use this one-time bridge sequence:

1. Publish the new main application release under the main repository's normal `vX.Y.Z` tag, including the compatibility installer asset name expected by the legacy updater (`LenovoLegionToolkit_vX.Y.Z_Setup.exe`) and its SHA256 file.
2. Do not create a second legacy-application Release. The legacy repository route resolves to the main repository, and the same `vX.Y.Z` Release carries the `LenovoLegionToolkit_vX.Y.Z_Setup.exe` compatibility alias and the migration note. Do not publish plugin packages under a separate application tag.
3. Test the upgrade from a clean legacy installation, including install path, settings migration, first launch, plugin discovery, and rollback on a failed download.
4. The legacy plugin repository is now archived with its historical releases and assets preserved. Leave its README redirect in place; do not delete historical releases needed by existing clients.
5. The upgraded client then reads `plugin-catalog/store.json` from the main repository and installs packages from the same rolling release.

This keeps the old client useful as the transport into the new host while keeping application releases and plugin assets visually separate on GitHub.

## Retirement Checklist

- [x] Main repository contains the complete plugin history and current source.
- [x] `store.json` and all package ZIPs are published from the fixed `plugin-catalog` release.
- [x] Old plugin repository README points to `Plugins/Official` in the main repository.
- [x] Legacy plugin issues #57-#61 were moved to main-repository issues #169-#173 and labeled `area:plugins`.
- [x] Legacy application bridge release path has passed an isolated clean-install smoke test.
- [x] Existing plugin installations load or migrate without a host DLL bundled in new ZIPs.
- [x] Old plugin repository is archived only after the agreed client upgrade window.
