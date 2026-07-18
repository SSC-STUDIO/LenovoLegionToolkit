# Universal Device Toolkit Plugins

## Project overview

Official plugins for [Universal Device Toolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit) (UDT).
.NET 10 / WPF plugin ecosystem with SDK, Shared helpers, PluginWorkbench, and store packaging.

## Tech stack

- C# / .NET 10 (Windows)
- Solution: `UniversalDeviceToolkit-Plugins.sln`
- Build: `dotnet build`, `Make.bat`, `Directory.Build.props`
- Tooling: `udt-plugin.cmd` (alias: `llt-plugin.cmd`)

## Development rules

- Default branch: `master`
- Host baseline: **v5.0.0** (`Dependencies/Host/host-release.json`)
- Plugin SemVer source of truth: `Plugins/<Name>/plugin.manifest.json`
- Do **not** hand-edit root `store.json` for routine work — regenerate via tooling
- Update `CHANGELOG.md` for user-facing / release work
- Follow SDK interfaces; no source references into the sibling host repo

## Key paths

- `Plugins/` — plugin projects + tests
- `SDK/` — plugin SDK
- `Dependencies/Host/` — vendored host assemblies
- `Tools/` — PluginWorkbench + PluginTooling
- `Scripts/` — bootstrap / gates
- `Docs/` — author docs (`Docs/README.md` index)
- `store.json` — generated marketplace catalog

## Useful commands

```bat
udt-plugin.cmd doctor
udt-plugin.cmd validate --profile contributor
udt-plugin.cmd test
dotnet build UniversalDeviceToolkit-Plugins.sln -c Release
```
