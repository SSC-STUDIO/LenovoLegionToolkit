# Universal Device Toolkit Plugin Development Guide

This repository now supports two clear paths:

1. contributor path
2. official store path

## Contributor Path

Use this when you are developing a plugin locally, in a fork, or for an early PR.

### Standard Commands

```powershell
.\udt-plugin.cmd doctor
.\udt-plugin.cmd init --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"
.\udt-plugin.cmd dev --plugin my-plugin --theme system
.\udt-plugin.cmd build --plugin my-plugin
.\udt-plugin.cmd preview --plugin my-plugin --theme system
.\udt-plugin.cmd validate --plugin my-plugin --profile contributor
.\udt-plugin.cmd package --plugin my-plugin --build-first
```

`udt-plugin.cmd` is the canonical entry (`llt-plugin.cmd` is a compatibility alias). It publishes the tooling CLI into `Plugins/.build/tooling` and reuses that executable. This avoids repeated `dotnet run` builds and the file-lock failures that can happen when multiple validation commands start together.

**Published host baseline:** Universal Device Toolkit **v5.0.2** (`Plugins/HostBaseline/host-release.json`) until a v6 ZIP exists. Official 2.x plugins declare `minHostVersion: "6.0.0"` and publish to `plugin-catalog-preview`. Shipped 1.x packages remain on `plugin-catalog` for v5.0.2.

### Validation Profile

`contributor` checks:

- `plugin.manifest.json`
- synchronized runtime `plugin.json`
- project naming
- version alignment
- test project presence
- build output shape
- optional build/test execution

It does not require official store metadata.

## Official Store Path

Use this only when the plugin is intended to ship from the official repository.

### Additional File Contract

Each official plugin owns its store-facing metadata in:

```text
Plugins/<FolderName>/plugin.manifest.json
```

The `store` object contains:

- `description`
- `icon`
- `iconBackground`
- `tags`
- `dependencies`
- `supportedLanguages`
- optional `repositoryUrl`

Create or synchronize the initial store metadata with:

```powershell
.\udt-plugin.cmd promote --plugin my-plugin
```

`promote` also writes `store-entry.json` for compatibility with older release tooling.

### Validation Profiles

- `official-candidate`: plugin-local `store` metadata is required
- `official-release`: release/store alignment checks

Example:

```powershell
.\udt-plugin.cmd `
  validate `
  --plugin my-plugin `
  --profile official-candidate
```

## Plugin web UI (Electron)

Shipping UI is Electron. Declare `contributes.webPage` and implement `web/index.html` that calls `window.pluginHost.invoke` against Host `plugin.*` methods. Do not add WPF pages.

`udt-plugin.cmd dev` / `preview` historically opened PluginWorkbench; that host is retired. Use the Electron shell against a real Host.

## VS Code Extension Workflow Mapping

The tooling now follows the same shape as VS Code extension authoring:

| VS Code extension flow | Universal Device Toolkit plugin flow |
|---|---|
| `package.json` is the authoring manifest | `plugin.manifest.json` is the authoring manifest |
| `contributes` declares commands/views | `contributes` declares `webPage` / runtime / optimization entry points |
| `npm run watch` or F5 starts the extension host | Electron `npm run dev` loads `contributes.webPage` |
| `vsce package` creates `.vsix` | `package` creates `<plugin-id>-v<version>.zip` |
| Marketplace metadata is derived from manifest/package fields | generated `Plugins/.build/catalog/store.json` is generated from manifest store metadata and release assets |

The current difference is host compatibility: UDT still needs `plugin.json` in build output because the main app loader consumes that runtime manifest today. The author-facing source of truth is `plugin.manifest.json`; `plugin.json` is synchronized compatibility output.

## Generated Plugin Structure

New scaffolds use `Templates/PluginArchetypes/` as the capability source instead of copying `Plugins/Template`.

The CLI emits:

- plugin project
- test project
- `plugin.manifest.json`
- `plugin.json`
- plugin `CHANGELOG.md`
- resource files
- previewable pages/controls

## Version Management

Version numbers are easy to drift because they appear in several files. Use one rule:

| Layer | Source of truth | Example |
|---|---|---|
| Host app (UDT) | `Directory.Build.props` (`MajorVersion` / `MinorVersion` / `PatchVersion`) | `6.0.0` (shipped stable remains `5.0.2`) |
| Each plugin | `Plugins/Official/<Name>/plugin.manifest.json` → `version` | `custom-mouse` → `2.0.0-preview.1` |
| Plugin store catalog | Generated `Plugins/.build/catalog/store.json` (`--catalog-channel stable\|preview`) | per-plugin `version` + `fileSize` |

Do **not** treat `UniversalDeviceToolkit/Directory.Build.props` as a plugin version. Each plugin keeps its own SemVer.

### Bump a plugin (recommended)

```powershell
.\udt-plugin.cmd bump-version --plugin custom-mouse --version 2.0.0-preview.1
.\udt-plugin.cmd validate --plugin custom-mouse --profile official-candidate
.\udt-plugin.cmd package --plugin custom-mouse --build-first --output-dir Plugins\.build\release-assets
.\udt-plugin.cmd generate-store --plugin-ids custom-mouse --asset-root Plugins\.build\release-assets --catalog-channel preview --merge-existing --require-assets
```

`--part patch|minor|major` still yields a numeric `x.y.z` (prerelease suffix is dropped). Preview labels use explicit `--version`. `FileVersion` / `AssemblyVersion` stay numeric (`2.0.0`) when `Version` is `2.0.0-preview.1`. Publish 2.x with `plugins-release.yml` `catalog_channel=preview` only.

`bump-version` updates `plugin.manifest.json`, then `sync-version` propagates to:

- `plugin.json`
- `store-entry.json`
- `.csproj` `Version` / `FileVersion` / `AssemblyVersion`
- `[Plugin(... version: "...")]` in `*Plugin.cs`
- `package.assetName`

### Check drift without writing

```powershell
.\udt-plugin.cmd sync-version --plugin-ids custom-mouse,shell-integration,vive-tool --check
```

`migrate` is an alias for `sync-version` (same behavior).

## Release Metadata Model

Authoring metadata split:

- `plugin.manifest.json`: authoring source of truth for identity, contributions, package contents, and store metadata
- `plugin.json`: generated/synchronized runtime compatibility manifest
- `store-entry.json`: legacy compatibility output for official metadata
- generated `Plugins/.build/catalog/store.json`: generated release output

That means new plugin contributors should not start by editing generated `Plugins/.build/catalog/store.json`.

## CI Model

Recommended CI split:

- `validate.yml`: contributor validation
- `release.yml`: manual official publishing

Manual official publishing should:

1. validate the selected plugins
2. build them
3. package stable ZIP assets
4. publish GitHub releases
5. regenerate generated `Plugins/.build/catalog/store.json` with `generate-store --plugin-ids <ids> --merge-existing --require-assets`

## Notes

- Do not add source `ProjectReference` links back to the host application's source projects.
- Keep plugin outputs under `Plugins/.build/plugins/UniversalDeviceToolkit.Plugins.<FolderName>/`.
- Keep release ZIP naming stable: `<plugin-id>-v<version>.zip`.
