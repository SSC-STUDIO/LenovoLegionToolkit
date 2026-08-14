# 插件开发指南

本文档详细介绍如何为 Universal Device Toolkit（原 Lenovo Legion Toolkit）开发插件。

> **官方插件与宿主集成文档统一维护在主仓库中**
>
> 贡献者直接在本仓库的 [Plugins/Official](../../Plugins/Official) 中创建或修改插件。作者工作流请参阅 [PLUGIN_QUICKSTART.md](./PLUGIN_QUICKSTART.md) 与 [PLUGIN_DEVELOPMENT.md](./PLUGIN_DEVELOPMENT.md)。
>
> **本文档**聚焦于宿主侧接口契约、插件生命周期、以及 Electron `contributes.webPage` + Host RPC。WPF UserControl 已不是现行 UI。

## 目录

- [概述](#概述)
- [快速开始](#快速开始)
- [插件接口](#插件接口)
- [生命周期](#生命周期)
- [UI 扩展](#ui-扩展)
- [配置存储](#配置存储)
- [国际化](#国际化)
- [最佳实践](#最佳实践)
- [示例插件](#示例插件)

---

## 概述

Universal Device Toolkit 支持通过插件系统扩展功能。插件可以：

- 通过 `contributes.webPage` 提供 Electron 设置/功能页
- 集成到 Windows 优化功能
- 经 Host JSON-RPC（`pluginHost.invoke`）调用插件 C# API
- 访问主程序已暴露的 bridge 方法（含 `dialog:*`）

### 插件类型


| 类型       | 说明            | 可卸载 |
| -------- | ------------- | --- |
| **功能插件** | 提供独立功能模块      | ✅   |
| **系统插件** | 核心功能扩展，随主程序启动 | ❌   |


---

## 快速开始

以下步骤在本仓库的 [Plugins/Official](../../Plugins/Official) 中执行。插件项目不应添加到 `UniversalDeviceToolkit.Lib` 的 `ProjectReference`；插件通过 `UniversalDeviceToolkit.Plugins.SDK.dll` 引用宿主契约。

> **宿主 ABI（Phase 3 硬切换后）**：核心程序集与命名空间为 `UniversalDeviceToolkit.Lib` / `UniversalDeviceToolkit.Lib.Plugins`。新插件程序集应使用 `UniversalDeviceToolkit.Plugins.*`；宿主过渡期仍接受旧前缀 `LenovoLegionToolkit.Plugins.*`。完整清单与遗留兼容面见 [NamespaceMigration.md](../NamespaceMigration.md)。

### 1. 初始化插件

在主仓库的 `Plugins/` 目录内使用脚手架命令（模板示例：`settings-only`、`feature-settings`、`runtime-optimization`）：

```powershell
.\llt-plugin.cmd init `
  --template feature-settings `
  --folder MyPlugin `
  --id my-plugin `
  --name "My Plugin"
```

这会生成 `Plugins/Official/MyPlugin/`、测试项目、`plugin.manifest.json`、兼容输出的 `plugin.json` 与资源文件。完整作者流程见 [PLUGIN_QUICKSTART.md](./PLUGIN_QUICKSTART.md)。

### 2. 创建插件类

插件类继承 `UniversalDeviceToolkit.Plugins.SDK.PluginBase`，并使用 `using UniversalDeviceToolkit.Plugins.SDK;`。与官方插件一致的最小示例：

```csharp
using UniversalDeviceToolkit.Plugins.SDK;

namespace UniversalDeviceToolkit.Plugins.MyPlugin;

[Plugin(
    id: "my-plugin",
    name: "My Plugin",
    version: "1.0.0",
    description: "A sample plugin",
    author: "Your Name",
    MinimumHostVersion = "3.6.1",
    Icon = "Apps24"
)]
public class MyPlugin : PluginBase
{
    public override string Id => "my-plugin";
    public override string Name => MyPluginText.PluginName;
    public override string Description => MyPluginText.PluginDescription;
    public override string Icon => "Apps24";
    public override bool IsSystemPlugin => false;

    public override object? GetFeatureExtension()
        => new MyPluginFeaturePage();

    public override object? GetSettingsPage()
        => new MyPluginSettingsPage();

    public override void OnShutdown() => Stop();
    public override void Stop() { /* 停止后台服务 */ }
}

public sealed class MyPluginFeaturePage : IPluginPage
{
    public string PageTitle => MyPluginText.PageTitle;
    public string? PageIcon => "Apps24";
    public object CreatePage() => new MyFeatureControl();
}

public sealed class MyPluginSettingsPage : IPluginPage
{
    public string PageTitle => MyPluginText.SettingsPageTitle;
    public string? PageIcon => "Settings24";
    public object CreatePage() => new MySettingsControl();
}
```

仅设置页、无功能页的插件（如 `custom-mouse`）可令 `GetFeatureExtension()` 返回 `null`；仅功能页的插件可省略 `GetSettingsPage()`。

### 3. 插件元数据

`**plugin.manifest.json` 是作者侧单一事实来源**；构建时会生成宿主兼容的 `plugin.json`。使用 `minHostVersion`（不是旧字段 `minLLTVersion`）：

```json
{
  "schemaVersion": 1,
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "minHostVersion": "3.6.1",
  "author": "Your Name",
  "isSystemPlugin": false,
  "repository": "https://github.com/yourname/my-plugin",
  "issues": "https://github.com/yourname/my-plugin/issues",
  "contributes": {
    "featurePage": null,
    "settingsPage": null,
    "webPage": {
      "entry": "web/index.html"
    },
    "runtime": null,
    "optimizationActions": []
  }
}
```

`[Plugin]` 特性中的 `MinimumHostVersion` 应与 `minHostVersion` 保持一致。

---

## 插件接口

### IPlugin 接口

所有插件必须实现 `IPlugin` 接口：

```csharp
public interface IPlugin
{
    string Id { get; }              // 唯一标识符
    string Name { get; }            // 显示名称
    string Description { get; }     // 描述
    string Icon { get; }            // 图标名称
    bool IsSystemPlugin { get; }    // 是否系统插件
    string[]? Dependencies { get; } // 依赖的其他插件ID

    void OnInstalled();    // 安装后回调
    void OnUninstalled();  // 卸载前回调
    void OnShutdown();     // 应用关闭时回调
    void Stop();           // 停止运行中的进程
}
```

### PluginBase 基类

插件作者应继承 `UniversalDeviceToolkit.Plugins.SDK.PluginBase`（下文为宿主侧等价的抽象基类定义；类型由 `UniversalDeviceToolkit.Plugins.SDK.dll` 提供）：

```csharp
public abstract class PluginBase : IPlugin
{
    // 必须实现的成员
    public abstract string Id { get; }
    public abstract string Name { get; }
    
    // 可选重写的成员
    public virtual string Description => string.Empty;
    public virtual string Icon => "Apps24";
    public virtual bool IsSystemPlugin => false;
    public virtual string[]? Dependencies => null;
    
    // 扩展点
    public virtual object? GetFeatureExtension() => null;
    public virtual object? GetSettingsPage() => null;
    public virtual WindowsOptimizationCategoryDefinition? GetOptimizationCategory() => null;
    
    // 生命周期方法
    public virtual void OnInstalled() { }
    public virtual void OnUninstalled() { }
    public virtual void OnShutdown() { }
    public virtual void Stop() { }
}
```

---

## 生命周期

插件生命周期流程：

```
┌─────────────────────────────────────────────────────────────┐
│                      插件生命周期                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. 扫描阶段                                                 │
│     └── PluginManager.ScanAndLoadPlugins()                  │
│         └── 发现插件程序集                                   │
│         └── 创建插件实例                                     │
│                                                             │
│  2. 注册阶段                                                 │
│     └── PluginManager.RegisterPlugin()                      │
│         └── 添加到插件注册表                                 │
│                                                             │
│  3. 安装阶段                                                 │
│     └── PluginManager.InstallPlugin()                       │
│         └── OnInstalled() ← 在此初始化资源                   │
│                                                             │
│  4. 运行阶段                                                 │
│     └── contributes.webPage → Electron <webview>            │
│     └── pluginHost.invoke → Host plugin.* RPC               │
│     └── GetOptimizationCategory() → 系统优化动作（可选）    │
│                                                             │
│  5. 卸载阶段                                                 │
│     └── Stop() ← 停止后台服务                               │
│     └── OnUninstalled() ← 清理资源                          │
│                                                             │
│  6. 关闭阶段                                                 │
│     └── OnShutdown() ← 应用关闭时调用                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## UI 扩展

Shipping UI 是 Electron。插件不要再提供 WPF `UserControl` / `IPluginPage.CreatePage()` 作为现行页面。`GetFeatureExtension()` / `GetSettingsPage()` 对官方插件返回 `null`。

### contributes.webPage

在 `plugin.manifest.json` 声明入口，并把 `web/` 复制进插件包（csproj `Content Include="web\**\*"`）：

```json
"webPage": { "entry": "web/index.html" }
```

`plugins.list` 对已安装插件返回 `directory`（包根目录）和 `webPage`（相对入口）。Electron `PluginPageView` 用 `file://` 加载该页，guest preload 注入 `window.pluginHost`。

### pluginHost.invoke

```html
<link rel="stylesheet" href="plugin-ui.css" />
<script>
  const bridge = window.pluginHost
  const state = await bridge.invoke('plugin.customMouse.getState', {})
  bridge.on('plugin.vive.downloadProgress', (data) => { /* ... */ })
</script>
```

- 与主窗口同一套 invoke 路径，包括 Electron 本地 `dialog:open-file` / `dialog:save-file` / `dialog:select-folder` / `dialog:open-path`。
- 官方三插件不要用 `plugins.getConfig` / `plugins.setConfig`（那是插件目录 `config.json`，不是 live AppData store）。
- 业务仍在插件 C#；Host 通过 `IPluginManager.TryGetPlugin` 调已加载实例。

### 官方 RPC 域

| 插件 | 方法前缀 | 用途 |
| --- | --- | --- |
| CustomMouse | `plugin.customMouse.*` | 指针速度、交换键、光标主题 |
| ShellIntegration | `plugin.shell.*` | Nilesoft Shell 启用/预设/导入导出 |
| ViveTool | `plugin.vive.*` | 特性表、路径、下载 |

### 视觉规范

复用 `web/plugin-ui.css`（`up-*` 类：卡片、行、开关、按钮）。文案用插件 resx 或页面内英文+中文，不要另开一套 i18n。

`IPluginPage` 仍留在 SDK 里（冻结 ABI / 历史别名），新插件不要实现它来承载 UI。

---

## 配置存储

### 外部插件（贡献者路径）

官方与第三方插件通常使用 **插件本地 JSON 持久化**，而非宿主 `ApplicationSettings`：

- `**PluginBase.Configuration`**（`IPluginConfiguration`）：SDK 提供的按插件作用域键值存储，默认落在宿主 app-data 下的插件配置目录；适合简单设置（参见 `custom-mouse` 的 `Configuration.SetValue` / `SaveAsync` 模式）。
- **插件仓库 `Plugins/Shared` 中的 `SettingsManager` 等辅助类**：适合结构化设置模型与文件级 JSON 读写（各官方插件的 `*Settings` 类普遍采用此模式）。

贡献者不应引用宿主内部的 `IoCContainer` 或 `ApplicationSettings`。

### 宿主内建插件（非贡献者路径）

极少数与主程序同进程编译的内建插件可能通过 `ApplicationSettings` 读写设置；这不是插件仓库的贡献路径，第三方作者请忽略。

---

## 国际化

### 创建资源文件

```
Resources/
├── Resource.resx           # 默认（英语）
├── Resource.zh-Hans.resx   # 简体中文
├── Resource.ja.resx        # 日语
└── ...
```

### 资源文件示例

```xml
<!-- Resource.resx -->
<data name="PluginName" xml:space="preserve">
  <value>My Plugin</value>
</data>
<data name="PluginDescription" xml:space="preserve">
  <value>A sample plugin for Universal Device Toolkit</value>
</data>
```

```xml
<!-- Resource.zh-Hans.resx -->
<data name="PluginName" xml:space="preserve">
  <value>我的插件</value>
</data>
<data name="PluginDescription" xml:space="preserve">
  <value>Universal Device Toolkit 示例插件</value>
</data>
```

### 在代码中使用

```csharp
public class MyPlugin : PluginBase
{
    public override string Name => Resource.PluginName;
    public override string Description => Resource.PluginDescription;
}
```

---

## Windows 优化集成

插件可以提供 Windows 优化分类：

```csharp
public override WindowsOptimizationCategoryDefinition? GetOptimizationCategory()
{
    var actions = new List<WindowsOptimizationActionDefinition>
    {
        new(
            id: "my-plugin.optimize-feature",
            name: Resource.OptimizeFeatureName,
            description: Resource.OptimizeFeatureDescription,
            action: async ct =>
            {
                // 执行优化操作
                await Task.Delay(100, ct);
            },
            recommended: true,
            isAppliedAsync: async ct =>
            {
                // 检查是否已应用
                return await CheckIfAppliedAsync();
            }
        )
    };

    return new WindowsOptimizationCategoryDefinition(
        id: "my-plugin.category",
        name: Resource.CategoryName,
        description: Resource.CategoryDescription,
        actions: actions,
        pluginId: Id
    );
}
```

### Electron 插件页验证

官方插件通过 `contributes.webPage` 提供设置 UI。Electron 用 `<webview>` 加载 `web/index.html`，页面经 `window.pluginHost.invoke` 调用 Host `plugin.*` RPC。WPF `GetSettingsPage` / `MainAppPluginUi.Smoke` 已退役。

验证：

1. `plugins.list` 为已安装项返回 `directory` 与 `webPage`。
2. 页面调用的方法已在 Host `PluginOfficialHandlers` 注册（CustomMouse / Shell / Vive 分别为 `plugin.customMouse.*` / `plugin.shell.*` / `plugin.vive.*`）。
3. 契约测试：`UniversalDeviceToolkit.Electron/tests/pluginOfficialContract.test.mjs` 与 `UniversalDeviceToolkit.Tests.Contracts` 中的官方插件 RPC/manifest Guard（名单：`Plugins/Official/plugin-rpc-contract.json`）。

---

## 最佳实践

### 1. 命名规范


| 项目    | 规范               | 示例                 |
| ----- | ---------------- | ------------------ |
| 插件 ID | 小写字母、数字、连字符      | `my-plugin`        |
| 插件类   | `{Name}Plugin`   | `MyPlugin`         |
| 页面类   | `{Name}Page`     | `MyFeaturePage`    |
| 设置类   | `{Name}Settings` | `MyPluginSettings` |


### 2. 资源管理

```csharp
public class MyPlugin : PluginBase
{
    private CancellationTokenSource? _cts;
    
    public override void OnInstalled()
    {
        _cts = new CancellationTokenSource();
        StartBackgroundService(_cts.Token);
    }
    
    public override void Stop()
    {
        _cts?.Cancel();
    }
    
    public override void OnUninstalled()
    {
        _cts?.Dispose();
        _cts = null;
    }
}
```

### 3. 错误处理

```csharp
public override void OnInstalled()
{
    try
    {
        InitializePlugin();
    }
    catch (Exception ex)
    {
        Log.Instance.Error($"Plugin initialization failed: {ex.Message}");
        // 不要抛出异常，记录日志即可
    }
}
```

### 4. 版本兼容性

在 `plugin.manifest.json` 中声明 `minHostVersion`，并与 `[Plugin]` 特性的 `MinimumHostVersion` 保持一致：

```json
{
  "minHostVersion": "3.6.1"
}
```

```csharp
[Plugin(
    id: "my-plugin",
    MinimumHostVersion = "3.6.1"  // 与 plugin.manifest.json 一致
)]
public class MyPlugin : PluginBase { }
```

---

## 示例插件

完整实现见主仓的 [Plugins/Official](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/tree/master/Plugins/Official) 目录：


| 插件 ID                    | 页面模型             | 说明                                                 |
| ------------------------ | ---------------- | -------------------------------------------------- |
| **custom-mouse**         | `webPage` + Windows 优化 | `web/index.html`；Host `plugin.customMouse.*` |
| **shell-integration**    | `webPage`（系统插件）  | `isSystemPlugin: true`；Host `plugin.shell.*` |
| **vive-tool**            | `webPage`        | 特性表 + 路径/下载；Host `plugin.vive.*` |


---

## 调试插件

### 推荐：主仓库插件工具链

日常开发应在主仓库的 `Plugins/` 目录使用 `udt-plugin.cmd` 构建，再用 Electron 打开插件 web 页：

```powershell
.\udt-plugin.cmd build --plugin my-plugin
```

然后 `npm run dev`（Electron）或 `npm run dev:web`，从插件列表进入带 `contributes.webPage` 的页面。`pluginHost.invoke` 调用 Host `plugin.*` RPC。

### 备选：复制到宿主 plugins 目录

调试宿主加载行为时，可将打包输出手动复制到主程序 `plugins` 目录：

- 开发环境：`Plugins/.build/plugins/`
- 安装环境：`%LOCALAPPDATA%/UniversalDeviceToolkit/plugins/`

优先使用 `.\llt-plugin.cmd package --plugin my-plugin --build-first` 生成 ZIP，再解压或复制到上述目录。

---

## 发布插件

### 打包

在主仓库的 `Plugins/` 目录：

```powershell
.\llt-plugin.cmd package --plugin my-plugin --build-first
.\llt-plugin.cmd validate --plugin my-plugin --profile contributor
```

输出 ZIP 包含插件程序集、`UniversalDeviceToolkit.Plugins.SDK.dll`、生成的 `plugin.json`、`plugin.manifest.json` 与资源文件。

### 提交插件改动

1. 从主仓库创建分支并修改 [Plugins/Official](../../Plugins/Official)
2. 使用 `llt-plugin.cmd init` 在 `Plugins/` 下创建插件，或扩展现有插件
3. 通过 `validate` 与 `workbench-smoke` 后提交 Pull Request

---

## 相关文档

- [Plugins/Official — PLUGIN_QUICKSTART.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/Docs/Plugins/PLUGIN_QUICKSTART.md) - 插件作者快速开始
- [Plugins/Official — PLUGIN_DEVELOPMENT.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/Docs/Plugins/PLUGIN_DEVELOPMENT.md) - 插件完整开发指南
- [Plugins/Official — BUILD_SMOKE.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/Docs/Plugins/BUILD_SMOKE.md) - 构建与视觉冒烟
- [ARCHITECTURE.md](./ARCHITECTURE.md) - 插件系统架构
- [CONTRIBUTING.md](../../CONTRIBUTING.md) - 主程序贡献指南
- [AGENTS.md](../../AGENTS.md) - 开发者指南
