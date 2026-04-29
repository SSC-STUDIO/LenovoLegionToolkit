# Lenovo Legion Toolkit Plugins

Official plugins and contributor tooling for Lenovo Legion Toolkit (LLT).

## What Changed

This repository now has one standard author workflow:

1. `doctor`
2. `new`
3. `build`
4. `preview`
5. `validate`
6. `pack`
7. `promote` only when a plugin should enter the official store

The standard entry point is `Tools/PluginTooling.Cli`.

## Prerequisites

- Windows 10/11 x64
- .NET 10 SDK
- A valid host baseline under `Dependencies/Host`

Bootstrap host references when needed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\ensure-host-dependencies.ps1
```

`Dependencies/Host/host-release.json` is the pinned host baseline for standalone plugin development.

## Standard Author Workflow

Check the environment:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- doctor
```

Create machine-readable agent reports:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  doctor `
  --json-report-path artifacts\agent\doctor.json

dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  inspect `
  --json-report-path artifacts\agent\inspect.json
```

Create a new plugin:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  new `
  --template feature-settings `
  --folder MyPlugin `
  --id my-plugin `
  --name "My Plugin"
```

Build one plugin:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  build `
  --plugin my-plugin
```

Preview it in the standalone host:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  preview `
  --plugin my-plugin `
  --theme system `
  --view feature
```

Validate author requirements:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  validate `
  --plugin my-plugin `
  --profile contributor
```

Create a local ZIP:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  pack `
  --plugin my-plugin `
  --build-first
```

## Official Store Flow

Only official plugins need `store-entry.json`.

Create the official metadata scaffold:

```powershell
dotnet run --project .\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj -- `
  promote `
  --plugin my-plugin
```

Then fill in the final store-facing metadata:

- `description`
- `icon`
- `iconBackground`
- `tags`
- `dependencies`
- `supportedLanguages`

Validation profiles:

- `contributor`: local author checks, no `store-entry.json` required
- `official-candidate`: official metadata required
- `official-release`: release/store alignment checks

## PluginWorkbench

`PluginWorkbench` is the standard preview UI for authors.

It now:

- loads built plugin outputs or local ZIPs
- uses a host-style preview shell for feature/settings/optimization content
- supports `System / Light / Dark`
- defaults to safe `Preview` mode
- requires explicit confirmation before `Real Runtime`

Direct launch:

```powershell
dotnet run --project .\Tools\PluginWorkbench\PluginWorkbench.csproj -- `
  --repository-root . `
  --plugin-id custom-mouse `
  --theme dark `
  --view settings
```

Smoke:

```powershell
make.bat workbench-smoke --plugin-id custom-mouse --theme Dark
```

## Store Metadata Model

Runtime identity lives in each plugin's `plugin.json`.

Official store-facing metadata now lives beside the plugin in `store-entry.json`.

Root `store.json` should be treated as generated release output, not as the first thing authors edit for new plugins.

## Common Commands

Short wrapper commands are available through `make.bat`:

- `make.bat doctor`
- `make.bat workbench-smoke --plugin-id custom-mouse --theme Dark`
- `make.bat new --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"`
- `make.bat validate --plugin my-plugin --profile contributor`
- `make.bat preview --plugin my-plugin --theme system`
- `make.bat pack --plugin my-plugin --build-first`
- `make.bat promote --plugin my-plugin`

## Release Model

Preferred CI model:

- `validate.yml` for PR/push validation
- `release.yml` for manual official publishing

Official release assets must keep the stable naming contract:

- `<plugin-id>-v<version>.zip`

## Documents

- [Quick Start](./Docs/PLUGIN_QUICKSTART.md)
- [Development Guide](./Docs/PLUGIN_DEVELOPMENT.md)
- [Architecture](./Docs/ARCHITECTURE.md)
- [AI Agent Workflow](./Docs/AI_AGENT_WORKFLOW.md)
- [Contributing](./CONTRIBUTING.md)
- [CHANGELOG](./CHANGELOG.md)
