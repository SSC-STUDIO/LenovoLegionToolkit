# Lenovo Legion Toolkit Plugins

This repository contains official plugins for Lenovo Legion Toolkit (LLT).

## Available Plugins

### Custom Mouse
**ID:** `custom-mouse`  
**Version:** 1.0.0  
**Author:** LenovoLegionToolkit Team

Customize mouse settings including DPI, polling rate, and button mappings.

**Features:**
- DPI adjustment (100-16000)
- Polling rate control (125Hz, 250Hz, 500Hz, 1000Hz)
- Button remapping
- State persistence across sessions

**Permissions:**
- SystemInformation
- HardwareAccess

---

### Shell Integration
**ID:** `shell-integration`  
**Version:** 1.0.0  
**Author:** LenovoLegionToolkit Team

Integrate Lenovo Legion Toolkit with Windows shell context menu.

**Features:**
- Right-click context menu integration
- Quick mode switching
- Direct launcher access

**Permissions:**
- SystemInformation
- RegistryRead
- RegistryWrite

**Note:** This is a system plugin and cannot be uninstalled by users.

---

## Development

### Prerequisites
- .NET 10.0 SDK
- Visual Studio 2022 or VS Code

### Building
```bash
# Build all plugins
dotnet build LenovoLegionToolkit-Plugins.sln

# Build specific plugin
dotnet build plugins/CustomMouse/CustomMouse.csproj

# Build in Release mode
dotnet build -c Release
```

### Creating a New Plugin

1. Create a new folder under `plugins/`
2. Create a `.csproj` file referencing the SDK
3. Implement the `IPlugin` interface
4. Add a `plugin.json` manifest file

Example plugin structure:
```
plugins/
└── MyPlugin/
    ├── MyPlugin.csproj
    ├── MyPlugin.cs
    └── plugin.json
```

### Plugin Manifest (plugin.json)

```json
{
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Plugin description",
  "minLLTVersion": "3.6.0",
  "isSystemPlugin": false,
  "dependencies": [],
  "permissions": ["SystemInformation"],
  "entryPoint": "Namespace.ClassName",
  "stateful": false
}
```

### Available Permissions

- `None` - No special permissions
- `FileSystemRead` - Read file system
- `FileSystemWrite` - Write file system
- `NetworkAccess` - Network access
- `RegistryRead` - Read registry
- `RegistryWrite` - Write registry
- `SystemInformation` - Access system info
- `HardwareAccess` - Hardware control
- `UICustomization` - UI customization
- `InterPluginCommunication` - Inter-plugin communication

## CI/CD

This repository uses GitHub Actions for:

- **Build:** Automatically builds all plugins on push/PR
- **Test:** Runs unit and integration tests
- **Release:** Creates releases from git tags

### Tag Format

- `v1.0.0` - Release all plugins with version 1.0.0
- `CustomMouse-v1.0.0` - Release specific plugin

## License

MIT License - See LICENSE file for details
