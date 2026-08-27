# Universal Device Toolkit 插件

<p align="center">
  <b><a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit">Universal Device Toolkit</a> 官方插件生态</b><br/>
  官方还没写进宿主的那块硬件 - 不是 Windows 工具箱。<br/>
  需要宿主 <b>v6.0.0+</b> 才能安装 2.x 包 · v5.0.2 仍加载 1.x · .NET 10 · Electron web UI
</p>

[English README](README.md)

---

## 定位

插件**只**在硬件、厂商协议或驻留集成需要时扩展 UDT。目录刻意保持精简；准入门槛见 [Docs/Plugins/STRATEGY.md](../Docs/Plugins/STRATEGY.md)（设备能力三轨 + 三问筛子）。通用系统工具不收——那些面由宿主的 `udt` CLI 与自动化引擎承担。

---

## 快速安装

1. 打开 **Universal Device Toolkit**（2.x 包需要 v6.0.0 或更高；v5.0.2 仍从 `plugin-catalog` 安装 1.x）
2. 进入 **插件 → 浏览商店**
3. 点击 **安装**
4. 按提示重启应用

商店插件无需手动下载 ZIP。

---

## 插件目录

| 状态 | 插件 | 版本 | 说明 | 安装 ID |
|------|------|------|------|---------|
| 上架 | **光标与指针** | v2.0.0 | 主题感知光标、指针速度、主按键交换、安全备份/还原 | `custom-mouse` |
| 上架 | **ViVeTool** | v2.0.0 | 可视化浏览/开关 Windows 隐藏功能标志 | `vive-tool` |
| 下架 | **Nilesoft Shell 管理器** | v2.0.0 | 商店已下架；已安装用户可继续使用，并非宿主内置替代 | `shell-integration` |

> 版本以各插件 `plugin.manifest.json` 为准。已发布的 **v5.0.2** 继续读 `plugin-catalog`（1.x）；稳定 **v6.0.0** 从同一目录读取 **2.0.0**；预览宿主（`v6.0.0-preview.N`）仍读 `plugin-catalog-preview`。不要把预览插件 ZIP 上传到 `plugin-catalog`。

---

## 为什么选择这些插件？

每一项都是**设备能力**（或通过严选的驻留集成），按[策略筛子](../Docs/Plugins/STRATEGY.md)准入：

### 光标与指针（`custom-mouse`）——硬件主轨
鼠标/指针控制需要按设备写代码：随系统浅色/深色切换的光标主题、指针速度、主按键交换、安全备份与还原。后续计划：为 `udt` CLI 暴露窄接口，让 Agent 调同一能力。

### ViVeTool（`vive-tool`）——遗留，不是样板
功能标志开关是脚本形态；它先于当前准入门槛存在，保留给已安装用户。不再作为商店门面宣传；未来目录整理可标 `Removed`。

### Nilesoft Shell 管理器（`shell-integration`）——已下架
驻留轨道需要每天使用的真实证据。商店已下架；源码保留给已安装用户和本地导入。不是宿主内置的 Nilesoft 管理器。

### 100% 免费开源
MIT 许可，无广告、无付费墙、无遥测。

### 测试
Shared + 官方插件有完整单元测试，GitHub Actions 覆盖构建/校验/发布。

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
| `promote` / `generate-store` | 官方商店元数据 / 生成 `Plugins/.build/catalog/store.json` |

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
+- Plugins/HostBaseline/       # 宿主版本清单；二进制下载到被忽略的 .host/
+- Plugins/Templates/          # Authoring archetypes
+- Plugins/.build/             # Ignored build, package, and catalog output
+- Docs/Plugins/                # Plugin documentation
`- .github/workflows/plugins-* # Monorepo plugin CI and release workflows
```

---

## 版本约定

| 层级 | 真相源 | 当前基线 |
|------|--------|----------|
| 宿主（编译用 vendored 基线） | `host-release.json` / 主仓 | **5.0.2**（打出首个 `v6.0.0` ZIP 后再刷新） |
| 插件 SemVer | `plugin.manifest.json` → `version` | **2.0.0** |
| 最低宿主 | `minHostVersion`；运行时 `plugin.json` 的 `MinLltVersion` 为宿主 ABI 字段名 | **6.0.0** |
| 商店目录 | 生成的 `Plugins/.build/catalog/store.json` | 稳定 `plugin-catalog`（1.x + 2.0.0）或预览 `plugin-catalog-preview` |

---

## 贡献

1. 先过[策略筛子](../Docs/Plugins/STRATEGY.md)——只收设备能力或经严选的驻留集成
2. 从 `master` 开分支  
3. `.\udt-plugin.cmd doctor`  
4. `.\udt-plugin.cmd dev` 开发  
5. `validate` + 测试通过  
6. 提 PR  

详见 [CONTRIBUTING.md](../CONTRIBUTING.md) 与 [Docs/PLUGIN_DEVELOPMENT.md](../Docs/Plugins/PLUGIN_DEVELOPMENT.md)。

---

## 文档

- [文档索引](../Docs/Plugins/README.md)
- [插件策略](../Docs/Plugins/STRATEGY.md)（什么进目录、准入筛子）
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
