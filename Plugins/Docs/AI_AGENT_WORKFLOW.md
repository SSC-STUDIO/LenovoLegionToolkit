# AI Agent Workflow

This document is for AI agents and automation running against the plugin repository. It does not introduce AI model features or MCP integration.

## Start

1. Read the machine-wide workstation context from `/mnt/c/Users/96152/.agents/skills/workstation-context/SKILL.md`.
2. Check `git status --short --branch`.
3. Preserve unrelated dirty changes. Do not revert user work.
4. Prefer the Windows `dotnet.exe` from WSL for WPF builds:

```sh
'/mnt/c/Program Files/dotnet/dotnet.exe' build LenovoLegionToolkit-Plugins.sln --configuration Release --nologo
```

## Agent Evidence Paths

Use `artifacts/agent/` for machine-readable reports:

```sh
dotnet run --project Tools/PluginTooling.Cli/PluginTooling.Cli.csproj -- \
  doctor \
  --repository-root . \
  --json-report-path artifacts/agent/doctor.json

dotnet run --project Tools/PluginTooling.Cli/PluginTooling.Cli.csproj -- \
  inspect \
  --repository-root . \
  --json-report-path artifacts/agent/inspect.json

dotnet run --project Tools/PluginTooling.Cli/PluginTooling.Cli.csproj -- \
  validate \
  --repository-root . \
  --profile contributor \
  --skip-build \
  --skip-tests \
  --json-report-path artifacts/agent/validate-contributor.json
```

Run full candidate validation when the repository is ready for a slower check:

```sh
dotnet run --project Tools/PluginTooling.Cli/PluginTooling.Cli.csproj -- \
  validate \
  --repository-root . \
  --profile official-candidate \
  --json-report-path artifacts/agent/validate-official.json
```

## Store Generation

Root `store.json` should be reproducible from plugin manifests, `store-entry.json`, release assets, and a fixed release date.

Check without writing:

```sh
dotnet run --project Tools/PluginTooling.Cli/PluginTooling.Cli.csproj -- \
  generate-store \
  --repository-root . \
  --check \
  --release-date 2026-04-21T15:03:21.2902122+00:00
```

Regenerate only when the store diff is intentional:

```sh
dotnet run --project Tools/PluginTooling.Cli/PluginTooling.Cli.csproj -- \
  generate-store \
  --repository-root . \
  --release-date 2026-04-21T15:03:21.2902122+00:00
```

Do not hand-edit root `store.json` for normal plugin authoring.

## Workbench Smoke

Build the solution and smoke the standalone host in both themes for each official plugin:

```bat
make.bat workbench-smoke --plugin-id custom-mouse --theme Light
make.bat workbench-smoke --plugin-id custom-mouse --theme Dark
make.bat workbench-smoke --plugin-id network-acceleration --theme Light
make.bat workbench-smoke --plugin-id network-acceleration --theme Dark
make.bat workbench-smoke --plugin-id shell-integration --theme Light
make.bat workbench-smoke --plugin-id shell-integration --theme Dark
make.bat workbench-smoke --plugin-id vive-tool --theme Light
make.bat workbench-smoke --plugin-id vive-tool --theme Dark
```

UI acceptance points:

- Light and Dark themes use host resources for foregrounds and status colors.
- Main action buttons, status text, settings entry points, Shell style dialog controls, and ViveTool search/table controls have stable `AutomationId` values.
- Buttons and status rows do not overflow in the Workbench settings shell.
- ViveTool keeps a DataGrid-first feature view with readable status and action columns.

## Changelog

Record user-visible changes:

- root `CHANGELOG.md` for repository-level workflow, official plugin UI, and tooling changes
- plugin `CHANGELOG.md` for plugin-specific UI or behavior changes

Skip purely internal renames and development-only cleanup unless users or contributors need to know.
