# Lenovo Legion Toolkit Plugin Quick Start

This is the shortest path for creating a new plugin in this repository.

## 1. Check The Environment

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- doctor
```

If host references are missing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\ensure-host-dependencies.ps1
```

## 2. Create A Scaffold

Pick one archetype:

- `settings-only`
- `feature-settings`
- `runtime-optimization`

Example:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  init `
  --template feature-settings `
  --folder MyPlugin `
  --id my-plugin `
  --name "My Plugin"
```

This generates:

- `Plugins/MyPlugin/`
- `Plugins/MyPlugin.Tests/`
- `plugin.manifest.json`
- `plugin.json`
- plugin `CHANGELOG.md`
- resource files
- a test project

## 3. Build The Plugin

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  build `
  --plugin my-plugin
```

## 4. Preview In The Host Shell

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  preview `
  --plugin my-plugin `
  --theme system `
  --view feature
```

Use `dev` for the normal inner loop:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  dev `
  --plugin my-plugin `
  --theme system `
  --view feature
```

`PluginWorkbench` is the standard preview host. It supports:

- `System / Light / Dark`
- host-style feature preview
- host-style settings preview
- optimization preview
- `Preview / Real Runtime`

## 5. Validate Author Requirements

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  validate `
  --plugin my-plugin `
  --profile contributor
```

## 6. Pack A Local ZIP

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  package `
  --plugin my-plugin `
  --build-first
```

## 7. Promote Only For Official Store Candidates

If the plugin should become an official plugin:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  promote `
  --plugin my-plugin
```

That ensures the `store` object in `plugin.manifest.json` is ready and writes the legacy `store-entry.json` compatibility file. After that, validate with:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  validate `
  --plugin my-plugin `
  --profile official-candidate
```

## 8. Do Not Start With Root store.json

For new plugin authoring:

- edit `plugin.manifest.json`
- implement the plugin
- preview it
- validate it
- package it

`plugin.json` is kept as a host-compatibility output. Root `store.json` is release output, not the normal starting point for contributors.
