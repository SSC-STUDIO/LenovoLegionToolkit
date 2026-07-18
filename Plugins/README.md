# Universal Device Toolkit Plugins

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="Assets/social-preview.svg">
    <img alt="Universal Device Toolkit Plugins - Extend your Windows device management" src="Assets/social-preview.svg" width="800">
  </picture>
</p>

<p align="center">
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/latest">
    <img src="https://img.shields.io/github/v/release/SSC-STUDIO/UniversalDeviceToolkit-Plugins?style=for-the-badge&logo=github&logoColor=white&color=brightgreen&label=Latest+Release&labelColor=222" alt="Latest Release" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/stargazers">
    <img src="https://img.shields.io/github/stars/SSC-STUDIO/UniversalDeviceToolkit-Plugins?style=for-the-badge&color=yellow&logo=github&labelColor=222" alt="Stars" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/LICENSE">
    <img src="https://img.shields.io/github/license/SSC-STUDIO/UniversalDeviceToolkit-Plugins?style=for-the-badge&logo=opensourceinitiative&logoColor=white&color=blue&labelColor=222" alt="License: MIT" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/actions">
    <img src="https://img.shields.io/github/actions/workflow/status/SSC-STUDIO/UniversalDeviceToolkit-Plugins/release.yml?style=for-the-badge&logo=github&logoColor=white&label=CI&labelColor=222" alt="CI" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/discussions">
    <img src="https://img.shields.io/badge/Discussions-Welcome!-blue?style=for-the-badge&logo=github&logoColor=white&labelColor=222" alt="Discussions" />
  </a>
</p>

<p align="center">
  <b>Official plugin ecosystem for <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit">Universal Device Toolkit</a></b><br/>
  <i>Free. Open-source. No ads. No telemetry. Just better Windows.</i><br/>
  <i>Requires host <b>v5.0.0+</b> · .NET 10 · WPF-UI 4.3.0</i>
</p>

<p align="center">
  <a href="README.zh-CN.md">中文说明</a>
</p>

---

## Plugin Catalog

| Status | Plugin | Version | Description | Install ID |
|--------|--------|---------|-------------|------------|
| Active | **Cursor & Pointer** | v1.0.17 | Theme-aware cursor styles, Windows pointer speed, button swapping, safe cursor backup/restore | `custom-mouse` |
| Active | **ViVeTool** | v1.2.3 | Browse and toggle hidden Windows feature flags from a searchable GUI | `vive-tool` |
| Active | **Nilesoft Shell Manager** | v1.0.13 | Manage Nilesoft Shell registration and UDT-managed config (requires Nilesoft Shell) | `shell-integration` |
| Migrated | Battery Health | v1.0.0 | **Not in store** — built into Universal Device Toolkit; source kept for settings migration only | `battery-health` |
| Migrated | Network Acceleration | v1.2.0 | **Not in store** — built into Universal Device Toolkit; source kept for settings migration only | `network-acceleration` |

> Catalog versions match `Plugins/*/plugin.manifest.json` (source of truth). Active store listings live in generated root [`store.json`](./store.json).

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
~600 unit tests across Shared + official plugins, plus CI workflows for build/validate/release.

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

### Migrated (host built-ins)
**Battery Health** and **Network Acceleration** features now ship inside Universal Device Toolkit. Plugin projects remain for upgrade migration only and are **not** offered in the marketplace.

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
| `generate-store` | Regenerate root `store.json` from manifests + assets |

> Mental model (VS Code extension-like): `plugin.manifest.json` ≈ `package.json`, `dev` ≈ `npm run dev`, `package` ≈ `vsce package`.

---

## PluginWorkbench

Preview without the full host:

```powershell
dotnet run --project .\Tools\PluginWorkbench\PluginWorkbench.csproj -- `
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
UniversalDeviceToolkit-Plugins/
├── Plugins/                 # Official plugin projects + tests
│   ├── Shared/              # Shared helpers (settings, process, HTTP, …)
│   ├── CustomMouse/
│   ├── ShellIntegration/
│   ├── ViveTool/
│   ├── BatteryHealth/       # Migrated (not in store)
│   └── NetworkAcceleration/ # Migrated (not in store)
├── SDK/                     # Plugin SDK surfaces
├── Dependencies/Host/       # Vendored host refs (see host-release.json → v5.0.0)
├── Tools/                   # PluginWorkbench + PluginTooling.CLI
├── Scripts/                 # Host bootstrap, governance, helpers
├── Docs/                    # Authoring & architecture (see Docs/README.md)
├── store.json               # Generated store catalog (release output)
├── udt-plugin.cmd           # Canonical tooling entry
├── llt-plugin.cmd           # Compatibility alias
└── Make.bat                 # Convenience wrappers
```

---

## Version Rules

| Layer | Source of truth | Current baseline |
|-------|-----------------|------------------|
| Host app | sibling `UniversalDeviceToolkit` / `Dependencies/Host/host-release.json` | **5.0.0** |
| Each plugin SemVer | `Plugins/<Name>/plugin.manifest.json` → `version` | see catalog |
| Min host | `minHostVersion` in manifest; runtime `plugin.json` still exposes ABI field `MinLltVersion` | **5.0.0** |
| Store catalog | Generated `store.json` | release output only |

Do not hand-edit root `store.json` for routine authoring. Prefer:

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

Read [CONTRIBUTING.md](./CONTRIBUTING.md) and [Docs/PLUGIN_DEVELOPMENT.md](./Docs/PLUGIN_DEVELOPMENT.md).

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

- **Issues**: [GitHub Issues](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/issues)
- **Discussions**: [GitHub Discussions](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/discussions)
- **Host app**: [Universal Device Toolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit)

---

## License

MIT — see [LICENSE](./LICENSE). Individual plugins ship under the same license unless noted.

---

<p align="center">
  Built with care by SSC-STUDIO and the Universal Device Toolkit community.
</p>
