# Lenovo Legion Toolkit Plugin Development Guide

This repository now supports two clear paths:

1. contributor path
2. official store path

## Contributor Path

Use this when you are developing a plugin locally, in a fork, or for an early PR.

### Standard Commands

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- doctor
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- init --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- dev --plugin my-plugin --theme system
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- build --plugin my-plugin
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- preview --plugin my-plugin --theme system
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- validate --plugin my-plugin --profile contributor
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- package --plugin my-plugin --build-first
```

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
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- promote --plugin my-plugin
```

`promote` also writes `store-entry.json` for compatibility with older release tooling.

### Validation Profiles

- `official-candidate`: plugin-local `store` metadata is required
- `official-release`: release/store alignment checks

Example:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
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

| VS Code extension flow | LLT plugin flow |
|---|---|
| `package.json` is the authoring manifest | `plugin.manifest.json` is the authoring manifest |
| `contributes` declares commands/views | `contributes` declares feature/settings/runtime/optimization entry points |
| `npm run watch` or F5 starts the extension host | `dev` builds and opens `PluginWorkbench` |
| `vsce package` creates `.vsix` | `package` creates `<plugin-id>-v<version>.zip` |
| Marketplace metadata is derived from manifest/package fields | root `store.json` is generated from manifest store metadata and release assets |

The current difference is host compatibility: LLT still needs `plugin.json` in build output because the main app loader consumes that runtime manifest today. The author-facing source of truth is `plugin.manifest.json`; `plugin.json` is synchronized compatibility output.

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

## Release Metadata Model

Authoring metadata split:

- `plugin.manifest.json`: authoring source of truth for identity, contributions, package contents, and store metadata
- `plugin.json`: generated/synchronized runtime compatibility manifest
- `store-entry.json`: legacy compatibility output for official metadata
- root `store.json`: generated release output

That means new plugin contributors should not start by editing root `store.json`.

## CI Model

Recommended CI split:

- `validate.yml`: contributor validation
- `release.yml`: manual official publishing

Manual official publishing should:

1. validate the selected plugins
2. build them
3. package stable ZIP assets
4. publish GitHub releases
5. regenerate root `store.json`

## Notes

- Do not add source `ProjectReference` links back to the sibling main repository.
- Keep plugin outputs under `Build/plugins/LenovoLegionToolkit.Plugins.<FolderName>/`.
- Keep release ZIP naming stable: `<plugin-id>-v<version>.zip`.
