# Universal Device Toolkit Plugins

<p align="center">
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins">
    <img src="https://img.shields.io/badge/C%23-.NET%2010-blue?style=for-the-badge&logo=csharp&logoColor=white" alt="C# .NET 10" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins">
    <img src="https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Windows 10/11" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/stargazers">
    <img src="https://img.shields.io/github/stars/SSC-STUDIO/UniversalDeviceToolkit-Plugins?style=for-the-badge&color=yellow&logo=github" alt="Stars" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/LICENSE">
    <img src="https://img.shields.io/github/license/SSC-STUDIO/UniversalDeviceToolkit-Plugins?style=for-the-badge&logo=opensourceinitiative&logoColor=white" alt="License" />
  </a>
  <a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/actions">
    <img src="https://img.shields.io/github/actions/workflow/status/SSC-STUDIO/UniversalDeviceToolkit-Plugins/release.yml?style=for-the-badge&logo=github&logoColor=white" alt="CI" />
  </a>
</p>

<p align="center">
  <b>Official plugin ecosystem for Universal Device Toolkit (formerly Lenovo Legion Toolkit)</b><br/>
  Extend your Windows device management experience with community-driven plugins.
</p>

<p align="center">
  <i>Free. Open-source. No ads. No telemetry. Just better Windows.</i>
</p>

---

## Plugin Catalog

<p align="center">
  <img src="https://img.shields.io/badge/Plugins-4-blue?style=flat-square" />
  <img src="https://img.shields.io/badge/Downloads-10K%2B-brightgreen?style=flat-square" />
  <img src="https://img.shields.io/badge/Contributors-Welcome!-purple?style=flat-square" />
</p>

| # | Plugin | Version | Description | Install |
|---|--------|---------|-------------|---------|
| 🔥 | **Network Acceleration** | v1.2.0 | Real-time network telemetry with a redesigned dual-tab UI. Track speeds, peak traffic, and apply gaming presets with one click. | `network-acceleration` |
| 🖱️ | **Custom Mouse** | v1.0.16 | Theme-aware cursor styles, DPI profiles, and seamless Windows pointer speed management. Auto-adapts to Light/Dark mode. | `custom-mouse` |
| 🔧 | **ViVeTool** | v1.2.2 | Unlock hidden Windows feature flags from a searchable GUI. No command-line needed — browse, search, enable, and disable features safely. | `vive-tool` |
| 🐚 | **Shell Integration** | v1.0.12 | Right-click context menu integration. Instant access to power features from anywhere in Windows Explorer. | `shell-integration` |

