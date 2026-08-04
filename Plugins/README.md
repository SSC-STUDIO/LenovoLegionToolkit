# Universal Device Toolkit Plugins

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="Assets/social-preview.svg">
    <img alt="Universal Device Toolkit Plugins - Extend your Windows device management" src="Assets/social-preview.svg" width="800">
  </picture>
</p>

<p align="center">
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/tag/plugin-catalog">
    <img src="https://img.shields.io/badge/Plugin%20Catalog-rolling-2ea44f?style=for-the-badge&logo=github&logoColor=white&labelColor=222" alt="Plugin Catalog" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit/stargazers">
    <img src="https://img.shields.io/github/stars/SSC-STUDIO/UniversalDeviceToolkit?style=for-the-badge&color=yellow&logo=github&labelColor=222" alt="Stars" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/LICENSE">
    <img src="https://img.shields.io/github/license/SSC-STUDIO/UniversalDeviceToolkit?style=for-the-badge&logo=opensourceinitiative&logoColor=white&color=blue&labelColor=222" alt="License: MIT" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit/actions/workflows/plugins-release.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/SSC-STUDIO/UniversalDeviceToolkit/plugins-release.yml?style=for-the-badge&logo=github&logoColor=white&label=CI&labelColor=222" alt="Plugin CI" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit/discussions">
    <img src="https://img.shields.io/badge/Discussions-Welcome!-blue?style=for-the-badge&logo=github&logoColor=white&labelColor=222" alt="Discussions" />
  </a>
</p>

<p align="center">
  <b>Official plugin ecosystem for <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit">Universal Device Toolkit</a></b><br/>
  <i>Free. Open-source. No ads. No telemetry. Just better Windows.</i><br/>
  <i>Requires host <b>v5.0.0+</b> · .NET 10 · WPF-UI 4.3.0</i>
</p>

<p align="center">
  <a href="README_zh-hans.md">中文说明</a>
</p>

---

## Plugin Catalog

| Status | Plugin | Version | Description | Install ID |
|--------|--------|---------|-------------|------------|
| Active | **Cursor & Pointer** | v1.0.18 | Theme-aware cursor styles, Windows pointer speed, button swapping, safe cursor backup/restore | `custom-mouse` |
| Active | **ViVeTool** | v1.2.4 | Browse and toggle hidden Windows feature flags from a searchable GUI | `vive-tool` |
| Active | **Nilesoft Shell Manager** | v1.0.14 | Manage Nilesoft Shell registration and UDT-managed config (requires Nilesoft Shell) | `shell-integration` |

> Catalog versions match `Plugins/Official/*/plugin.manifest.json` (source of truth). The generated catalog is `Plugins/.build/catalog/plugin-catalog.json` and is published in the rolling `plugin-catalog` release.

---

## Quick Install

1. Open **Universal Device Toolkit** (v5.0.0 or later)
2. Go to **Plugins → Browse Store**
3. Click **Install** on a plugin
4. Restart the app if prompted

No manual downloads required for store plugins.

---

## Why These Plugins?

### 100% Free & Open Source
No paywalls, premium tiers, or ads. MIT-licensed source on GitHub.

### Native Windows 11 Look & Feel
Built with **.NET 10** and **WPF-UI 4.3.0** using Fluent Design tokens. Light/Dark theme support is first-class.

### Extensible by Design
Clean SDK, scaffolder, and **PluginWorkbench** so you can preview plugins without launching the full host app.

### Localized
Official plugins ship resource satellites for **32 cultures** (including `en`, `zh-Hans`, `zh-Hant`).

### Tested
Hundreds of unit tests across Shared + official plugins, plus CI workflows for build/validate/release.

---

## Feature Highlights

### Cursor & Pointer (`custom-mouse`)
- Theme-aware cursor styles (follow Windows Light/Dark)
- Pointer speed and primary-button swap
- Safe cursor backup and restore

### ViVeTool (`vive-tool`)
- Searchable feature-flag browser
- Enable/disable without hand-written CLI
- Safe defaults; feature pages + settings page

### Nilesoft Shell Manager (`shell-integration`)
- Register/unregister Nilesoft Shell from UDT
- Apply or roll back UDT-managed configuration
- Requires a separate Nilesoft Shell install

---

## Author Workflow

Canonical CLI entry: **`udt-plugin.cmd`**  
(`llt-plugin.cmd` is a compatibility alias with the same behavior.)

```powershell
# Environment check
.\udt-plugin.cmd doctor

# Scaffold
.\udt-plugin.cmd init --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"

# Inner loop
.\udt-plugin.cmd dev --plugin my-plugin --theme system --view feature

# Test / validate / package
.\udt-plugin.cmd test --plugin my-plugin
.\udt-plugin.cmd validate --plugin my-plugin --profile contributor
.\udt-plugin.cmd package --plugin my-plugin --build-first
```

