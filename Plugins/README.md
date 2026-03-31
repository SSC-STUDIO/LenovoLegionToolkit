# Lenovo Legion Toolkit Plugins

Official plugins and plugin development tooling for Lenovo Legion Toolkit (LLT).

## Repository Model

- This repository builds independently from the main `LenovoLegionToolkit` repo.
- Plugin projects compile against vendored host references under `Dependencies/Host`.
- Do not add `ProjectReference` links back to the sibling `LenovoLegionToolkit` source tree.
- Official plugin outputs are expected under `Build/plugins/LenovoLegionToolkit.Plugins.<FolderName>/`.

To refresh host references after the main app changes:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\refresh-host-references.ps1 -UseSiblingRepoBuild
```

## Prerequisites

- .NET 10 SDK
- Windows 10/11 x64
- A valid `Dependencies/Host` baseline, or a sibling LLT checkout that can supply it

## Common Commands

Build all plugins:

```powershell
dotnet build .\LenovoLegionToolkit-Plugins.sln -c Release
```

Validate store metadata, builds, outputs, and optional tests:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\plugin-completion-check.ps1
```

Validate specific plugin IDs only:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\plugin-completion-check.ps1 -PluginIds custom-mouse shell-integration vive-tool -OutputJson artifacts\plugin-completion-check-latest.json
```

Scaffold a new plugin from the maintained template:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\new-plugin.ps1 `
  -FolderName MyPlugin `
  -PluginId my-plugin `
  -DisplayName "My Plugin" `
  -Author "Your Name"
```

Run the visual completion tool:

```powershell
dotnet run --project .\Tools\PluginCompletionUiTool\PluginCompletionUiTool.csproj
```

## Plugin Conventions

- Plugin folder: `Plugins/<FolderName>/`
- Project file: `LenovoLegionToolkit.Plugins.<FolderName>.csproj`
- Manifest file name: `plugin.json`
- Output directory: `Build/plugins/LenovoLegionToolkit.Plugins.<FolderName>/`
- Required manifest fields: `id`, `name`, `version`, `minLLTVersion`, `author`, `isSystemPlugin`
- Official release ZIP name: `<plugin-id>-v<version>.zip`
- Official release tag: `<plugin-id>-v<version>`

Example `plugin.json`:

```json
{
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "minLLTVersion": "3.6.1",
  "author": "Your Name",
  "isSystemPlugin": false,
  "repository": "https://github.com/yourname/your-plugin-repo",
  "issues": "https://github.com/yourname/your-plugin-repo/issues"
}
```

## Release Workflow

This repository's release path is workflow-driven, not tag-driven.

Current official flow:

1. Update `plugin.json`, project version metadata, plugin `CHANGELOG.md`, repository root `CHANGELOG.md`, and `store.json` source metadata as needed.
2. Run `Scripts/plugin-completion-check.ps1`.
3. Trigger `.github/workflows/build.yml` with `workflow_dispatch`.
4. Provide:
   - `plugin`: optional comma-separated folder names
   - `version`: required for release publishing; must match `plugin.json`
5. The workflow builds ZIP assets, publishes per-plugin GitHub releases, then updates `store.json`.

Do not rely on `make.bat zip` or tag-only release instructions; those are not the current publication path.

## Third-Party And New Plugin Onboarding

Use the scaffold script first, then adjust generated UI/resources/tests:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\new-plugin.ps1 `
  -FolderName SamplePlugin `
  -PluginId sample-plugin `
  -DisplayName "Sample Plugin" `
  -Author "Your Name" `
  -Description "Sample plugin for Lenovo Legion Toolkit"
```

Then:

1. Review generated `plugin.json` and project version metadata
2. Add or update the generated plugin `CHANGELOG.md`
3. Review generated test project under `Plugins/<FolderName>.Tests/`
4. Build the new plugin
5. Run completion check for its plugin ID
6. Add or update the corresponding `store.json` entry if the plugin should be published from this repo
7. Update the repository root `CHANGELOG.md`

## Documents

- [Quick Start](./Docs/PLUGIN_QUICKSTART.md)
- [Development Guide](./Docs/PLUGIN_DEVELOPMENT.md)
- [CHANGELOG](./CHANGELOG.md)
