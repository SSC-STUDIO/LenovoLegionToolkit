# Contributing to Universal Device Toolkit Plugins

Thank you for helping improve the official plugin ecosystem for
[Universal Device Toolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit).

## Ways to contribute

- Report bugs or request features via [GitHub Issues](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/issues)
- Improve documentation or translations
- Fix bugs / add features in existing plugins
- Author a new plugin (contributor path or official store path)

## Prerequisites

- Windows 10/11 x64
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git
- Visual Studio 2022 (17.8+) or VS Code / another IDE with C# support

Optional sibling checkout for host source:

```text
/root/src/UniversalDeviceToolkit          # host
/root/src/UniversalDeviceToolkit-Plugins  # this repo
```

If host DLLs are missing, tooling bootstraps from `Dependencies/Host/host-release.json`
(currently **host v5.0.0**).

## Development setup

```powershell
git clone https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins.git
cd UniversalDeviceToolkit-Plugins

.\udt-plugin.cmd doctor
dotnet restore
dotnet build UniversalDeviceToolkit-Plugins.sln -c Release
```

`udt-plugin.cmd` is the canonical entry. `llt-plugin.cmd` is a compatibility alias.
`Make.bat` wraps common tasks (`doctor`, `dev`, `package`, …).

## Plugin development paths

### Contributor path (forks / early PRs)

```powershell
.\udt-plugin.cmd doctor
.\udt-plugin.cmd init --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"
.\udt-plugin.cmd dev --plugin my-plugin --theme system --view feature
.\udt-plugin.cmd test --plugin my-plugin
.\udt-plugin.cmd validate --plugin my-plugin --profile contributor
.\udt-plugin.cmd package --plugin my-plugin --build-first
```

You do **not** need official store promotion for this path.

### Official store path

Only when the plugin should ship from this repository’s marketplace:

```powershell
.\udt-plugin.cmd promote --plugin my-plugin
.\udt-plugin.cmd validate --plugin my-plugin --profile official-candidate
```

Requirements:

- `plugin.manifest.json` with a complete `store` object
- Plugin `CHANGELOG.md`
- Entry under root `CHANGELOG.md` `[Unreleased]`
- Tests for non-trivial logic
- Successful `official-candidate` validation

## Versioning

| Layer | Source of truth |
|-------|-----------------|
| Plugin SemVer | `Plugins/<Name>/plugin.manifest.json` → `version` |
| Minimum host | `minHostVersion` (must match host baseline **5.0.0+** for current ABI) |
| Runtime host field | `plugin.json` → `MinLltVersion` (legacy JSON property name; value is the UDT host version) |
| Root store catalog | Generated `store.json` — not hand-edited for routine work |

```powershell
.\udt-plugin.cmd bump-version --plugin <id> --part patch|minor|major
.\udt-plugin.cmd sync-version --plugin <id>          # propagate without bump
.\udt-plugin.cmd sync-version --plugin-ids a,b --check
```

## Coding standards

- Follow [Docs/CODING_STANDARDS.md](./Docs/CODING_STANDARDS.md)
- Prefer `PluginBase` and documented SDK interfaces
- UI: `DynamicResource` colors, `.resx` strings, `WpfFallbackHelper` fallback UI
- `TreatWarningsAsErrors` is enabled — keep zero warnings
- No source references into the sibling host repo; use vendored host assemblies

### Commit messages

[Conventional Commits](https://www.conventionalcommits.org/):

```text
feat(custom-mouse): add high-contrast cursor pack
fix(vive-tool): handle empty feature catalog
docs(readme): sync catalog versions with manifests
chore(tooling): prefer udt-plugin.cmd entry
```

## Pull requests

Checklist:

- [ ] `dotnet build` succeeds
- [ ] Relevant tests pass (`.\udt-plugin.cmd test` or `dotnet test`)
- [ ] Light + Dark checked in PluginWorkbench when UI changes
- [ ] `plugin.manifest.json` / `CHANGELOG.md` updated when needed
- [ ] No hand-edited root `store.json` unless you regenerated it intentionally
- [ ] PR describes **what** and **why**

Default branch: **`master`**.

## Documentation map

See [Docs/README.md](./Docs/README.md) for the current vs historical document list.

## Community

- Issues: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/issues
- Discussions: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/discussions
- Host app: https://github.com/SSC-STUDIO/UniversalDeviceToolkit

Thank you for contributing.
