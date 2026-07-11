# Universal Device Toolkit 插件 — 让 Windows 更好用

Free. Open-source. No ads. No telemetry. Just better Windows.

[English README](README.md)

---

## 🚀 快速安装

1. 打开 **Universal Device Toolkit**
2. 进入 **插件** → **浏览商店**
3. 点击任意插件的 **安装** 按钮
4. 重启应用

就这样 — 无需手动下载，无需复杂设置。

---

## 📦 插件目录

| # | 插件 | 版本 | 说明 | 安装 |
|---|--------|---------|-------------|---------|
| 🔋 | **电池健康（已迁移）** | v1.0.0 | 已弃用：电池健康监控和提醒现已内置于 Universal Device Toolkit。仅保留用于旧版用户的设置迁移 | `battery-health` |
| 🔥 | **网络加速（已迁移）** | v1.2.0 | 已弃用：网络诊断和选择性代理加速现已内置于 Universal Device Toolkit。仅保留用于旧版用户的设置迁移 | `network-acceleration` |
| 🖱️ | **光标与指针** | v1.0.16 | 主题感知光标样式、Windows 指针速度、按键交换以及安全的光标备份与还原。自动适配浅色/深色模式 | `custom-mouse` |
| 🔧 | **ViVeTool** | v1.2.2 | 从 GUI 解锁 Windows 隐藏功能标志。无需命令行 — 浏览、搜索、启用、禁用功能 | `vive-tool` |
| 🐚 | **Nilesoft Shell 管理器** | v1.0.12 | 在 Universal Device Toolkit 中管理 Nilesoft Shell 的注册及 UDT 管理的配置。需要单独安装 Nilesoft Shell | `shell-integration` |

> **想要更多插件？** 查看 [插件商店](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/store.json) 或 [自己构建](#作者工作流)！

---

## ✨ 为什么选择这些插件？

### 🔐 100% 免费开源
无付费墙、无高级版、无广告。所有代码都在 GitHub 上，MIT 许可证。审核它、fork 它、贡献回去。

### 🎨 原生 Windows 11 外观
使用 .NET 10 和 WPF-UI 4.3.0 构建，这些插件使用真正的 Fluent Design 标记。它们自动适配你的 Windows 主题 — 没有"仅浅色模式"或"深色模式损坏"的 bug。

### 🔧 可扩展设计
插件 SDK 干净且文档完善。想要一个做 X 的插件？Fork 仓库，运行 `init`，2 分钟内开始构建。包含的 PluginWorkbench 让你可以预览插件而无需启动完整宿主应用。

### 🌍 本地化
所有插件开箱即支持英文和中文。添加新语言只需添加 `.resx` 文件。

### 🧪 实战测试
每个官方插件都附带单元测试、视觉冒烟测试（浅色 + 深色主题）和通过 GitHub Actions 的自动化 CI/CD。

---

## 🎯 功能亮点

### 电池健康（已迁移）
**已弃用** — 电池健康监控和提醒现已内置于 Universal Device Toolkit。此插件仅为从旧版本升级的用户保留设置迁移，新用户应使用内置的电池健康功能。

### 网络加速（已迁移）
**已弃用** — 网络诊断和选择性代理加速现已内置于 Universal Device Toolkit。此插件仅为从旧版本升级的用户保留设置迁移，新用户应使用内置的网络诊断功能。

### 光标与指针
- 主题感知光标样式（随 Windows 深色/浅色模式自动切换）
- Windows 指针速度与按键交换控制
- 安全的光标备份与还原
- 通过优化面板无缝集成 Windows

### ViVeTool
- 浏览和切换 Windows 隐藏功能标志
- 无需加入 Insider 即可进行 Insider-build 风格的调整
- 带搜索和过滤的干净表格 UI
- 安全默认 — 切换不会破坏任何东西

### Nilesoft Shell 管理器
- 在 Universal Device Toolkit 中直接管理 Nilesoft Shell 注册
- 应用或回滚由 UDT 管理的配置项
- 需要单独安装 Nilesoft Shell

---

## 👨‍💻 作者工作流

这个仓库包含完整的插件创作工具链：

```powershell
# 检查你的环境
.\llt-plugin.cmd doctor

# 创建新插件
.\llt-plugin.cmd init --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"

# 使用实时预览开发
.\llt-plugin.cmd dev --plugin my-plugin --theme system --view feature

# 测试、验证、打包
.\llt-plugin.cmd test --plugin my-plugin
.\llt-plugin.cmd validate --plugin my-plugin --profile contributor
.\llt-plugin.cmd package --plugin my-plugin --build-first
```

### 插件工具命令

| 命令 | 说明 |
|---------|-------------|
| `doctor` | 诊断环境和依赖 |
| `init` | 从模板搭建新插件 |
| `dev` | 构建 + 实时预览循环 |
| `test` | 运行单元测试 |
| `validate` | 检查创作和商店元数据 |
| `package` | 生成可安装的 ZIP |
| `promote` | 准备官方商店条目 |

> 工具链镜像 **VS Code 扩展开发模型**: `plugin.manifest.json` 是你的 `package.json`，`dev` 是你的 `npm run dev`，`package` 是你的 `vsce package`。

---

## 🏗️ 仓库结构

```
UniversalDeviceToolkit-Plugins/
├── Plugins/              # 官方插件项目
│   ├── CustomMouse/
│   ├── NetworkAcceleration/
│   ├── ShellIntegration/
│   └── ViveTool/
├── SDK/                  # 插件 SDK（接口和帮助类）
├── Dependencies/         # 共享依赖
├── Tools/                # PluginWorkbench + PluginTooling.CLI
├── Scripts/              # 自动化脚本
├── Docs/                 # 架构和创作指南
├── store.json            # 插件商店目录（发布输出）
└── Make.bat             # 常见任务的便捷包装器
```

---

## 🤝 贡献

我们欢迎贡献！无论你是修复 bug、添加功能还是创建全新的插件：

1. Fork 仓库
2. 创建功能分支 (`git checkout -b feature/my-plugin`)
3. 运行 `.\llt-plugin.cmd doctor` 验证你的环境
4. 使用 `.\llt-plugin.cmd dev` 开发你的插件
5. 提交 Pull Request

开始前请阅读 [CONTRIBUTING.md](./CONTRIBUTING.md) 和 [插件开发指南](./Docs/PLUGIN_DEVELOPMENT.md)。

---

## 📚 文档

- [快速开始](./Docs/PLUGIN_QUICKSTART.md) — 5 分钟内运行你的第一个插件
- [开发指南](./Docs/PLUGIN_DEVELOPMENT.md) — 深入插件 API
- [架构](./Docs/ARCHITECTURE.md) — 系统设计和依赖关系图
- [AI 代理工作流](./Docs/AI_AGENT_WORKFLOW.md) — 自动化友好的工作流文档
- [编码标准](./Docs/CODING_STANDARDS.md) — 命名、模式和禁止的反模式
- [更新日志](./CHANGELOG.md) — 发布历史

---

## 🌟 社区与支持

- **Issues**: [GitHub Issues](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/issues)
- **讨论**: [GitHub Discussions](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/discussions)
- **主应用**: [Universal Device Toolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit)

---

## 📄 许可证

这个项目是开源的。详见各插件的许可证。

---

<p align="center">
  由 SSC-STUDIO 团队和 Universal Device Toolkit 社区用 ❤️ 构建。
</p>
