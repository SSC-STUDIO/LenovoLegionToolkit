# Lenovo Legion Toolkit Plugin Development Guide / 插件开发指南

This document describes the current plugin development and release model used by the `LenovoLegionToolkit-Plugins` repository.
本文档说明 `LenovoLegionToolkit-Plugins` 仓库当前真实使用的插件开发与发布模型。

## Build Model / 构建模型

- Plugins compile against vendored host references in `Dependencies/Host`.
- Do not add source `ProjectReference` links back to the sibling `LenovoLegionToolkit` repository.
- Official plugin outputs are expected in `Build/plugins/LenovoLegionToolkit.Plugins.<FolderName>/`.

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\refresh-host-references.ps1 -UseSiblingRepoBuild
```

## Creating A Plugin / 创建插件

Preferred path:
推荐路径：

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\new-plugin.ps1 `
  -FolderName MyPlugin `
  -PluginId my-plugin `
  -DisplayName "My Plugin" `
  -Author "Your Name"
```

Generated structure:
生成后的结构：

```text
Plugins/
├── MyPlugin/
│   ├── LenovoLegionToolkit.Plugins.MyPlugin.csproj
│   ├── plugin.json
│   ├── MyPlugin.cs
│   └── ...
└── MyPlugin.Tests/
    ├── MyPlugin.Tests.csproj
    └── MyPluginPluginTests.cs
```

## Required Conventions / 必须遵守的约定

- Folder name: `Plugins/<FolderName>/`
- Project file: `LenovoLegionToolkit.Plugins.<FolderName>.csproj`
- Manifest file name: `plugin.json`
- Release ZIP: `<plugin-id>-v<version>.zip`
- Release tag: `<plugin-id>-v<version>`
- Output directory: `Build/plugins/LenovoLegionToolkit.Plugins.<FolderName>/`

## Manifest Contract / 清单约定

Minimum required fields in `plugin.json`:
`plugin.json` 最小必需字段：

```json
{
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "minLLTVersion": "3.6.1",
  "author": "Your Name",
  "isSystemPlugin": false,
  "repository": "https://github.com/yourname/your-plugin",
  "issues": "https://github.com/yourname/your-plugin/issues"
}
```

Use `minLLTVersion`, not `minimumHostVersion`.
请使用 `minLLTVersion`，不要再使用旧字段 `minimumHostVersion`。

## Validation / 校验

Run the completion checker before opening a PR or preparing a release:
在提交 PR 或准备发布前，先跑完成度检查：

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\plugin-completion-check.ps1
```

Specific plugins:
只校验指定插件：

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\plugin-completion-check.ps1 -PluginIds my-plugin
```

What it checks:
它会检查：

- `store.json` and `plugin.json` metadata alignment
- project naming and version alignment
- build output presence
- plugin changelog presence
- sibling test project presence

## Release Workflow / 发布流程

Current official release flow is GitHub Actions `workflow_dispatch`.
当前正式发布流程是 GitHub Actions 的 `workflow_dispatch`，不是 tag 自动触发。

Workflow file:

```text
.github/workflows/build.yml
```

Release steps:
发布步骤：

1. Update `plugin.json`
2. Update plugin `.csproj` version metadata
3. Update plugin `CHANGELOG.md`
4. Update `store.json` source entry if needed
5. Update the repository root `CHANGELOG.md`
6. Run `plugin-completion-check.ps1`
7. For the current official working set, validate with `powershell -ExecutionPolicy Bypass -File .\Scripts\plugin-completion-check.ps1 -PluginIds custom-mouse shell-integration vive-tool -OutputJson artifacts\plugin-completion-check-latest.json`
8. Trigger `build.yml` manually with:

- `plugin`: optional folder name(s)
- `version`: required for release publishing and must match `plugin.json`

The workflow will:
该工作流会：

1. validate completion
2. build selected plugins
3. create ZIP assets named from `plugin.json`
4. publish per-plugin GitHub releases
5. update `store.json`

## Notes For Third-Party Authors / 对第三方作者的说明

- This repository now includes tooling that can scaffold and validate non-official plugins.
- If your plugin is not meant to ship from this official repository, you can still reuse the scaffold and validation workflow locally or in your own fork.
- If you want your plugin added to the official store, prepare a PR with:

  - plugin source
  - `plugin.json`
  - plugin `CHANGELOG.md`
  - test project
  - correct `store.json` entry
  - evidence that `plugin-completion-check.ps1` passes