| Command | Purpose |
|---------|---------|
| `doctor` | Environment + host dependency checks |
| `init` | Scaffold from `settings-only` / `feature-settings` / `runtime-optimization` |
| `dev` | Build + PluginWorkbench preview loop |
| `test` | Unit tests |
| `validate` | Authoring / store metadata gates (`contributor`, `official-candidate`, …) |
| `package` | Installable ZIP |
| `bump-version` / `sync-version` | SemVer source of truth → project files |
| `promote` | Official store metadata in `plugin.manifest.json` |
| `generate-store` | Regenerate generated `Plugins/.build/catalog/plugin-catalog.json` from manifests + assets |

> Mental model (VS Code extension-like): `plugin.manifest.json` ≈ `package.json`, `dev` ≈ `npm run dev`, `package` ≈ `vsce package`.

---

## PluginWorkbench

Preview without the full host:

```powershell
dotnet run --project .\Plugins\Tooling\PluginWorkbench\PluginWorkbench.csproj -- `
  --repository-root . `
  --plugin-id custom-mouse `
  --theme dark `
  --view settings
```

- Loads build outputs or local ZIPs  
- Host-style shell (feature / settings / optimization cards)  
- System / Light / Dark  
- Safe **Preview** mode by default; **Real Runtime** is explicit  

---

## Repository Structure

```
UniversalDeviceToolkit/
+- Plugins/
|  +- Official/             # Official plugin projects and tests
|  |  +- CustomMouse/
|  |  +- ShellIntegration/
|  |  +- ViveTool/
|  +- SDK/Runtime/          # Plugin SDK runtime surface
|  +- Shared/               # Shared plugin helpers
|  +- Shared.Tests/          # Shared helper tests
|  +- Testing/               # Tooling and performance tests
|  +- Tooling/               # CLI, PluginWorkbench, and smoke tools
|  +- HostBaseline/          # Tracked host release manifest; binaries are downloaded into .host/
|  +- .build/                # Ignored build, package, and catalog output
|  +- udt-plugin.cmd         # Canonical tooling entry
|  +- llt-plugin.cmd          # Compatibility alias
|  `- Make.bat                # Convenience wrappers
+- Docs/Plugins/             # Authoring and architecture docs
`- .github/workflows/plugins-* # Monorepo plugin CI and release workflows
```

---

## Version Rules

| Layer | Source of truth | Current baseline |
|-------|-----------------|------------------|
| Host app | `Plugins/HostBaseline/host-release.json` | **5.0.0** |
| Each plugin SemVer | `Plugins/Official/<Name>/plugin.manifest.json` → `version` | see catalog |
| Min host | `minHostVersion` in manifest; runtime `plugin.json` still exposes ABI field `MinLltVersion` | **5.0.0** |
| Store catalog | Generated `Plugins/.build/catalog/plugin-catalog.json` | release output only |

Do not hand-edit generated `Plugins/.build/catalog/plugin-catalog.json` for routine authoring. Prefer:

```powershell
.\udt-plugin.cmd bump-version --plugin custom-mouse --part patch
.\udt-plugin.cmd package --plugin custom-mouse --build-first
.\udt-plugin.cmd generate-store --plugin-ids custom-mouse --merge-existing --require-assets
```

---

## Contributing

1. Fork and branch from `master`
2. `.\udt-plugin.cmd doctor`
3. Develop with `.\udt-plugin.cmd dev`
4. `validate` + tests green
5. Open a PR

Read [CONTRIBUTING.md](../CONTRIBUTING.md) and [Docs/Plugins/PLUGIN_DEVELOPMENT.md](../Docs/Plugins/PLUGIN_DEVELOPMENT.md).

---

## Documentation

| Doc | Description |
|-----|-------------|
| [Docs/README.md](./Docs/README.md) | Documentation index |
| [Quick Start](./Docs/PLUGIN_QUICKSTART.md) | First plugin in minutes |
| [Development Guide](./Docs/PLUGIN_DEVELOPMENT.md) | API, validation, release flow |
| [Architecture](./Docs/ARCHITECTURE.md) | Layout and dependency map |
| [SDK Changelog](./Docs/SDK_CHANGELOG.md) | SDK / host compatibility |
| [Coding Standards](./Docs/CODING_STANDARDS.md) | Style and anti-patterns |
| [AI Agent Workflow](./Docs/AI_AGENT_WORKFLOW.md) | Automation-friendly commands |
| [Changelog](./CHANGELOG.md) | Project history |

---

## Community & Support

- **Issues**: [GitHub Issues](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/issues)
- **Discussions**: [GitHub Discussions](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/discussions)
- **Host app**: [Universal Device Toolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit)

---

## License

MIT — see [LICENSE](./LICENSE). Individual plugins ship under the same license unless noted.

---

<p align="center">
  Built with care by SSC-STUDIO and the Universal Device Toolkit community.
</p>
