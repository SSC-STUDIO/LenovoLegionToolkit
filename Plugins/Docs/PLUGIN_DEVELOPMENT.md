# Lenovo Legion Toolkit Plugin Development Guide

This repository now supports two clear paths:

1. contributor path
2. official store path

## Contributor Path

Use this when you are developing a plugin locally, in a fork, or for an early PR.

### Standard Commands

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- doctor
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- new --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- build --plugin my-plugin
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- preview --plugin my-plugin --theme system
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- validate --plugin my-plugin --profile contributor
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- pack --plugin my-plugin --build-first
```

### Validation Profile

`contributor` checks:

- `plugin.json`
- project naming
- version alignment
- test project presence
- build output shape
- optional build/test execution

It does not require `store-entry.json`.

## Official Store Path

Use this only when the plugin is intended to ship from the official repository.

### Additional File Contract

Each official plugin now owns its store-facing metadata in:

```text
Plugins/<FolderName>/store-entry.json
```

This file contains:

- `description`
- `icon`
- `iconBackground`
- `tags`
- `dependencies`
- `supportedLanguages`
- optional `repositoryUrl`

Create the initial file with:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- promote --plugin my-plugin
```

### Validation Profiles

- `official-candidate`: plugin-local official metadata is required
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

## Generated Plugin Structure

New scaffolds use `Templates/PluginArchetypes/` as the capability source instead of copying `Plugins/Template`.

The CLI emits:

- plugin project
- test project
- `plugin.json`
- plugin `CHANGELOG.md`
- resource files
- previewable pages/controls

## Release Metadata Model

Authoring metadata split:

- `plugin.json`: runtime identity and compatibility
- `store-entry.json`: official store-facing metadata
- root `store.json`: generated release output

That means new plugin contributors should not start by editing root `store.json`.

## CI Model

Recommended CI split:

- `validate.yml`: contributor validation
- `release.yml`: manual official publishing

Manual official publishing should:

1. validate the selected plugins
2. build them
3. pack stable ZIP assets
4. publish GitHub releases
5. regenerate root `store.json`

## Notes

- Do not add source `ProjectReference` links back to the sibling main repository.
- Keep plugin outputs under `Build/plugins/LenovoLegionToolkit.Plugins.<FolderName>/`.
- Keep release ZIP naming stable: `<plugin-id>-v<version>.zip`.
