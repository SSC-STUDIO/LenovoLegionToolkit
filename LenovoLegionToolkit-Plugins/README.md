# Lenovo Legion Toolkit Plugins

![LLT Plugins](https://img.shields.io/badge/Lenovo%20Legion%20Toolkit-Plugins-blue?style=for-the-badge&logo=windows)
![.NET 10](https://img.shields.io/badge/.NET-10.0-purple?style=for-the-badge&logo=.net)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

Official plugin repository for Lenovo Legion Toolkit. Extend your Legion Toolkit with community-developed plugins!

## 📸 Plugin Showcase

### ViveTool - Windows Feature Management

![ViveTool Screenshot](https://via.placeholder.com/800x450/1a1a2e/ffffff?text=ViveTool+Plugin)

Windows feature management plugin for advanced system configuration.

### CustomMouse - Cursor Customization

![CustomMouse Screenshot](https://via.placeholder.com/800x450/0f3460/ffffff?text=CustomMouse+Plugin)

Custom mouse cursor styles with intelligent color detection.

### ShellIntegration - Enhanced Windows Shell

![ShellIntegration Screenshot](https://via.placeholder.com/800x450/16213e/ffffff?text=ShellIntegration+Plugin)

Enhanced Windows Shell integration with context menu extensions.

### NetworkAcceleration - Network Optimization

![NetworkAcceleration Screenshot](https://via.placeholder.com/800x450/e94560/ffffff?text=NetworkAcceleration+Plugin)

Network acceleration with DNS over HTTPS support.

---

## 🎯 Available Plugins

| Plugin | Description | Status |
|--------|-------------|--------|
| [ViveTool](plugins/ViveTool/) | Windows feature management | ✅ Active |
| [CustomMouse](plugins/CustomMouse/) | Custom cursor styles | ✅ Active |
| [ShellIntegration](plugins/ShellIntegration/) | Shell enhancements | ✅ Active |
| [NetworkAcceleration](plugins/NetworkAcceleration/) | Network optimization | ✅ Active |

---

## 🚀 Quick Start

### Download Plugins

**Option 1: Plugin Manager (Recommended)**
1. Open Lenovo Legion Toolkit
2. Navigate to **Plugins & Extensions**
3. Browse available plugins
4. Click **Install** on desired plugins

**Option 2: Manual Download**
- Visit the [Releases](https://github.com/Crs10259/LenovoLegionToolkit-Plugins/releases) page
- Download the desired plugin ZIP file
- Extract to `%APPDATA%\LenovoLegionToolkit\plugins\[plugin-id]\`

---

## 🔧 For Developers

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- Windows 10/11 with PowerShell 5.1+
- Visual Studio 2022 or VS Code

### Build Plugins

#### Windows (PowerShell)

```powershell
# Build all plugins (Release)
make.bat

# Build a specific plugin
make.bat ViveTool

# Create ZIP packages
make.bat zip

# Clean build outputs
make.bat clean
```

#### Linux/macOS (Bash)

```bash
# Build all plugins
./build.sh

# Build specific plugin
./build.sh ViveTool

# Create ZIP packages
./build.sh zip
```

### Create New Plugin

See our comprehensive [Plugin Development Guide](docs/PLUGIN_DEVELOPMENT.md) for detailed instructions.

#### Minimal Example

```csharp
using LenovoLegionToolkit.Lib.Plugins;

namespace MyPlugin
{
    public class MyPlugin : IPlugin
    {
        public string Id => "my-plugin";
        public string Name => "My Plugin";
        public string Description => "Description here";
        public string Icon => "Apps24";
        public bool IsSystemPlugin => false;
        public string[]? Dependencies => null;

        public void OnInstalled() { }
        public void OnUninstalled() { }
        public void OnShutdown() { }
        public void Stop() { }
    }
}
```

---

## 📁 Project Structure

```
LenovoLegionToolkit-Plugins/
├── plugins/                          # Plugin source code
│   ├── SDK/                          # Plugin SDK
│   ├── template/                     # Plugin template
│   ├── src/Common/                   # Shared utilities
│   ├── ViveTool/                     # ViveTool plugin
│   ├── CustomMouse/                  # CustomMouse plugin
│   ├── ShellIntegration/             # ShellIntegration plugin
│   └── NetworkAcceleration/          # NetworkAcceleration plugin
├── docs/                             # Documentation
│   ├── PLUGIN_DEVELOPMENT.md        # Full development guide
│   └── PLUGIN_QUICKSTART.md         # Quick start guide
├── build/                            # Build output
├── .github/
│   └── workflows/
│       └── build.yml                # CI/CD pipeline
├── store.json                        # Plugin store metadata
├── make.bat                          # Windows build script
├── build.sh                          # Linux/macOS build script
└── README.md
```

---

## 🔄 CI/CD Pipeline

This repository uses GitHub Actions for continuous integration and deployment:

### Automated Workflow

1. **On Push**: Builds all plugins when code is pushed to `main` or `master`
2. **On Release**: Creates ZIP packages and GitHub Releases
3. **Store Updates**: Automatically updates `store.json` with new checksums

### Manual Trigger

Navigate to [Actions > Build Plugins](https://github.com/Crs10259/LenovoLegionToolkit-Plugins/actions/workflows/build.yml):

1. Click **Run workflow**
2. Select specific plugin (optional)
3. Enter version number for release
4. Click **Run workflow**

---

## 📦 Plugin Store

Plugins are distributed via the built-in plugin store. The store metadata is fetched from:

```
https://raw.githubusercontent.com/Crs10259/LenovoLegionToolkit-Plugins/main/store.json
```

### Store Schema

```json
{
  "plugins": [
    {
      "id": "plugin-name",
      "name": "Plugin Name",
      "version": "1.0.0",
      "description": "Plugin description",
      "author": "Author Name",
      "downloadUrl": "https://github.com/.../plugin-v1.0.0.zip",
      "minimumHostVersion": "2.14.0",
      "icon": "Apps24",
      "iconBackground": "#0078D4"
    }
  ],
  "lastUpdated": "2026-02-06T00:00:00Z",
  "storeVersion": "1.0.0"
}
```

---

## 🤝 Contributing

1. **Fork** this repository
2. **Create** a feature branch: `git checkout -b feature/new-plugin`
3. **Develop** your plugin following our [guidelines](docs/PLUGIN_DEVELOPMENT.md)
4. **Test** your plugin: `make.bat YourPlugin`
5. **Submit** a pull request

### Contribution Guidelines

- Follow C# coding conventions
- Include XML documentation
- Add localization support (English + Chinese)
- Write unit tests for core functionality
- Update `store.json` with plugin metadata

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/Crs10259/LenovoLegionToolkit-Plugins/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Crs10259/LenovoLegionToolkit-Plugins/discussions)
- **Main Project**: [Lenovo Legion Toolkit](https://github.com/Crs10259/LenovoLegionToolkit)

---

**Built with ❤️ for the Lenovo Legion Community**
