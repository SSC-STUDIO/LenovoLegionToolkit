# Lenovo Legion Toolkit 插件开发与提交指南

本文档介绍如何为 Lenovo Legion Toolkit 开发插件并将它们提交到在线插件商店。

## 目录

- [插件系统概述](#插件系统概述)
- [开发新插件](#开发新插件)
- [插件结构](#插件结构)
- [提交插件到商店](#提交插件到商店)
- [自动发布工作流](#自动发布工作流)
- [插件更新](#插件更新)

## 插件系统概述

Lenovo Legion Toolkit (LLT) 支持插件系统，允许动态扩展应用程序功能。插件可以从在线商店下载安装，无需重启应用程序。

### 核心特性

- **动态加载**：插件从 `build/plugins` 目录在运行时加载
- **依赖管理**：自动安装和检查插件依赖
- **UI 集成**：插件可以提供自定义 UI 页面和设置
- **功能扩展**：插件可以扩展现有功能或添加新功能
- **在线安装**：支持从 GitHub 仓库在线下载和安装插件

## 开发新插件

### 1. 创建插件项目

在 `LenovoLegionToolkit.Plugins` 目录下创建新的插件项目：

```bash
dotnet new classlib -n LenovoLegionToolkit.Plugins.MyPlugin -f net8.0-windows
```

### 2. 添加项目引用

编辑插件项目的 `.csproj` 文件，添加必要的引用：

```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\LenovoLegionToolkit.Lib\LenovoLegionToolkit.Lib.csproj" />
    <ProjectReference Include="..\LenovoLegionToolkit.Plugins.SDK\LenovoLegionToolkit.Plugins.SDK.csproj" />
  </ItemGroup>
</Project>
```

### 3. 实现插件类

创建插件类，实现 `IPlugin` 接口：

```csharp
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;

[assembly: Plugin(
    Name = "My Plugin",
    Author = "Your Name",
    Version = "1.0.0",
    MinimumHostVersion = "2.0.0"
)]

namespace LenovoLegionToolkit.Plugins.MyPlugin;

public class MyPlugin : PluginBase
{
    public override string Id => "MyPlugin";
    public override string Name => "My Plugin";
    public override string Description => "A description of my plugin functionality";
    public override string Icon => "Settings24";
    public override bool IsSystemPlugin => false;
    public override string[]? Dependencies => null;

    public override void OnInstalled()
    {
        // Plugin installed callback
    }

    public override void OnUninstalled()
    {
        // Plugin uninstalled callback
    }

    public override void OnShutdown()
    {
        // Application shutdown callback
    }

    public override IPluginFeatureExtension? GetFeatureExtension()
    {
        // Return plugin feature extension if needed
        return null;
    }
}
```

### 4. 可选：添加 UI 页面

如果插件需要 UI 页面，实现 `IPluginPage` 接口：

```csharp
using LenovoLegionToolkit.Plugins.SDK;

public class MyPluginPage : IPluginPage
{
    public string Title => "My Plugin";
    public string Symbol => "Settings24";
    
    public UserControl GetPage() => new MyPluginPageControl();
}
```

## 插件结构

插件应包含以下文件结构：

```
build/plugins/MyPlugin/
├── MyPlugin.dll              # 主插件程序集
├── LenovoLegionToolkit.Lib.dll
├── LenovoLegionToolkit.Plugins.SDK.dll
├── zh-hans/                  # 可选：本地化资源
│   └── MyPlugin.resources.dll
└── en/
    └── MyPlugin.resources.dll
```

## 提交插件到商店

### 方法 1：使用 GitHub Actions（推荐）

1. 触发 `publish-plugin` 工作流：
   - 访问 GitHub Actions 页面
   - 选择 "Publish Plugins" 工作流
   - 输入插件名称和版本号
   - 填写更新日志
   - 启动工作流

2. 工作流将自动：
   - 构建插件
   - 创建 ZIP 包
   - 计算 SHA256 哈希
   - 创建 GitHub Release
   - 更新 `plugins/store.json`

### 方法 2：手动提交

1. 构建插件：
   ```bash
   dotnet build LenovoLegionToolkit.Plugins.MyPlugin -c Release
   ```

2. 创建插件 ZIP 包（包含所有依赖 DLL）

3. 计算 SHA256 哈希：
   ```powershell
   (Get-FileHash MyPlugin.zip -Algorithm SHA256).Hash.ToLower()
   ```

4. 手动创建 GitHub Release

5. 更新 `plugins/store.json`：
   ```json
   {
     "id": "MyPlugin",
     "name": "My Plugin",
     "description": "Plugin description",
     "icon": "Settings24",
     "author": "Your Name",
     "version": "1.0.0",
     "minimumHostVersion": "2.0.0",
     "downloadUrl": "https://github.com/BartoszCiczek/LenovoLegionToolkit/releases/download/plugins/MyPlugin.zip",
     "fileHash": "your_sha256_hash",
     "fileSize": 12345,
     "releaseDate": "2024-01-01T00:00:00Z",
     "changelog": "Initial release",
     "tags": ["utility"],
     "isSystemPlugin": false
   }
   ```

## 自动发布工作流

项目包含了 GitHub Actions 工作流 `.github/workflows/publish-plugin.yml`，支持自动化的插件发布流程。

### 工作流参数

| 参数 | 描述 | 必需 |
|------|------|------|
| `plugin_name` | 插件名称（项目名称） | 是 |
| `version` | 版本号（如 1.0.0） | 是 |
| `changelog` | 更新日志 | 否 |

### 工作流步骤

1. **构建插件**：使用 .NET 8 构建插件项目
2. **创建插件包**：打包插件及其依赖
3. **计算哈希**：生成 SHA256 哈希用于验证
4. **创建 Release**：发布到 GitHub Releases
5. **更新商店**：自动更新 `plugins/store.json`

## 插件更新

### 小版本更新（向后兼容）

1. 更新插件代码
2. 增加版本号（如 1.0.0 -> 1.0.1）
3. 触发发布工作流
4. 用户将在插件商店看到更新提示

### 大版本更新（可能不兼容）

1. 更新插件代码
2. 增加版本号（如 1.0.0 -> 2.0.0）
3. 更新 `minimumHostVersion` 如果需要
4. 在更新日志中说明不兼容变更
5. 触发发布工作流

### 插件商店 JSON 格式

`plugins/store.json` 文件格式：

```json
{
  "plugins": [
    {
      "id": "PluginId",
      "name": "Plugin Name",
      "description": "Plugin description",
      "icon": "SymbolName",
      "author": "Author Name",
      "version": "1.0.0",
      "minimumHostVersion": "2.0.0",
      "dependencies": ["OtherPlugin"],
      "downloadUrl": "https://github.com/.../PluginId.zip",
      "fileHash": "sha256_hash",
      "fileSize": 12345,
      "releaseDate": "2024-01-01T00:00:00Z",
      "changelog": "Update notes",
      "tags": ["utility", "system"],
      "isSystemPlugin": false
    }
  ],
  "lastUpdated": "2024-01-01T00:00:00Z",
  "storeVersion": "1.0.0"
}
```

## 常见问题

### Q: 插件安装后不显示？

A: 确保插件 DLL 名称以 `LenovoLegionToolkit.Plugins.` 开头，并且插件类实现了 `IPlugin` 接口且有无参构造函数。

### Q: 插件依赖其他插件怎么办？

A: 在插件类的 `Dependencies` 属性中指定依赖的插件 ID，安装时会自动安装依赖。

### Q: 如何调试插件？

A: 在 Visual Studio 中设置启动项目为 `LenovoLegionToolkit.WPF`，然后调试运行。插件代码修改后重新构建即可生效。

### Q: 插件更新后用户需要重启吗？

A: 不需要。插件系统支持热重载，用户更新插件后刷新页面即可看到新功能。

## 相关链接

- [插件 SDK](../LenovoLegionToolkit.Plugins.SDK)
- [ViVeTool 插件示例](../LenovoLegionToolkit.Plugins.ViveTool)
- [Network Acceleration 插件示例](../LenovoLegionToolkit.Plugins.NetworkAcceleration)
