# Universal Device Toolkit 插件

<p align="center">
  <b><a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit">Universal Device Toolkit</a> 官方插件生态</b><br/>
  Free. Open-source. No ads. No telemetry. Just better Windows.<br/>
  需要宿主 <b>v5.0.0+</b> · .NET 10 · WPF-UI 4.3.0
</p>

[English README](README.md)

---

## 快速安装

1. 打开 **Universal Device Toolkit**（v5.0.0 或更高）
2. 进入 **插件 → 浏览商店**
3. 点击 **安装**
4. 按提示重启应用

商店插件无需手动下载 ZIP。

---

## 插件目录

| 状态 | 插件 | 版本 | 说明 | 安装 ID |
|------|------|------|------|---------|
| 上架 | **光标与指针** | v1.0.18 | 主题感知光标、指针速度、主按键交换、安全备份/还原 | `custom-mouse` |
| 上架 | **ViVeTool** | v1.2.4 | 可视化浏览/开关 Windows 隐藏功能标志 | `vive-tool` |
| 上架 | **Nilesoft Shell 管理器** | v1.0.14 | 管理 Nilesoft Shell 注册与 UDT 配置（需单独安装 Shell） | `shell-integration` |

> 版本以各插件 `plugin.manifest.json` 为准；上架清单见根目录 [`Plugins/.build/catalog/plugin-catalog.json`](./.build/catalog/plugin-catalog.json)（发布生成物）。

---

## 为什么选择这些插件？

### 100% 免费开源
MIT 许可，无广告、无付费墙、无遥测。

### 原生 Windows 11 体验
**.NET 10** + **WPF-UI 4.3.0** Fluent 设计标记，浅色/深色主题一等公民。

### 可扩展
SDK、脚手架与 **PluginWorkbench** 支持不启动完整宿主即可预览。

### 本地化
官方插件提供约 **32** 种资源区域（含 `en` / `zh-Hans` / `zh-Hant`）。

### 测试
Shared + 官方插件有完整单元测试，GitHub Actions 覆盖构建/校验/发布。

---

## 功能亮点

### 光标与指针（`custom-mouse`）
- 随系统浅色/深色切换的光标主题
- 指针速度与主按键交换
- 安全备份与还原

### ViVeTool（`vive-tool`）
- 可搜索的功能标志列表
- 无需手写 CLI 即可启用/禁用
- 功能页 + 设置页

### Nilesoft Shell 管理器（`shell-integration`）
- 在 UDT 中注册/注销 Nilesoft Shell
- 应用或回滚 UDT 管理的配置项
- 需要本机已安装 Nilesoft Shell

---

## 作者工作流

标准入口：**`udt-plugin.cmd`**  
（`llt-plugin.cmd` 为兼容别名，行为相同。）

```powershell
.\udt-plugin.cmd doctor
.\udt-plugin.cmd init --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"
.\udt-plugin.cmd dev --plugin my-plugin --theme system --view feature
.\udt-plugin.cmd test --plugin my-plugin
.\udt-plugin.cmd validate --plugin my-plugin --profile contributor
.\udt-plugin.cmd package --plugin my-plugin --build-first
```

| 命令 | 说明 |
|------|------|
| `doctor` | 环境与宿主依赖诊断 |
| `init` | 从模板脚手架 |
| `dev` | 构建 + Workbench 预览 |
| `test` / `validate` / `package` | 测试、门禁、打 ZIP |
| `bump-version` / `sync-version` | 版本真相源同步 |
| `promote` / `generate-store` | 官方商店元数据 / 生成 `Plugins/.build/catalog/plugin-catalog.json` |

---

## 仓库结构

```
UniversalDeviceToolkit/
+- Plugins/Official/         # Official plugin projects and tests
|  +- CustomMouse/
|  +- ShellIntegration/
|  `- ViveTool/
+- Plugins/SDK/Runtime/       # Plugin SDK runtime surface
+- Plugins/Shared/            # Shared plugin helpers
+- Plugins/Shared.Tests/       # Shared helper tests
+- Plugins/Testing/            # Tooling and performance tests
+- Plugins/Tooling/            # CLI, PluginWorkbench, and smoke tools
+- Plugins/Dependencies/Host/  # Vendored host baseline; downloaded cache is .host/
+- Plugins/Templates/          # Authoring archetypes
+- Plugins/.build/             # Ignored build, package, and catalog output
+- Docs/Plugins/                # Plugin documentation
`- .github/workflows/plugins-* # Monorepo plugin CI and release workflows
```

---

## 版本约定

| 层级 | 真相源 | 当前基线 |
|------|--------|----------|
| 宿主 | `host-release.json` / 主仓 | **5.0.0** |
| 插件 SemVer | `plugin.manifest.json` → `version` | 见上表 |
| 最低宿主 | `minHostVersion`；运行时 `plugin.json` 的 `MinLltVersion` 为宿主 ABI 字段名 | **5.0.0** |
| 商店目录 | 生成的 `Plugins/.build/catalog/plugin-catalog.json` | 勿手改作为日常入口 |

---

## 贡献

1. 从 `master` 开分支  
2. `.\udt-plugin.cmd doctor`  
3. `.\udt-plugin.cmd dev` 开发  
4. `validate` + 测试通过  
5. 提 PR  

详见 [CONTRIBUTING.md](../CONTRIBUTING.md) 与 [Docs/PLUGIN_DEVELOPMENT.md](../Docs/Plugins/PLUGIN_DEVELOPMENT.md)。

---

## 文档

- [文档索引](../Docs/Plugins/README.md)
- [快速开始](../Docs/Plugins/PLUGIN_QUICKSTART.md)
- [开发指南](../Docs/Plugins/PLUGIN_DEVELOPMENT.md)
- [架构](../Docs/Plugins/ARCHITECTURE.md)
- [SDK 变更](../Docs/Plugins/SDK_CHANGELOG.md)
- [更新日志](./CHANGELOG.md)

---

## 社区

- Issues / Discussions：本仓库 GitHub 页面  
- 主应用：[Universal Device Toolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit)

## 许可证

MIT，见 [LICENSE](../LICENSE)。

---

<p align="center">
  由 SSC-STUDIO 与 Universal Device Toolkit 社区构建。
</p>
