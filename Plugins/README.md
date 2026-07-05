# Universal Device Toolkit Plugins

Official plugins and contributor tooling for Universal Device Toolkit.

## What Changed

This repository now has one standard author workflow:

1. `doctor`
2. `init`
3. `dev`
4. `test`
5. `validate`
6. `package`
7. `promote` only when a plugin should enter the official store

The standard entry point is `llt-plugin.cmd`, which delegates to `Tools/PluginTooling.Cli`.

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
.\llt-plugin.cmd doctor
```

Create machine-readable agent reports:

```powershell
.\llt-plugin.cmd `
  doctor `
  --json-report-path artifacts\agent\doctor.json

.\llt-plugin.cmd `
  inspect `
  --json-report-path artifacts\agent\inspect.json
```

Create a new plugin:

```powershell
.\llt-plugin.cmd `
  init `
  --template feature-settings `
  --folder MyPlugin `
  --id my-plugin `
  --name "My Plugin"
```

Build and preview in one loop:

```powershell
.\llt-plugin.cmd dev --plugin my-plugin --theme system --view feature
```

Run only the build, test, or preview step when needed:

```powershell
.\llt-plugin.cmd build --plugin my-plugin
.\llt-plugin.cmd test --plugin my-plugin
.\llt-plugin.cmd preview --plugin my-plugin --theme system --view feature
```

Validate author requirements:

```powershell
.\llt-plugin.cmd validate --plugin my-plugin --profile contributor
```

Create a local ZIP:

```powershell
.\llt-plugin.cmd package --plugin my-plugin --build-first
```

## Official Store Flow

Official store metadata lives in `Plugins/<Plugin>/plugin.manifest.json` under the `store` object.
`store-entry.json` is still emitted as a compatibility file for older release scripts.

Create the official metadata scaffold:

```powershell
.\llt-plugin.cmd promote --plugin my-plugin
```

Then fill in the final store-facing metadata:

- `description`
- `icon`
- `iconBackground`
- `tags`
- `dependencies`
- `supportedLanguages`

Validation profiles:

- `contributor`: local author checks
- `official-candidate`: official `store` metadata required
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

`plugin.manifest.json` is the authoring source of truth. It combines:

- runtime identity and compatibility
- feature/settings/runtime contributions
- package contents and asset name
- official store metadata

`plugin.json` is generated or synchronized for current host compatibility. Root `store.json` is generated release output, not a normal authoring file.

## VS Code Workflow Parity

The workflow now mirrors VS Code extension development:

- `plugin.manifest.json` plays the role of VS Code's `package.json`
- `contributes` declares plugin entry points
- `dev` is the build-and-open preview loop
- `package` creates the installable ZIP, similar to `vsce package`
- `generate-store` derives release metadata instead of hand-editing root `store.json`

The remaining difference is host compatibility: Universal Device Toolkit still ships `plugin.json` in plugin outputs until the main app loader moves to the unified manifest.

## Common Commands

Short wrapper commands are available through `make.bat`:

- `make.bat doctor`
- `make.bat workbench-smoke --plugin-id custom-mouse --theme Dark`
- `make.bat init --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"`
- `make.bat dev --plugin my-plugin --theme system`
- `make.bat validate --plugin my-plugin --profile contributor`
- `make.bat preview --plugin my-plugin --theme system`
- `make.bat package --plugin my-plugin --build-first`
- `make.bat migrate`
- `make.bat promote --plugin my-plugin`

`new` and `pack` remain compatibility aliases for `init` and `package`.

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
