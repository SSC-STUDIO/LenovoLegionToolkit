> **Historical document** — launch/sprint material. Version numbers and plugin counts may be outdated.
> Source of truth: root `README.md`, `Docs/PLUGIN_*.md`, and `Plugins/*/plugin.manifest.json`.
> See also [Docs/README.md](./README.md).

# 🌏 多平台推广文案汇总 (Ready-to-Post Copies)

> 使用前请替换 `[当前star数]` 为实际数字

---

## 1. V2EX 分享创造 — 中文

**标题:** [创造] 开源 Windows 设备管理插件生态系统 — 5个免费插件让 Windows 更好用

**正文:**

大家好！我想分享一个开源项目：**Universal Device Toolkit Plugins** — 一个为 Windows 设备管理工具提供插件扩展的生态系统。

### 🎯 项目亮点

- **100% 免费开源** (MIT License)
- **原生 Windows 11 外观** (.NET 10 + WPF-UI 4.3.0 Fluent Design)
- **5 个官方插件**，全部支持浅色/深色主题

### 🔥 插件介绍

1. **网络加速** (Network Acceleration)
   - 实时下载/上传流量监控
   - 游戏/流媒体/工作自适应加速预设
   - 一键网络优化
   - 峰值流量监控和活跃适配器检测

2. **ViVeTool GUI**
   - 从 GUI 解锁 Windows 隐藏功能标志
   - 无需命令行 — 带搜索和过滤的可浏览表格
   - 安全默认设置 — 切换不会破坏系统

3. **自定义鼠标** (Custom Mouse)
   - 主题感知光标样式（自动适配 Windows 深色/浅色模式）
   - 每个应用程序的 DPI 配置
   - 无缝 Windows 指针速度管理

4. **Shell 集成** (Shell Integration)
   - 右键菜单集成
   - 从资源管理器任何位置即时访问电源功能

5. **电池健康** (Battery Health)
   - 电池健康监控 + 循环计数
   - 容量衰减检测

### 📦 安装

需要有 Universal Device Toolkit（轻量无后台的 OEM 管理套件替代品）：
1. 打开 Universal Device Toolkit
2. 进入 **插件** → **浏览商店**
3. 点击 **安装**
4. 重启应用

### 🔗 链接

- **GitHub**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins
- **主应用**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
- **文档**: 仓库内 `Docs/` 目录

### 🙏 求 Star

目前项目还只有 `[当前star数]` 个 star，希望社区朋友们能帮忙 star 一下，让更多人看到这个开源项目！

如果有任何问题或建议，欢迎在 GitHub Discussions 讨论 😊

---

## 2. 知乎 — 技术文章/想法

**标题:** 如何用 C# .NET 10 构建 WPF 桌面应用的插件系统？

**正文:**

最近在做一个开源项目 **Universal Device Toolkit Plugins**，一个为 Windows 设备管理工具提供插件扩展的生态系统。在这里分享一下架构设计中的一些心得。

### 架构设计

1. **插件 SDK**: 基于接口的干净 SDK (`IPlugin`, `IPluginFeaturePage`, `IPluginSettingsPage`)
2. **插件清单**: JSON 作者清单 (`plugin.manifest.json`)，类似 VS Code 的 `package.json`
3. **Fallback UI 模式**: 每个插件都有 `BuildFallbackUi()` 方法，在 XAML 加载失败时程序化构建整个 UI
4. **主题无关**: 所有颜色使用 `DynamicResource` 绑定（零硬编码），所有文本使用 `x:Static` 本地化

### 技术栈

- C# .NET 10
- WPF + WPF-UI 4.3.0 (Fluent Design)
- GitHub Actions CI/CD

### 项目链接

GitHub: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins

欢迎有兴趣的朋友 star / fork / 提 PR！🙏

---

## 3. Bilibili — 视频描述文案

**标题:** 【开源】Windows 插件生态系统 — 网络加速 + 解锁隐藏功能 + 自定义鼠标

**简介:**

一个开源的 Windows 设备管理插件生态系统，包含 5 个免费插件：

✅ 网络加速 — 实时流量监控 + 游戏优化
✅ ViVeTool GUI — 解锁 Windows 隐藏功能
✅ 自定义鼠标 — DPI 配置 + 光标主题
✅ Shell 集成 — 右键菜单集成
✅ 电池健康 — 电池健康监控 + 循环计数

🔗 GitHub: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins
🔗 主应用: https://github.com/SSC-STUDIO/UniversalDeviceToolkit

100% 免费开源，MIT License，无广告无遥测！

---

## 4. Twitter/X — 英文短推

**Tweet 1:**
Just released v1.2.0 of Network Acceleration plugin for Windows 🚀

✅ Real-time telemetry dashboard
✅ Gaming presets with 1-click optimization
✅ Peak traffic monitoring
✅ Adaptive acceleration profiles

Part of Universal Device Toolkit Plugins — 100% FREE & open-source

GitHub: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins

#Windows #OpenSource #dotnet #WPF

---

**Tweet 2:**
Built a plugin ecosystem for Windows device management 🛠️

5 official plugins:
🔥 Network Acceleration
🖱️ Custom Mouse
🔧 ViVeTool GUI
🐚 Shell Integration
🔋 Battery Health

.NET 10 + WPF — Native Fluent Design
Zero hardcoded colors — full Dark/Light theme support

GitHub (⭐ appreciated!): https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins

#CSharp #Windows11 #OpenSource

---

## 5. Reddit — 通用短版（适用于多个 subreddit）

**标题:** Free open-source Windows plugin ecosystem — 5 plugins, native Fluent Design, zero telemetry

**正文:**

Hey everyone! 👋

I've been working on **Universal Device Toolkit Plugins** — a free, open-source plugin ecosystem for Windows device management.

**5 official plugins:**
- 🔥 **Network Acceleration** — Real-time telemetry, gaming presets, one-click optimization
- 🖱️ **Custom Mouse** — DPI profiles, theme-aware cursors, pointer speed management
- 🔧 **ViVeTool GUI** — Unlock hidden Windows features from a searchable table
- 🐚 **Shell Integration** — Right-click context menu access
- 🔋 **Battery Health** — Battery health monitoring, cycle count, capacity degradation

**Tech stack:**
- .NET 10 + WPF
- WPF-UI 4.3.0 (Fluent Design)
- Zero hardcoded colors — full Light/Dark theme support
- MIT License — 100% free, no ads, no telemetry

**GitHub:** https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins

Would love feedback from the community! What plugins would you want to see next? ⭐ appreciated!

---

## 📝 使用说明

1. 选择你要发布的平台
2. 复制对应文案
3. 替换 `[当前star数]` 为实际数字
4. 发布！
5. 在 `Docs/PROMOTION_CHECKLIST.md` 中记录发布信息

---

## 🔗 快速链接

- **GitHub 仓库**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins
- **主应用仓库**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
- **中文 README**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/blob/master/README.zh-CN.md
- **Discussions**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/discussions
- **详细推广计划**: `Docs/PROMOTION.md`
- **发布清单**: `Docs/PROMOTION_CHECKLIST.md`

