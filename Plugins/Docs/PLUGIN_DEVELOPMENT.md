# Lenovo Legion Toolkit - Plugin Development Guide / 插件开发指南

This guide provides comprehensive information for developing plugins for Lenovo Legion Toolkit.

---

## Table of Contents / 目录

1. [Quick Start / 快速开始](#quick-start)
2. [Project Structure / 项目结构](#project-structure)
3. [Core Concepts / 核心概念](#core-concepts)
4. [Plugin Interface / 插件接口](#plugin-interface)
5. [Creating a New Plugin / 创建新插件](#creating-a-new-plugin)
6. [Best Practices / 最佳实践](#best-practices)
7. [Testing / 测试](#testing)
8. [Publishing / 发布](#publishing)

---

## Quick Start / 快速开始

### Prerequisites / 前置条件

- .NET 10.0 SDK or later
- Windows 10/11
- Visual Studio 2022 or VS Code

### Clone and Build / 克隆和构建

```bash
# Clone the plugins repository
git clone https://github.com/Crs10259/LenovoLegionToolkit-Plugins.git
cd LenovoLegionToolkit-Plugins

# Build all plugins
make.bat

# Build specific plugin
make.bat MyPlugin
```

---

## Project Structure / 项目结构

```
LenovoLegionToolkit-Plugins/
├── Plugins/                         # Plugin source code
│   ├── SDK/                        # Plugin SDK (development dependency)
│   ├── template/                   # Plugin template for new plugins
│   ├── src/Common/                 # Shared code for all plugins
│   ├── CustomMouse/
│   ├── NetworkAcceleration/
│   ├── ShellIntegration/
│   └── ViveTool/
├── docs/                           # Documentation
├── store.json                      # Plugin store metadata
└── README.md
```

---

## Core Concepts / 核心概念

### Plugin Architecture / 插件架构

Lenovo Legion Toolkit uses a **plugin-based architecture** where:

1. **Host Application**: The main Lenovo Legion Toolkit application
2. **Plugin SDK**: Provides interfaces and base classes for plugins
3. **Plugins**: Independent modules that extend functionality

### Plugin Types / 插件类型

- **Feature Plugins**: Add new features to the application
- **Utility Plugins**: Provide utility functions
- **System Plugins**: Integrate with system features (marked with `IsSystemPlugin = true`)

---

## Plugin Interface / 插件接口

### IPlugin Interface / IPlugin 接口

Every plugin must implement the `IPlugin` interface:

```csharp
public interface IPlugin
{
    // Required properties / 必需属性
    string Id { get; }                    // Unique plugin identifier
    string Name { get; }                  // Display name
    string Description { get; }           // Plugin description
    string Icon { get; }                  // Fluent UI icon name
    bool IsSystemPlugin { get; }          // System plugin flag

    // Optional properties / 可选属性
    string[]? Dependencies { get; }       // Plugin dependencies

    // Lifecycle methods / 生命周期方法
    void OnInstalled();                   // Called after installation
    void OnUninstalled();                 // Called before uninstallation
    void OnShutdown();                    // Called on application shutdown
    void Stop();                         // Called before update/uninstall
}
```

### PluginBase Class / PluginBase 基类

For convenience, use the `PluginBase` class:

```csharp
[Plugin(
    id: "myplugin",
    name: "My Plugin",
    version: "1.0.0",
    description: "My custom plugin",
    author: "Your Name",
    MinimumHostVersion = "2.0.0",
    Icon = "Apps24"
)]
public class MyPlugin : PluginBase
{
    public override string Id => "myplugin";
    public override string Name => "My Plugin";
    // ... implement other properties
}
```

### PluginPage Interface / PluginPage 接口

Provide UI for your plugin:

```csharp
public class MyPluginPage : IPluginPage
{
    public string PageTitle => "My Page";
    public string? PageIcon => "Apps24";

    public object CreatePage()
    {
        return new MyPluginControl();
    }
}
```

---

## Creating a New Plugin / 创建新插件

### Method 1: Using the Template / 方法一：使用模板

1. Copy the template directory:
   ```bash
   cp -r plugins/template plugins/MyPlugin
   ```

2. Rename files:
   - `LenovoLegionToolkit.Plugins.Template.csproj` → `LenovoLegionToolkit.Plugins.MyPlugin.csproj`
   - `MyPluginTemplate.cs` → `MyPlugin.cs`
   - etc.

3. Update the project file with your plugin ID and name

4. Implement the plugin logic

### Method 2: From Scratch / 方法二：从头创建

1. Create the project:
   ```bash
   dotnet new classlib -n LenovoLegionToolkit.Plugins.MyPlugin -o ./plugins/MyPlugin
   ```

2. Update the project file:
   ```xml
   <PropertyGroup>
     <TargetFramework>net10.0-windows</TargetFramework>
     <UseWPF>true</UseWPF>
   </PropertyGroup>

   <ItemGroup>
     <ProjectReference Include="..\SDK\LenovoLegionToolkit.Plugins.SDK.csproj" />
   </ItemGroup>
   ```

3. Create the plugin class

4. Add UI pages if needed

### Required Files / 必需文件

```
plugins/MyPlugin/
├── MyPlugin.cs                  # Main plugin class
├── MyPluginPage.xaml            # Feature page (optional)
├── MyPluginPage.xaml.cs
├── MyPluginSettingsPage.xaml    # Settings page (optional)
├── MyPluginSettingsPage.xaml.cs
├── Resources/
│   ├── Resource.resx            # English resources
│   └── Resource.zh-hans.resx    # Chinese (Simplified) resources
└── CHANGELOG.md
```

---

## Best Practices / 最佳实践

### 1. Follow Naming Conventions / 遵循命名规范

```csharp
// Plugin class
public class MyPlugin : PluginBase { }

// UI pages
public class MyPluginPage : IPluginPage { }
public class MyPluginSettingsPage : IPluginPage { }

// UI controls
public partial class MyPluginControl : UserControl { }
```

### 2. Use Dependency Injection / 使用依赖注入

Access main application services:

```csharp
public class MyPlugin : PluginBase
{
    private readonly ILogger<MyPlugin> _logger;
    private readonly ISettingsService _settingsService;

    public MyPlugin(
        ILogger<MyPlugin> logger,
        IPluginManager pluginManager)
    {
        _logger = logger;
        _settingsService = pluginManager;
    }
}
```

### 3. Proper Resource Management / 正确的资源管理

```csharp
public override void OnShutdown()
{
    // Clean up resources
    _service?.Dispose();
    base.OnShutdown();
}
```

### 4. Error Handling / 错误处理

```csharp
public override void OnInstalled()
{
    try
    {
        // Installation logic
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to install plugin");
        throw;
    }
}
```

### 5. Localization / 本地化

Add strings to both `Resource.resx` and `Resource.zh-hans.resx`:

```xml
<!-- Resource.resx -->
<data name="MyPlugin_Description" xml:space="preserve">
    <value>My plugin description</value>
</data>

<!-- Resource.zh-hans.resx -->
<data name="MyPlugin_Description" xml:space="preserve">
    <value>我的插件描述</value>
</data>
```

---

## Testing / 测试

### Unit Testing / 单元测试

```csharp
public class MyPluginTests
{
    [Fact]
    public void PluginId_ShouldBeCorrect()
    {
        var plugin = new MyPlugin();
        Assert.Equal("myplugin", plugin.Id);
    }
}
```

### Integration Testing / 集成测试

Test with the actual Lenovo Legion Toolkit application.

---

## Publishing / 发布

### 1. Update store.json

Add your plugin to `store.json`:

```json
{
  "icon": "Apps24",
  "iconBackground": "#0078D4",
  "name": "MyPlugin",
  "downloadUrl": "https://github.com/Crs10259/LenovoLegionToolkit-Plugins/releases/download/MyPlugin-v1.0.0/MyPlugin.zip",
  "version": "1.0.0",
  "changelog": "Version 1.0.0 (2026-02-05)\n- Initial release",
  "author": "Your Name",
  "id": "myplugin",
  "minimumHostVersion": "2.0.0"
}
```

### 2. Create Release

1. Build the plugin: `make.bat MyPlugin`
2. Create ZIP package: `make.bat zip`
3. Create GitHub Release with the ZIP file

### 3. Submit PR

Submit a pull request to the main repository.

---

## Common Issues / 常见问题

### Q: Plugin not loading?
A: Check the plugin ID matches the directory name and verify all DLLs are present.

### Q: UI not showing?
A: Ensure `GetFeatureExtension()` returns a valid `IPluginPage`.

### Q: Build errors?
A: Verify SDK and main project references are correct.

---

## Resources / 资源

- [Plugin SDK Source](../SDK/)
- [Existing Plugins](../Plugins/)
- [Main Application](https://github.com/Crs10259/LenovoLegionToolkit)
- [Plugin Repository](https://github.com/Crs10259/LenovoLegionToolkit-Plugins)
- [Issues](https://github.com/Crs10259/LenovoLegionToolkit-Plugins/issues)

---

*Last updated: 2026-02-09*
