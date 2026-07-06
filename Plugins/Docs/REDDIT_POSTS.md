# Reddit Promotion Posts

## r/LenovoLegion — Main Post

**Title**: I built 4 open-source plugins to supercharge Lenovo Legion laptops (C# / .NET 10 / WPF)

**Body**:

Hey Legion community! 👋

I'm the developer behind **Universal Device Toolkit Plugins** — a collection of open-source plugins for Lenovo Legion Toolkit (LLT). 

After months of development, I'm excited to share what we've built:

## 🔌 4 Plugins Available Now

1. **🖱️ Custom Mouse** — Advanced mouse sensitivity & acceleration control. Create per-game DPI profiles, customize cursor styles, and seamlessly adapt to Light/Dark mode.

2. **⚡ Vive Tool** — Feature flag management for Windows. Unlock hidden Windows features safely from a searchable GUI. No command-line needed!

3. **📋 Shell Integration** — Right-click context menu enhancements. Instant access to power features from File Explorer.

4. **🚀 Network Acceleration** — Real-time network telemetry & optimization. Track speeds, monitor peak traffic, and apply gaming presets with one click. (Just redesigned in v1.2.0!)

## 🎯 Why This Matters

- **100% Free & Open Source** (MIT license)
- **No ads, no telemetry, no paywalls**
- **Native Windows 11 look & feel** (WPF + DynamicResource theming)
- **Full CI validation** — All 4 plugins pass automated testing
- **186 unit tests** for the ViveTool plugin alone

## 📊 Quality First

We just achieved a major milestone: **zero warnings across all 6 projects**. Every build is clean. Every public API has XML documentation. 

## 🔗 Links

- **GitHub Repo**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins
- **Download LLT**: [Lenovo Legion Toolkit](https://github.com/BartoszCichecki/LenovoLegionToolkit)
- **Install Plugins**: Use LLT's built-in plugin store

## 🌟 Get Involved

- **Try it**: Install LLT and browse the plugin store
- **Develop**: We have a full SDK + CLI tooling for creating plugins
- **Star us**: If you find this useful, please ⭐ the repo — help us reach 100+ stars!

**What plugin would you like to see next?** I'm thinking about a battery optimization plugin or a fan control UI. Let me know in the comments! 👇

---

*P.S. If you're a C# developer, check out our codebase. We just enforced `TreatWarningsAsErrors=true` globally and fixed all CA1062/CA2024 warnings. Good example of modern .NET 10 code!*

---

## r/csharp — Alternative Post

**Title**: Achieved zero warnings across 6 C# projects — here's our journey (.NET 10, WPF, Win11)

**Body**:

Hey C# community! 👋

I want to share our experience achieving **zero warnings across 6 C# projects** in our open-source plugin ecosystem for Lenovo laptops.

## 🎯 The Challenge

We have:
- 4 WPF plugin projects (CustomMouse, ViveTool, ShellIntegration, NetworkAcceleration)
- 1 SDK project
- 1 Shared library
- 1 Core tooling library

All targeting .NET 10, using WPF, with Windows 11 as target.

## ✅ What We Fixed

1. **CA1062 (Null validation)**: Added `ArgumentNullException.ThrowIfNull` to all public methods
2. **CA2024 (Async streaming)**: Fixed `ProcessRunner.PumpAsync` to avoid `EndOfStream` in async context
3. **CS1591 (Missing XML comments)**: Added XML documentation to all public APIs
4. **Version mismatches**: Synced versions across `plugin.json`, `plugin.manifest.json`, and `.csproj`

## 🛠️ How We Enforced It

Added to `Directory.Build.props`:
```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<WarningsAsErrors>CS1591;CA1062;CA2024</WarningsAsErrors>
```

Now **every** warning is a build error. No more ignoring warnings!

## 📊 Results

- **Build**: 0 warnings, 0 errors ✅
- **CI**: All 4 plugins pass validation ✅
- **Tests**: 186 unit tests passing (ViveTool)
- **Code quality**: Significantly improved

## 🔗 Check Out the Codebase

If you're interested in modern C# / WPF / .NET 10 code, take a look:

📦 https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins

We'd love feedback on our code quality and architecture! 

**Question for the community**: What other Roslyn analyzers do you recommend enabling for WPF projects?

---

## r/dotnet — Alternative Post

**Title**: Building a plugin ecosystem on .NET 10 — lessons learned

**Body**:

Hey .NET community! 👋

I've been building a **plugin ecosystem for Lenovo Legion Toolkit** on .NET 10, and I want to share some lessons learned.

## 🏗️ Architecture

- **Plugin SDK**: Interface-based plugin system with `IPlugin`, `IFeaturePage`, `ISettingsPage`
- **Dynamic Loading**: Plugins are ZIP files with `plugin.json` manifest
- **WPF UI**: Each plugin provides XAML-based settings and feature pages
- **Localization**: `x:Static` based resource system with 34 languages

## 🔧 Technical Highlights

1. **Zero-warnings build**: Enforced `TreatWarningsAsErrors=true` globally
2. **CI validation**: Multi-plugin validation with contributor profile
3. **CLI tooling**: `PluginTooling.Cli` for scaffolding, packaging, validation
4. **Unit testing**: 186 tests for ViveTool plugin

## 📦 Plugin Catalog

1. **Custom Mouse** — Mouse sensitivity & DPI profiles
2. **Vive Tool** — Windows feature flag management
3. **Shell Integration** — Context menu enhancements
4. **Network Acceleration** — Network telemetry & optimization

## 🔗 GitHub

📦 https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins

We're targeting **100+ GitHub stars** to grow the community. If you find this interesting, please ⭐!

**Question**: How do you handle plugin versioning and backward compatibility in your .NET projects?

---

## Dev.to Article Outline

**Title**: Building a WPF Plugin System with .NET 10: A Practical Guide

**Sections**:
1. Introduction — Why plugins?
2. Architecture — Plugin SDK design
3. Dynamic loading — Assembly loading & interface discovery
4. WPF integration — XAML-based UI injection
5. CI/CD — Validating plugins in GitHub Actions
6. Lessons learned — What we'd do differently
7. Conclusion — Try it yourself!

**Call to Action**: ⭐ GitHub repo, join Discussions, contribute!

---

## V2EX Post (Chinese)

**Title**: 我给联想笔记本写了 4 个开源插件（C# /.NET 10/WPF）

**Body**:

大家好！👋

我最近给联想笔记本开发了一个开源插件生态 —— **Universal Device Toolkit Plugins**。

## 🔌 4 个插件

1. **🖱️ Custom Mouse** — 鼠标灵敏度 & 加速度控制，支持按游戏配置 DPI 配置文件
2. **⚡ Vive Tool** — Windows 功能标志管理，图形化界面解锁隐藏功能
3. **📋 Shell Integration** — 右键菜单增强，快速访问电源功能
4. **🚀 Network Acceleration** — 实时网络遥测与优化（v1.2.0 刚重设计！）

## 🎯 特点

- **100% 开源**（MIT 许可证）
- **无广告、无遥测、无付费墙**
- **原生 Windows 11 外观**（WPF + 动态资源主题）
- **完整 CI 验证** — 所有插件通过自动化测试
- **186 个单元测试**

## 📊 代码质量

我们刚刚达成一个重要里程碑：**所有 6 个项目零警告构建**。每个构建都是干净的，每个公共 API 都有 XML 文档。

## 🔗 链接

- **GitHub 仓库**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins
- **下载 LLT**: [Lenovo Legion Toolkit](https://github.com/BartoszCichecki/LenovoLegionToolkit)
- **安装插件**: 使用 LLT 内置的插件商店

## 🌟 求 Star！

我们的目标是 **100+ GitHub stars**，如果你觉得有用，请给我们一个 ⭐！

**你们希望看到什么插件？** 我在考虑电池优化插件或风扇控制 UI。欢迎在评论区留言！👇

---

## 知乎文章大纲

**标题**: 如何给联想笔记本做硬件优化？我写了 4 个开源插件（C#/.NET 10）

**大纲**:
1. 引言 —— 为什么需要插件？
2. 插件生态架构 —— SDK 设计思路
3. 4 个插件详解 —— 功能与实现
4. 代码质量之旅 —— 从有警告到零警告
5. CI/CD 实践 —— GitHub Actions 自动化验证
6. 开源社区建设 —— 如何吸引贡献者
7. 结语 —— 试试看吧！

**行动号召**: ⭐ GitHub 仓库，加入 Discussions，贡献代码！

---

**These posts are ready to publish! Pick one platform and start shipping! 🚀**