> **Looking for more plugins?** Check the [plugin store](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/store.json) or [build your own](#author-workflow).

---

## Quick Install

1. Open **Universal Device Toolkit**
2. Navigate to **Plugins** → **Browse Store**
3. Click **Install** on any plugin
4. Restart the application

That's it — no manual downloads, no complex setup.

---

## Why These Plugins?

### 🔐 100% Free & Open Source
No paywalls, no premium tiers, no ads. Every line of code is on GitHub under the MIT License. Audit it, fork it, contribute back.

### 🎨 Native Windows 11 Look & Feel
Built with .NET 10 and WPF-UI 4.3.0, these plugins use real Fluent Design tokens. They adapt to your Windows theme automatically — no "light mode only" or "dark mode broken" bugs.

### 🔧 Extensible by Design
The plugin SDK is clean and well-documented. Want a plugin that does X? Fork the repo, run `init`, and you're building in 2 minutes. The included PluginWorkbench lets you preview plugins without launching the full host app.

### 🌍 Localized
All plugins support English and Chinese out of the box. Adding a new language is as simple as adding a `.resx` file.

### 🧪 Battle-Tested
Every official plugin ships with unit tests, visual smoke tests (Light + Dark themes), and automated CI/CD via GitHub Actions.

---

## Feature Highlights

### Network Acceleration
- Real-time download/upload telemetry with a beautiful dashboard
- Adaptive acceleration presets for gaming, streaming, and work
- One-click network optimization
- Peak traffic monitoring and active adapter detection

### Custom Mouse
- Theme-aware cursor styles (auto-switch with Windows Dark/Light mode)
- Per-application DPI profiles
- Seamless Windows integration via the optimization panel

### ViVeTool
- Browse and toggle hidden Windows feature flags
- Insider-build style tweaks without joining Insider
- Clean table UI with search and filtering
- Safe defaults — nothing breaks on toggle

### Shell Integration
- Add Universal Device Toolkit actions to the Windows right-click context menu
- Quick access to power plans, RGB control, and fan profiles
- Minimal footprint, no background services

---

## Author Workflow

This repository includes a complete plugin authoring toolchain:

```powershell
# Check your environment
.\llt-plugin.cmd doctor

# Create a new plugin
.\llt-plugin.cmd init --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"

# Develop with live preview
.\llt-plugin.cmd dev --plugin my-plugin --theme system --view feature

# Test, validate, package
.\llt-plugin.cmd test --plugin my-plugin
.\llt-plugin.cmd validate --plugin my-plugin --profile contributor
.\llt-plugin.cmd package --plugin my-plugin --build-first
```

### Plugin Tooling Commands

| Command | Description |
|---------|-------------|
| `doctor` | Diagnose environment and dependencies |
| `init` | Scaffold a new plugin from a template |
| `dev` | Build + live preview loop |
| `test` | Run unit tests |
| `validate` | Check authoring and store metadata |
| `package` | Produce installable ZIP |
| `promote` | Prepare official store entry |

> The toolchain mirrors the **VS Code extension development model**: `plugin.manifest.json` is your `package.json`, `dev` is your `npm run dev`, and `package` is your `vsce package`.

---

## PluginWorkbench — Standalone Preview

Don't want to launch the full host app? Use **PluginWorkbench**:

```powershell
dotnet run --project .\Tools\PluginWorkbench\PluginWorkbench.csproj -- `
  --repository-root . `
  --plugin-id custom-mouse `
  --theme dark `
  --view settings
```

Features:
- Loads built plugin outputs or local ZIPs
- Host-style preview shell (matches the real UI)
- `System` / `Light` / `Dark` theme switching
- Safe `Preview` mode (no real actions) with explicit `Real Runtime` confirmation

---

## Repository Structure

```
UniversalDeviceToolkit-Plugins/
├── Plugins/              # Official plugin projects
│   ├── CustomMouse/
│   ├── NetworkAcceleration/
│   ├── ShellIntegration/
│   └── ViveTool/
├── SDK/                  # Plugin SDK (interfaces & helpers)
├── Dependencies/         # Shared dependencies
├── Tools/                # PluginWorkbench + PluginTooling.CLI
├── Scripts/              # Automation scripts
├── Docs/                 # Architecture & authoring guides
├── store.json            # Plugin store catalog (release output)
└── Make.bat             # Convenience wrapper for common tasks
```

---

## Contributing

We welcome contributions! Whether you're fixing a bug, adding a feature, or creating a brand-new plugin:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-plugin`)
3. Run `.\llt-plugin.cmd doctor` to validate your environment
4. Develop your plugin with `.\llt-plugin.cmd dev`
5. Submit a pull request

Please read [CONTRIBUTING.md](./CONTRIBUTING.md) and the [Plugin Development Guide](./Docs/PLUGIN_DEVELOPMENT.md) before getting started.

---

## Documentation

- [Quick Start](./Docs/PLUGIN_QUICKSTART.md) — Get your first plugin running in 5 minutes
- [Development Guide](./Docs/PLUGIN_DEVELOPMENT.md) — Deep dive into the plugin API
- [Architecture](./Docs/ARCHITECTURE.md) — System design and dependency map
- [AI Agent Workflow](./Docs/AI_AGENT_WORKFLOW.md) — Automation-friendly workflow docs
- [Coding Standards](./Docs/CODING_STANDARDS.md) — Naming, patterns, and forbidden anti-patterns
- [Changelog](./CHANGELOG.md) — Release history

---

## Community & Support

- **Issues**: [GitHub Issues](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/issues)
- **Discussions**: [GitHub Discussions](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/discussions)
- **Main Application**: [Universal Device Toolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit)

---

## License

This project is open-source. See individual plugin licenses for details.

---

<p align="center">
  Built with ❤️ by the SSC-STUDIO team and the Universal Device Toolkit community.
</p>
