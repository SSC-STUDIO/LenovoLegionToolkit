# Lenovo Legion Toolkit 插件开发指南

> 为 Lenovo Legion Toolkit 开发单个插件的完整流程

---

## 📋 目录

1. [环境准备](#1-环境准备)
2. [创建插件项目](#2-创建插件项目)
3. [实现插件功能](#3-实现插件功能)
4. [添加插件元数据](#4-添加插件元数据)
5. [构建和测试](#5-构建和测试)
6. [发布插件](#6-发布插件)

---

## 1. 环境准备

### 必需软件

- **.NET 10.0 SDK** 或更高版本
  ```bash
  dotnet --version  # 验证安装
  ```

- **Visual Studio 2022** 或 **VS Code**

- **Windows 10/11** 操作系统

### 下载 Lenovo Legion Toolkit

确保已安装最新版 Lenovo Legion Toolkit (v2.14.0+) 用于测试

---

## 2. 创建插件项目

### 步骤 1: 创建项目文件夹

```bash
# 创建项目目录
mkdir MyFirstPlugin
cd MyFirstPlugin
```

### 步骤 2: 初始化项目

```bash
# 创建类库项目
dotnet new classlib -n LenovoLegionToolkit.Plugins.MyFirstPlugin

# 进入项目目录
cd LenovoLegionToolkit.Plugins.MyFirstPlugin
```

### 步骤 3: 修改项目文件

编辑 `.csproj` 文件：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <!-- 引用 Lenovo Legion Toolkit SDK -->
  <ItemGroup>
    <PackageReference Include="LenovoLegionToolkit.Plugins.SDK" Version="1.0.0" />
  </ItemGroup>

</Project>
```

> **注意**: SDK 包需要从 LLT 官方仓库下载或引用本地 DLL

---

## 3. 实现插件功能

### 步骤 1: 创建主插件类

创建 `MyFirstPlugin.cs`：

```csharp
using System;
using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Plugins.MyFirstPlugin
{
    /// <summary>
    /// 我的第一个插件
    /// </summary>
    public class MyFirstPlugin : IPlugin
    {
        // 必需: 插件唯一标识符（只能包含小写字母、数字和连字符）
        public string Id => "my-first-plugin";
        
        // 必需: 插件显示名称
        public string Name => "我的第一个插件";
        
        // 必需: 插件描述
        public string Description => "这是一个示例插件";
        
        // 必需: 图标名称（Fluent UI System Icons）
        public string Icon => "Apps24";
        
        // 必需: 是否为系统级插件
        public bool IsSystemPlugin => false;
        
        // 可选: 依赖的其他插件 ID
        public string[]? Dependencies => null;

        /// <summary>
        /// 插件安装后调用
        /// </summary>
        public void OnInstalled()
        {
            // 初始化代码
            Console.WriteLine($"[{Name}] 插件已安装");
        }

        /// <summary>
        /// 插件卸载前调用
        /// </summary>
        public void OnUninstalled()
        {
            // 清理代码
            Console.WriteLine($"[{Name}] 插件已卸载");
        }

        /// <summary>
        /// 应用程序关闭时调用
        /// </summary>
        public void OnShutdown()
        {
            // 释放资源
            Console.WriteLine($"[{Name}] 应用程序关闭");
        }

        /// <summary>
        /// 插件停止时调用（更新或卸载前）
        /// </summary>
        public void Stop()
        {
            // 停止服务
            Console.WriteLine($"[{Name}] 插件停止");
        }
    }
}
```

### 步骤 2: 创建插件页面（可选）

如果需要 UI，创建 `MyFirstPluginPage.xaml`：

```xml
<UserControl x:Class="LenovoLegionToolkit.Plugins.MyFirstPlugin.MyFirstPluginPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             mc:Ignorable="d" 
             d:DesignHeight="450" d:DesignWidth="800">
    <Grid>
        <TextBlock Text="欢迎使用我的插件！" 
                   FontSize="24" 
                   HorizontalAlignment="Center" 
                   VerticalAlignment="Center"/>
    </Grid>
</UserControl>
```

创建 `MyFirstPluginPage.xaml.cs`：

```csharp
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Plugins.MyFirstPlugin
{
    /// <summary>
    /// 插件页面
    /// </summary>
    public partial class MyFirstPluginPage : UserControl, IPluginPage
    {
        public string PageTitle => "我的插件";
        public string? PageIcon => "Apps24";

        public MyFirstPluginPage()
        {
            InitializeComponent();
        }

        public object CreatePage()
        {
            return this;
        }
    }
}
```

### 步骤 3: 实现功能扩展点（可选）

创建 `MyFirstPluginExtension.cs`：

```csharp
using System;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.MyFirstPlugin
{
    /// <summary>
    /// 插件功能扩展
    /// </summary>
    public class MyFirstPluginExtension
    {
        private readonly MyFirstPlugin _plugin;

        public MyFirstPluginExtension(MyFirstPlugin plugin)
        {
            _plugin = plugin;
        }

        /// <summary>
        /// 获取功能页面（在 LLT 主界面显示）
        /// </summary>
        public object? GetFeatureExtension()
        {
            // 返回插件页面实例
            return new MyFirstPluginPage();
        }

        /// <summary>
        /// 获取设置页面
        /// </summary>
        public object? GetSettingsExtension()
        {
            // 返回设置页面
            return null; // 如果没有设置页面，返回 null
        }

        /// <summary>
        /// 获取托盘菜单项
        /// </summary>
        public MenuItem[]? GetTrayMenuExtensions()
        {
            // 返回托盘菜单项
            return null;
        }
    }
}
```

---

## 4. 添加插件元数据

### 步骤 1: 创建 plugin.json

在项目根目录创建 `plugin.json`：

```json
{
  "id": "my-first-plugin",
  "name": "我的第一个插件",
  "version": "1.0.0",
  "minLLTVersion": "2.14.0",
  "author": "您的名字",
  "repository": "https://github.com/yourusername/my-first-plugin",
  "issues": "https://github.com/yourusername/my-first-plugin/issues"
}
```

**字段说明**:

| 字段 | 必需 | 说明 |
|------|------|------|
| `id` | ✅ | 插件唯一标识符，只能包含小写字母、数字和连字符 |
| `name` | ✅ | 插件显示名称 |
| `version` | ✅ | 版本号，格式为 `主版本.次版本.修订版本` |
| `minLLTVersion` | ✅ | 最低支持的 LLT 版本 |
| `author` | ✅ | 作者名称 |
| `repository` | ❌ | 源码仓库地址 |
| `issues` | ❌ | 问题反馈地址 |

### 步骤 2: 设置文件属性

在 `.csproj` 中添加：

```xml
<ItemGroup>
  <None Update="plugin.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## 5. 构建和测试

### 步骤 1: 构建插件

```bash
# 还原依赖
dotnet restore

# 构建项目
dotnet build --configuration Release

# 输出目录
# bin/Release/net10.0-windows/
```

### 步骤 2: 本地安装测试

1. **找到 LLT 插件目录**:
   ```
   %APPDATA%\LenovoLegionToolkit\plugins\
   ```

2. **创建插件目录**:
   ```
   %APPDATA%\LenovoLegionToolkit\plugins\my-first-plugin\
   ```

3. **复制文件**:
   - `LenovoLegionToolkit.Plugins.MyFirstPlugin.dll`
   - `plugin.json`
   - 其他依赖 DLL

4. **重启 Lenovo Legion Toolkit**

5. **检查插件是否加载**:
   - 打开 LLT
   - 导航到"插件和扩展"
   - 查看"我的第一个插件"是否显示

### 步骤 3: 调试

如果遇到问题，检查：

- ✅ 插件 ID 是否唯一且不包含大写字母
- ✅ `plugin.json` 格式是否正确
- ✅ DLL 文件是否已复制到正确位置
- ✅ 最低 LLT 版本是否匹配

---

## 6. 发布插件

### 步骤 1: 准备发布包

创建发布目录结构：

```
my-first-plugin-v1.0.0/
├── LenovoLegionToolkit.Plugins.MyFirstPlugin.dll
├── plugin.json
└── [其他依赖 DLL]
```

打包为 ZIP：

```bash
# Windows PowerShell
Compress-Archive -Path "my-first-plugin-v1.0.0\*" -DestinationPath "my-first-plugin-v1.0.0.zip"
```

### 步骤 2: 发布到 GitHub

1. **创建 GitHub 仓库**

2. **推送代码**:
   ```bash
   git init
   git add .
   git commit -m "Initial commit"
   git remote add origin https://github.com/yourusername/my-first-plugin.git
   git push -u origin main
   ```

3. **创建 Release**:
   - 在 GitHub 仓库页面点击 "Releases"
   - 点击 "Create a new release"
   - 版本标签: `v1.0.0`
   - 上传 `my-first-plugin-v1.0.0.zip`
   - 填写更新日志
   - 发布

### 步骤 3: 提交到 LLT 插件商店（可选）

要让你的插件显示在 LLT 内置插件商店中：

1. **Fork** [LenovoLegionToolkit-Plugins](https://github.com/Crs10259/LenovoLegionToolkit-Plugins) 仓库

2. **编辑 `store.json`**:
   ```json
   {
     "plugins": [
       {
         "id": "my-first-plugin",
         "name": "我的第一个插件",
         "version": "1.0.0",
         "description": "这是一个示例插件",
         "author": "您的名字",
         "downloadUrl": "https://github.com/yourusername/my-first-plugin/releases/download/v1.0.0/my-first-plugin-v1.0.0.zip",
         "minimumHostVersion": "2.14.0",
         "icon": "Apps24",
         "iconBackground": "#0078D4",
         "isSystemPlugin": false,
         "fileSize": 0,
         "changelog": "版本 1.0.0\n- 初始发布",
         "releaseDate": "2026-02-06T00:00:00Z",
         "tags": ["utility"]
       }
     ]
   }
   ```

3. **创建 Pull Request**

---

## 📚 示例项目

### 最小可运行示例

**文件结构**:
```
MyFirstPlugin/
├── LenovoLegionToolkit.Plugins.MyFirstPlugin.csproj
├── plugin.json
└── MyFirstPlugin.cs
```

**完整代码**:

`LenovoLegionToolkit.Plugins.MyFirstPlugin.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="LenovoLegionToolkit.Plugins.SDK" Version="1.0.0" />
  </ItemGroup>
  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

`plugin.json`:
```json
{
  "id": "my-first-plugin",
  "name": "我的第一个插件",
  "version": "1.0.0",
  "minLLTVersion": "2.14.0",
  "author": "您的名字"
}
```

`MyFirstPlugin.cs`:
```csharp
using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Plugins.MyFirstPlugin
{
    public class MyFirstPlugin : IPlugin
    {
        public string Id => "my-first-plugin";
        public string Name => "我的第一个插件";
        public string Description => "Hello World 插件";
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

## ❓ 常见问题

### Q: 插件加载失败怎么办？

**检查清单**:
1. 确认 `plugin.json` 存在且格式正确
2. 确认插件 ID 只包含小写字母、数字和连字符
3. 确认 DLL 和 `plugin.json` 在同一目录
4. 查看 LLT 日志: `%APPDATA%\LenovoLegionToolkit\log\`

### Q: 如何调试插件？

**方法 1**: 使用 Visual Studio 附加到 LLT 进程
**方法 2**: 添加日志输出
```csharp
using LenovoLegionToolkit.Lib.Utils;

public void OnInstalled()
{
    Log.Instance.Trace($"[{Id}] 插件已安装");
}
```

### Q: 插件可以访问 LLT 的哪些功能？

查看 SDK 提供的接口：
- `IPluginManager` - 插件管理
- `ISettingsService` - 设置服务
- `ILogger<T>` - 日志服务
- 其他 LLT 内部服务

### Q: 如何更新插件？

1. 增加 `plugin.json` 中的版本号
2. 重新构建并打包
3. 用户通过 LLT 插件商店更新，或手动替换文件

---

## 📖 参考资源

- [完整插件开发指南](./PLUGIN_DEVELOPMENT.md)
- [Lenovo Legion Toolkit 主项目](https://github.com/Crs10259/LenovoLegionToolkit)
- [Lenovo Legion Toolkit 插件仓库](https://github.com/Crs10259/LenovoLegionToolkit-Plugins)
- [Fluent UI 图标列表](https://react.fluentui.dev/?path=/docs/icons-catalog--docs)
- [.NET 10 文档](https://docs.microsoft.com/dotnet/)

---

**最后更新**: 2026-02-09

如有问题，请在 LLT GitHub 仓库提交 Issue。
