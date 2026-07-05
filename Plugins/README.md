# Universal Device Toolkit Plugins

<p align="center">
  <img src="https://img.shields.io/badge/C%23-.NET%2010-blue?style=flat-square&logo=csharp" alt="C# .NET 10" />
  <img src="https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D4?style=flat-square&logo=windows" alt="Windows 10/11" />
  <img src="https://img.shields.io/github/stars/SSC-STUDIO/UniversalDeviceToolkit-Plugins?style=flat-square&color=yellow" alt="Stars" />
  <img src="https://img.shields.io/github/license/SSC-STUDIO/UniversalDeviceToolkit-Plugins?style=flat-square" alt="License" />
  <img src="https://img.shields.io/github/workflow/status/SSC-STUDIO/UniversalDeviceToolkit-Plugins/release?style=flat-square&logo=github" alt="CI" />
</p>

<p align="center">
  <b>Official plugin ecosystem for Universal Device Toolkit (formerly Lenovo Legion Toolkit)</b><br/>
  Extend your device management experience with community-driven plugins.
</p>

---

## Plugin Catalog

| Plugin | Description | Tags |
|--------|-------------|------|
| **Network Acceleration** | Boost network performance with real-time telemetry, adaptive acceleration presets, and one-click optimizations for gaming and work. | `network` `optimization` `gaming` `telemetry` |
| **Custom Mouse** | Personalize your mouse with theme-aware cursor styles, DPI profiles, and seamless Windows integration. | `mouse` `customization` `gaming` `cursor` `dpi` |
| **ViVeTool** | Unlock hidden Windows features and customize your system with the ultimate Windows feature flag manager. | `windows` `feature-flags` `vivetool` `tweaks` `insider` |
| **Shell Integration** | Seamlessly integrate into your Windows shell context menu for instant access to power features. | `system` `shell` `integration` `context-menu` |

> **Looking for more plugins?** Check the [plugin store](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/store.json) or [build your own](#author-workflow).

---

## Quick Install

1. Open **Universal Device Toolkit**
2. Navigate to **Plugins** → **Browse Store**
3. Click **Install** on any plugin
4. Restart the application

That's it — no manual downloads, no complex setup.

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
