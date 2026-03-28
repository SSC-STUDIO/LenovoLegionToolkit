# Lenovo Legion Toolkit 插件快速开始

> 面向“第一次在本仓库里创建新插件”的最短可执行流程。

## 1. 环境准备

- 安装 .NET 10 SDK
- 使用 Windows 10/11 x64
- 确保仓库根目录存在 `Dependencies/Host`

如果主仓库刚更新过，请先刷新宿主引用：

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\refresh-host-references.ps1 -UseSiblingRepoBuild
```

## 2. 生成插件骨架

优先使用仓库自带脚手架，不要手工从旧文档复制 `PackageReference` 或旧目录结构。

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\new-plugin.ps1 `
  -FolderName MyPlugin `
  -PluginId my-plugin `
  -DisplayName "My Plugin" `
  -Author "Your Name"
```

脚手架会生成：

- `Plugins/MyPlugin/`
- `Plugins/MyPlugin/plugin.json`
- `Plugins/MyPlugin/LenovoLegionToolkit.Plugins.MyPlugin.csproj`
- `Plugins/MyPlugin.Tests/`

## 3. 必须遵守的约定

- 插件目录：`Plugins/<FolderName>/`
- 项目文件：`LenovoLegionToolkit.Plugins.<FolderName>.csproj`
- 输出目录：`Build/plugins/LenovoLegionToolkit.Plugins.<FolderName>/`
- 清单文件名固定为：`plugin.json`
- ZIP 命名固定为：`<plugin-id>-v<version>.zip`

`plugin.json` 最小示例：

```json
{
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "minLLTVersion": "3.6.1",
  "author": "Your Name",
  "isSystemPlugin": false,
  "repository": "",
  "issues": ""
}
```

## 4. 构建与自检

构建单个插件：

```powershell
dotnet build .\Plugins\MyPlugin\LenovoLegionToolkit.Plugins.MyPlugin.csproj -c Release
```

运行完成度检查：

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\plugin-completion-check.ps1 -PluginIds my-plugin
```

只做元数据检查时可跳过 build/tests：

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\plugin-completion-check.ps1 `
  -PluginIds my-plugin `
  -SkipBuild `
  -SkipTests
```

## 5. 发布到本仓库的正确方式

本仓库当前使用 GitHub Actions `workflow_dispatch` 发版，不是“打 tag 自动发版”。

标准流程：

1. 更新插件目录下的 `plugin.json`
2. 更新对应 `.csproj` 版本
3. 更新插件自己的 `CHANGELOG.md`
4. 确保 `store.json` 条目字段正确
5. 运行 `plugin-completion-check.ps1`
6. 手动触发 `.github/workflows/build.yml`

触发参数：

- `plugin`: 可选，插件文件夹名，多个用逗号分隔
- `version`: 必填于正式发布场景，且必须与 `plugin.json` 的 `version` 一致

发布后生成的正式链接格式：

```text
https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases/download/<plugin-id>-v<version>/<plugin-id>-v<version>.zip
```

## 6. 常见错误

- 不要使用旧的 `Crs10259` 仓库地址
- 不要继续使用旧字段 `minimumHostVersion`
- 不要在插件项目中引用 sibling `LenovoLegionToolkit` 源码工程
- 不要使用旧的 `make.bat zip`
- 不要把输出目录改成 `bin/Release/...` 再假设 workflow 会自动找到

## 7. 下一步

- 需要更完整的接口/发布说明：看 [PLUGIN_DEVELOPMENT.md](./PLUGIN_DEVELOPMENT.md)
- 需要官方插件维护流程：看仓库根目录 [README.md](../README.md)
