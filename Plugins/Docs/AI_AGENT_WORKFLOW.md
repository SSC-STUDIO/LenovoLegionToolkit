# AI Agent Workflow

This document is for AI agents and automation running against the plugin repository. It does not introduce AI model features or MCP integration.

## Start

1. Read root `README.md`, `Docs/Plugins/README.md`, and `Plugins/KNOWLEDGE_BASE.md` for durable rules.
2. Check `git status --short --branch` (default branch: `master`).
3. Preserve unrelated dirty changes. Do not revert user work.
4. Use the repository tooling shim for plugin commands. It publishes the CLI once under `Plugins/.build/tooling` and reuses the executable:

```bat
udt-plugin.cmd doctor
```

`llt-plugin.cmd` is a compatibility alias. Host baseline is **v5.0.0** (`Plugins/HostBaseline/host-release.json`).

## Agent Evidence Paths

Use `artifacts/agent/` for machine-readable reports:

```sh
./udt-plugin.cmd \
  doctor \
  --json-report-path artifacts/agent/doctor.json

./udt-plugin.cmd \
  inspect \
  --json-report-path artifacts/agent/inspect.json

./udt-plugin.cmd \
  validate \
  --profile contributor \
  --skip-build \
  --skip-tests \
  --json-report-path artifacts/agent/validate-contributor.json
```

Run full candidate validation when the repository is ready for a slower check:

```sh
./udt-plugin.cmd \
  validate \
  --profile official-candidate \
  --json-report-path artifacts/agent/validate-official.json
```

## Version Management

- Plugin SemVer source of truth: `Plugins/Official/<Name>/plugin.manifest.json` → `version`
- Bump: `./udt-plugin.cmd bump-version --plugin <id> --part patch`
- Propagate without bumping: `./udt-plugin.cmd sync-version --plugin <id>`
- Drift check: `./udt-plugin.cmd sync-version --plugin-ids <ids> --check`
- Do not hand-edit `.csproj`, `plugin.json`, `[Plugin]` attribute, or generated catalog version fields for routine releases

## Store Generation

Generated `Plugins/.build/catalog/plugin-catalog.json` should be reproducible from `plugin.manifest.json` store metadata, release assets, and a fixed release date. `store-entry.json` is compatibility output only.

Check without writing:

```sh
./udt-plugin.cmd \
  generate-store \
  --check \
  --release-date 2026-04-21T15:03:21.2902122+00:00
```

Regenerate only when the store diff is intentional:

```sh
./udt-plugin.cmd \
  generate-store \
  --release-date 2026-04-21T15:03:21.2902122+00:00
```

When updating only a selected release set, preserve the other published entries and fail if the expected ZIP is missing:

```sh
./udt-plugin.cmd \
  generate-store \
  --plugin-ids custom-mouse,shell-integration \
  --asset-root Plugins/.build/release-assets \
  --merge-existing \
  --require-assets
```

Do not hand-edit generated `Plugins/.build/catalog/plugin-catalog.json` for normal plugin authoring.

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
