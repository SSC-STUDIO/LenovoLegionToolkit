# 插件开发指南

本文档详细介绍如何为 Universal Device Toolkit（原 Lenovo Legion Toolkit）开发插件。

> **官方插件开发在独立仓库中进行**
>
> 贡献者应克隆并工作在 [UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins)，而非在本主仓库内创建插件项目。作者工作流请参阅该仓库的 [Docs/PLUGIN_QUICKSTART.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/Docs/PLUGIN_QUICKSTART.md) 与 [Docs/PLUGIN_DEVELOPMENT.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/Docs/PLUGIN_DEVELOPMENT.md)。
>
> **本文档**聚焦于宿主侧接口契约、插件生命周期、以及插件 UI 应与主程序对齐的视觉规范；供理解宿主如何加载插件，也供在插件仓库中实现页面时对照宿主期望。

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

- 添加新的功能页面
- 集成到 Windows 优化功能
- 提供自定义设置界面
- 访问主程序的服务

### 插件类型


| 类型       | 说明            | 可卸载 |
| -------- | ------------- | --- |
| **功能插件** | 提供独立功能模块      | ✅   |
| **系统插件** | 核心功能扩展，随主程序启动 | ❌   |


---

## 快速开始

以下步骤在 [UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins) 仓库中执行。不要在本主仓库添加 `ProjectReference` 到 `UniversalDeviceToolkit.Lib`；插件通过 `LenovoLegionToolkit.Plugins.SDK.dll` 引用宿主契约。

### 1. 初始化插件

在插件仓库根目录使用脚手架命令（模板示例：`settings-only`、`feature-settings`、`runtime-optimization`）：

```powershell
.\llt-plugin.cmd init `
  --template feature-settings `
  --folder MyPlugin `
  --id my-plugin `
  --name "My Plugin"
```

这会生成 `Plugins/MyPlugin/`、测试项目、`plugin.manifest.json`、兼容输出的 `plugin.json` 与资源文件。完整作者流程见插件仓库 [PLUGIN_QUICKSTART.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/Docs/PLUGIN_QUICKSTART.md)。

### 2. 创建插件类

插件类继承 `LenovoLegionToolkit.Plugins.SDK.PluginBase`，并使用 `using LenovoLegionToolkit.Plugins.SDK;`。与官方插件一致的最小示例：

```csharp
using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.MyPlugin;

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
    "featurePage": {
      "class": "LenovoLegionToolkit.Plugins.MyPlugin.MyPluginFeaturePage",
      "title": "My Plugin"
    },
    "settingsPage": {
      "class": "LenovoLegionToolkit.Plugins.MyPlugin.MyPluginSettingsPage",
      "title": "My Plugin Settings"
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

插件作者应继承 `LenovoLegionToolkit.Plugins.SDK.PluginBase`（下文为宿主侧等价的抽象基类定义；类型由 `LenovoLegionToolkit.Plugins.SDK.dll` 提供）：

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
│     └── GetFeatureExtension() → 返回功能页面                │
│     └── GetSettingsPage() → 返回设置页面                    │
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

### IPluginPage 接口

UI 页面需要实现 `IPluginPage` 接口：

```csharp
public interface IPluginPage
{
    string PageTitle { get; }
    string? PageIcon { get; }
    object CreatePage();
}
```

### 功能扩展页面

```csharp
public class MyPluginFeaturePage : IPluginPage
{
    public string PageTitle => "My Feature";
    public string? PageIcon => "Apps24";
    
    public object CreatePage()
    {
        return new MyFeatureControl();
    }
}
```

### 设置页面

```csharp
public class MyPluginSettingsPage : IPluginPage
{
    public string PageTitle => "My Plugin Settings";
    public string? PageIcon => "Settings";
    
