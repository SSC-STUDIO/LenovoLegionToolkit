# 插件开发指南

本文档详细介绍如何为 Lenovo Legion Toolkit 开发插件。

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

Lenovo Legion Toolkit 支持通过插件系统扩展功能。插件可以：

- 添加新的功能页面
- 集成到 Windows 优化功能
- 提供自定义设置界面
- 访问主程序的服务

### 插件类型

| 类型 | 说明 | 可卸载 |
|------|------|--------|
| **功能插件** | 提供独立功能模块 | ✅ |
| **系统插件** | 核心功能扩展，随主程序启动 | ❌ |

---

## 快速开始

### 1. 创建项目

创建一个新的 WPF 类库项目：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <RootNamespace>MyPlugin</RootNamespace>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="path\to\LenovoLegionToolkit.Lib\LenovoLegionToolkit.Lib.csproj" />
  </ItemGroup>
</Project>
```

### 2. 创建插件类

```csharp
using LenovoLegionToolkit.Lib.Plugins;

namespace MyPlugin;

[Plugin(
    id: "my-plugin",
    name: "My Plugin",
    version: "1.0.0",
    description: "A sample plugin",
    author: "Your Name",
    MinimumHostVersion = "2.14.0",
    Icon = "Apps24"
)]
public class MyPlugin : PluginBase
{
    public override string Id => "my-plugin";
    public override string Name => "My Plugin";
    public override string Description => "A sample plugin";
    public override string Icon => "Apps24";
    public override bool IsSystemPlugin => false;

    public override object? GetFeatureExtension()
    {
        return new MyPluginFeaturePage();
    }

    public override object? GetSettingsPage()
    {
        return new MyPluginSettingsPage();
    }

    public override void OnInstalled()
    {
        // 插件安装后的初始化
    }

    public override void OnUninstalled()
    {
        // 插件卸载前的清理
    }

    public override void OnShutdown()
    {
        // 应用关闭时的清理
    }

    public override void Stop()
    {
        // 停止后台服务
    }
}
```

### 3. 创建插件元数据

创建 `Plugin.json` 文件：

```json
{
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "minLLTVersion": "2.14.0",
  "author": "Your Name",
  "repository": "https://github.com/yourname/my-plugin",
  "issues": "https://github.com/yourname/my-plugin/issues"
}
```

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

建议继承 `PluginBase` 基类：

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

---

## 配置存储

### 使用 ApplicationSettings

```csharp
public class MyPlugin : PluginBase
{
    private const string SettingsKey = "my-plugin-settings";
    
    public MyPluginSettings GetSettings()
    {
        var appSettings = IoCContainer.Resolve<ApplicationSettings>();
        var json = appSettings.Store.GetCustomValue(SettingsKey);
        return JsonSerializer.Deserialize<MyPluginSettings>(json) 
            ?? new MyPluginSettings();
    }
    
    public void SaveSettings(MyPluginSettings settings)
    {
        var appSettings = IoCContainer.Resolve<ApplicationSettings>();
        var json = JsonSerializer.Serialize(settings);
        appSettings.Store.SetCustomValue(SettingsKey, json);
        appSettings.SynchronizeStore();
    }
}

public class MyPluginSettings
{
    public bool EnableFeature { get; set; } = true;
    public string SelectedOption { get; set; } = "default";
}
```

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
  <value>A sample plugin for Lenovo Legion Toolkit</value>
</data>
```

```xml
<!-- Resource.zh-hans.resx -->
<data name="PluginName" xml:space="preserve">
  <value>我的插件</value>
</data>
<data name="PluginDescription" xml:space="preserve">
  <value>Lenovo Legion Toolkit 示例插件</value>
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

---

## 最佳实践

### 1. 命名规范

| 项目 | 规范 | 示例 |
|------|------|------|
| 插件 ID | 小写字母、数字、连字符 | `my-plugin` |
| 插件类 | `{Name}Plugin` | `MyPlugin` |
| 页面类 | `{Name}Page` | `MyFeaturePage` |
| 设置类 | `{Name}Settings` | `MyPluginSettings` |

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

```csharp
[Plugin(
    id: "my-plugin",
    MinimumHostVersion = "2.14.0"  // 最低兼容版本
)]
public class MyPlugin : PluginBase
{
    // 插件实现
}
```

---

## 示例插件

完整的示例插件请参考插件仓库：

- **CustomMouse**: 自定义鼠标指针插件
- **NetworkAcceleration**: 网络加速插件
- **ShellIntegration**: Shell 集成插件（系统插件）

插件仓库地址：[LenovoLegionToolkit-Plugins](https://github.com/Crs10259/LenovoLegionToolkit-Plugins)

---

## 调试插件

### 本地调试

1. 构建插件项目
2. 将输出文件复制到主程序的 `plugins` 目录：
   - 开发环境：`Build/plugins/`
   - 安装环境：`%APPDATA%/LenovoLegionToolkit/plugins/`
3. 启动主程序进行调试

### 调试配置

在 Visual Studio 中配置调试：

```xml
<!-- 在插件项目文件中添加 -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <StartAction>Program</StartAction>
  <StartProgram>path\to\LenovoLegionToolkit.WPF.exe</StartProgram>
</PropertyGroup>
```

---

## 发布插件

### 打包

1. 构建发布版本：`dotnet build -c Release`
2. 创建 ZIP 压缩包，包含：
   - 插件程序集 (.dll)
   - 依赖程序集
   - 资源文件
   - Plugin.json

### 提交到插件仓库

1. Fork 插件仓库
2. 在 `Plugins/` 目录下创建插件文件夹
3. 提交 Pull Request

---

## 相关文档

- [ARCHITECTURE.md](ARCHITECTURE.md) - 系统架构
- [CONTRIBUTING.md](../CONTRIBUTING.md) - 贡献指南
- [AGENTS.md](../AGENTS.md) - 开发者指南
