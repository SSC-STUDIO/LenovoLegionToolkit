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

**Host baseline:** Universal Device Toolkit **v5.0.0** (`Plugins/HostBaseline/host-release.json`). Official plugins declare `minHostVersion: "5.0.0"`.

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

## PluginWorkbench

`PluginWorkbench` is the standard author preview host.

### Why It Exists

Authors should not need a live main-app source checkout just to preview plugin UI. The workbench provides:

- host-style feature shell
- host-style settings shell
- optimization preview cards
- dialog hosting through `PluginHostContext`
- `System / Light / Dark`
- safe `Preview` mode by default

### Launch Options

```powershell
dotnet run --project .\Tools\PluginWorkbench\PluginWorkbench.csproj -- `
  --repository-root . `
  --plugin-id custom-mouse `
  --theme dark `
  --view settings
```

Arguments:

- `--plugin-id <id>`
- `--theme system|light|dark`
- `--view feature|settings|optimization`

## VS Code Extension Workflow Mapping

The tooling now follows the same shape as VS Code extension authoring:

| VS Code extension flow | Universal Device Toolkit plugin flow |
|---|---|
| `package.json` is the authoring manifest | `plugin.manifest.json` is the authoring manifest |
| `contributes` declares commands/views | `contributes` declares feature/settings/runtime/optimization entry points |
| `npm run watch` or F5 starts the extension host | `dev` builds and opens `PluginWorkbench` |
| `vsce package` creates `.vsix` | `package` creates `<plugin-id>-v<version>.zip` |
| Marketplace metadata is derived from manifest/package fields | generated `Plugins/.build/catalog/plugin-catalog.json` is generated from manifest store metadata and release assets |

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
| Host app (UDT) | `UniversalDeviceToolkit/Directory.Build.props` (`MajorVersion` / `MinorVersion` / `PatchVersion`) | `5.0.0` |
| Each plugin | `Plugins/Official/<Name>/plugin.manifest.json` → `version` | `custom-mouse` → `1.0.18` |
| Plugin store catalog | Generated `Plugins/.build/catalog/plugin-catalog.json` (release output, not hand-edited) | per-plugin `version` + `fileSize` |

Do **not** treat `UniversalDeviceToolkit/Directory.Build.props` as a plugin version. Each plugin keeps its own SemVer.

### Bump a plugin (recommended)

```powershell
.\udt-plugin.cmd bump-version --plugin custom-mouse --part patch
.\udt-plugin.cmd validate --plugin custom-mouse --profile official-candidate
.\udt-plugin.cmd package --plugin custom-mouse --build-first --output-dir Plugins\.build\release-assets
.\udt-plugin.cmd generate-store --plugin-ids custom-mouse --asset-root Plugins\.build\release-assets --merge-existing --require-assets
```

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
- generated `Plugins/.build/catalog/plugin-catalog.json`: generated release output

That means new plugin contributors should not start by editing generated `Plugins/.build/catalog/plugin-catalog.json`.

## CI Model

Recommended CI split:

- `validate.yml`: contributor validation
- `release.yml`: manual official publishing

Manual official publishing should:

1. validate the selected plugins
2. build them
3. package stable ZIP assets
4. publish GitHub releases
5. regenerate generated `Plugins/.build/catalog/plugin-catalog.json` with `generate-store --plugin-ids <ids> --merge-existing --require-assets`

## Notes

- Do not add source `ProjectReference` links back to the sibling main repository.
- Keep plugin outputs under `Plugins/.build/plugins/UniversalDeviceToolkit.Plugins.<FolderName>/`.
- Keep release ZIP naming stable: `<plugin-id>-v<version>.zip`.