    public object CreatePage()
    {
        return new MySettingsControl();
    }
}
```

### WPF 控件示例

```xml
<!-- MyFeatureControl.xaml -->
<UserControl x:Class="MyPlugin.MyFeatureControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <TextBlock Text="Hello from My Plugin!" 
                   HorizontalAlignment="Center" 
                   VerticalAlignment="Center"/>
    </Grid>
</UserControl>
```

### 插件 UI 视觉规范

官方插件应合并 `[Plugins/Shared/PluginUiResources.xaml](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/Plugins/Shared/PluginUiResources.xaml)`（由 `LenovoLegionToolkit.Plugins.Shared` 提供），以保持与 Universal Device Toolkit 主程序一致的卡片布局、间距与 WPF-UI 按钮外观：

```xml
<UserControl.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="pack://application:,,,/LenovoLegionToolkit.Plugins.Shared;component/PluginUiResources.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</UserControl.Resources>
```

**布局与样式约定**


| 资源键                                                                          | 用途                |
| ---------------------------------------------------------------------------- | ----------------- |
| `PluginPageRootMargin`                                                       | 页根边距（`0,0,16,12`） |
| `PluginHeroSurfaceStyle`                                                     | Hero / 概览条        |
| `PluginCardContentStyle`                                                     | 标准内容卡片            |
| `PluginMetricCardStyle`                                                      | 紧凑指标卡             |
| `PluginPrimaryButtonStyle` / `PluginSecondaryButtonStyle`                    | WPF-UI 主/次按钮      |
| `PluginSectionTitleStyle` / `PluginBodyTextStyle` / `PluginCaptionTextStyle` | 标题与正文层级           |
| `PluginIconBadgeStyle` + `PluginIconOnAccentStyle`                           | Accent 图标徽章       |


**编写注意**

- 卡片间距 8px、圆角 8px，与主程序 `DesignTokens` 对齐。
- 宿主已通过 `IPluginPage.PageTitle` 展示标题时，页面 Hero 不要再重复同文案。
- `DataGrid` 等密集控件外包 `PluginCardContentStyle` 边框。
- 视觉打磨时保留全部 `AutomationId`（Workbench / 主程序 UI 烟测依赖）。
- 不要在插件 `.csproj` 中重复 `<Page Include>` 引用 `PluginUiResources.xaml`（Shared 项目已编译，重复会导致 NETSDK1022）。

**本地视觉回归**（插件仓库）：

```powershell
make.bat workbench-smoke --plugin-id custom-mouse --theme Dark
```

截图输出：`artifacts/workbench-visual/<plugin-id>-<theme>/`（`preview` / `settings` / `real-runtime`）。详见 [UniversalDeviceToolkit-Plugins — BUILD_SMOKE.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/Docs/BUILD_SMOKE.md#visual-smoke-pluginworkbench)。

---

## 配置存储

### 外部插件（贡献者路径）

官方与第三方插件通常使用 **插件本地 JSON 持久化**，而非宿主 `ApplicationSettings`：

- `**PluginBase.Configuration`**（`IPluginConfiguration`）：SDK 提供的按插件作用域键值存储，默认落在宿主 app-data 下的插件配置目录；适合简单设置（参见 `network-acceleration` 的 `Configuration.SetValue` / `SaveAsync` 模式）。
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
├── Resource.zh-hans.resx   # 简体中文
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
<!-- Resource.zh-hans.resx -->
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

### 被 MainAppPluginUi.Smoke 覆盖时的入口约束

如果插件希望被 `Tools/MainAppPluginUi.Smoke` 稳定覆盖，至少应满足以下之一：

- **Feature page 路径**：`GetFeatureExtension()` 返回可被宿主加载的页面对象。
- **Settings page 路径**：`GetSettingsPage()` 返回可被 `PluginSettingsWindow` 打开的页面对象。
- **Windows Optimization 路径**：`GetOptimizationCategory()` 返回能在主程序中稳定出现的分类与动作。

当前 smoke 主要验证的是“宿主是否能真正打开入口”，不是只看插件声明了能力。因此：

1. optimization-route 插件除了返回 `WindowsOptimizationCategoryDefinition`，还应确保分类在主程序实际页面中能被定位；若分类未出现，smoke 会按失败处理。
2. settings page 与 feature page 应返回宿主可直接创建的 UI 对象，避免仅在插件内部声明但无法被 `PluginPageWrapper` 或 `PluginSettingsWindow` 实际承载。
3. AutomationId、标题或分类结构若频繁漂移，会直接增加 UI smoke 失败率；新增或调整宿主入口时，最好同步验证现有 smoke 路径是否仍可定位。

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

完整实现见 [UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins) 的 `Plugins/` 目录：


| 插件 ID                    | 页面模型             | 说明                                                 |
| ------------------------ | ---------------- | -------------------------------------------------- |
| **custom-mouse**         | 设置页 + Windows 优化 | 无功能页（`GetFeatureExtension()` 为 `null`）；提供鼠标光标与优化动作 |
| **network-acceleration** | 功能页 + 设置页        | 网络加速功能与独立设置界面                                      |
| **shell-integration**    | 仅设置页（系统插件）       | `isSystemPlugin: true`，不可卸载；无功能页                   |
| **vive-tool**            | 功能页 + 设置页        | ViVeTool 功能管理与设置                                   |


---

## 调试插件

### 推荐：插件仓库工具链

日常开发与预览应在插件仓库使用 `llt-plugin.cmd` 与 **PluginWorkbench**（支持 System/Light/Dark、Feature/Settings/Optimization 视图、Preview / Real Runtime）：

```powershell
.\llt-plugin.cmd build --plugin my-plugin
.\llt-plugin.cmd preview --plugin my-plugin --theme system --view feature
.\llt-plugin.cmd dev --plugin my-plugin --theme system --view settings
```

视觉回归与 Workbench 烟测（插件仓库）：

```powershell
make.bat workbench-smoke --plugin-id custom-mouse --theme Dark
```

### 备选：复制到宿主 plugins 目录

调试宿主加载行为时，可将打包输出手动复制到主程序 `plugins` 目录：

- 开发环境：`Build/plugins/`
- 安装环境：`%LOCALAPPDATA%/UniversalDeviceToolkit/plugins/`

优先使用 `.\llt-plugin.cmd package --plugin my-plugin --build-first` 生成 ZIP，再解压或复制到上述目录。

---

## 发布插件

### 打包

在插件仓库：

```powershell
.\llt-plugin.cmd package --plugin my-plugin --build-first
.\llt-plugin.cmd validate --plugin my-plugin --profile contributor
```

输出 ZIP 包含插件程序集、`LenovoLegionToolkit.Plugins.SDK.dll`、生成的 `plugin.json`、`plugin.manifest.json` 与资源文件。

### 提交到插件仓库

1. Fork [UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins)
2. 使用 `llt-plugin.cmd init` 在 `Plugins/` 下创建插件，或扩展现有插件
3. 通过 `validate` 与 `workbench-smoke` 后提交 Pull Request

---

## 相关文档

- [UniversalDeviceToolkit-Plugins — PLUGIN_QUICKSTART.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/Docs/PLUGIN_QUICKSTART.md) - 插件作者快速开始
- [UniversalDeviceToolkit-Plugins — PLUGIN_DEVELOPMENT.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/Docs/PLUGIN_DEVELOPMENT.md) - 插件仓库完整开发指南
- [UniversalDeviceToolkit-Plugins — BUILD_SMOKE.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/Docs/BUILD_SMOKE.md) - 构建与视觉冒烟
- [ARCHITECTURE.md](ARCHITECTURE.md) - 宿主系统架构
- [CONTRIBUTING.md](../CONTRIBUTING.md) - 主程序贡献指南
- [AGENTS.md](../AGENTS.md) - 开发者指南

