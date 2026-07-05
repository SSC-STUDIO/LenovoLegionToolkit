# Changelog / 更新日志

All notable changes to this project will be documented in this file.
此项目的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

## [Unreleased]

### Added / 添加
- **Social Preview Banner / 社交预览横幅**: Added `Assets/social-preview.svg` — a 1280x640 promotional banner featuring the plugin catalog, tech stack badges (.NET 10, C#, WPF, Windows 10/11), and key value propositions (100% Free, Open Source, No Ads/Telemetry/Paywalls) for use as GitHub social preview (og:image) and README header / 新增 `Assets/social-preview.svg` — 1280x640 推广横幅，展示插件目录、技术栈标签（.NET 10、C#、WPF、Windows 10/11）及核心价值主张（100% 免费、开源、无广告/遥测/付费墙），用于 GitHub 社交预览 (og:image) 和 README 页首

### Changed / 变更
- **README Enhancement / README 增强**: Reorganized badges with better visual hierarchy (Stars, License, CI, Discussions), added social preview banner as README header, simplified tagline / 重新组织徽章以提升视觉层次感（Stars、License、CI、Discussions），将社交预览横幅作为 README 页首，简化标语
- **UI Theming Fix / 界面主题修复**: Replaced `Brushes.Transparent` with `null` (WPF default, inherits parent background) in `NetworkAccelerationControl.xaml.cs` (3 locations) and `ViveToolPage.xaml.cs` (1 location), and updated `WpfFallbackHelper.cs` to use theme-aware brush resolution (`ResolveFallbackBrush`) instead of hardcoded `Brushes.White`/`Brushes.Black`/`Brushes.Gray` — ensures fallback UI adapts to Light/Dark theme / 将 `NetworkAccelerationControl.xaml.cs`（3 处）和 `ViveToolPage.xaml.cs`（1 处）中的 `Brushes.Transparent` 替换为 `null`（WPF 默认值，继承父背景），更新 `WpfFallbackHelper.cs` 使用主题感知的画笔解析（`ResolveFallbackBrush`）替代硬编码的 `Brushes.White`/`Brushes.Black`/`Brushes.Gray` — 确保 fallback UI 能适配浅色/深色主题

### Toolchain
- **Plugin Toolchain Stabilization**: Replaced direct `dotnet run` plugin-tooling entry points with a cached `Build/tooling` CLI shim, made the legacy build workflow package-only, and hardened selected-plugin `store.json` generation so release jobs merge existing entries and fail when expected ZIP assets are missing.
- **Workbench Visual Smoke Evidence**: Extended `PluginWorkbench.Smoke` with screenshot capture and luminance checks so Light/Dark plugin reviews produce PNG and JSON evidence instead of UI Automation checks only.
- **All-Plugin Release Batch**: Prepared `network-acceleration 1.1.9`, `shell-integration 1.0.12`, and `vive-tool 1.2.2` after full unit, official-candidate, and visual smoke validation.

### Changed / 变更
- **README Enhancement / README 增强**: Improved badge style (for-the-badge), added plugin catalog table with emoji icons and install IDs, added "Why These Plugins?" section (open-source, native Windows 11 look, extensible, localized, battle-tested). Enhanced visual appeal for star growth / 改进徽章样式、插件目录表增加 emoji 图标和安装 ID、新增"为什么选择这些插件？"章节（开源、原生 Windows 11 外观、可扩展、本地化、实战测试），提升视觉吸引力以促进 star 增长
- **Community Health / 社区健康**: Added `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1) and expanded `CONTRIBUTING.md` with development setup, plugin dev guide, UI guidelines, and coding standards. Created announcement issue #48 and enabled GitHub Discussions / 新增 `CODE_OF_CONDUCT.md`（Contributor Covenant v2.1），扩展 `CONTRIBUTING.md`（开发环境搭建、插件开发指南、UI 规范、编码标准），创建公告 issue #48 并启用 GitHub Discussions
- **Promotion Plan / 推广计划**: Created `Docs/PROMOTION.md` with ready-to-publish posts for Reddit (r/Windows11, r/Lenovo, r/opensource, r/csharp, r/pcmasterrace), V2EX, Zhihu, and Bilibili to drive from 2 to 100+ GitHub stars / 创建 `Docs/PROMOTION.md`，包含 Reddit、V2EX、知乎、Bilibili 等平台的推广文案，目标从 2 个 star 增长到 100+
- **Network Acceleration UI/UX Redesign / 网络加速界面重设计**: Completely redesigned `NetworkAccelerationControl.xaml` with a TabControl-based dual-tab layout (Dashboard + Optimization), hero status banner, telemetry metric cards with large 28px fonts, peak traffic and active adapter cards, quick actions panel, and a fully programmatic fallback UI that mirrors the XAML structure. All colors use DynamicResource (zero hardcoded), all text uses x:Static localization, and the fallback UI is entirely self-contained / 完全重设计 `NetworkAccelerationControl.xaml`，采用基于 TabControl 的双标签页布局（仪表盘 + 优化），新增顶部 Hero 状态横幅、28px 大字体的遥测指标卡片、峰值流量与活跃适配器卡片、快速操作面板，以及完全镜像 XAML 结构的编程式 fallback UI。所有颜色使用 DynamicResource（零硬编码），所有文本使用 x:Static 本地化，fallback UI 完全自包含
- **WPF-UI 4.3.0 Migration / WPF-UI 4.3.0 迁移**: Updated official plugins and the standalone PluginWorkbench to WPF-UI `4.3.0`, migrated WPF UI icon/theme API usages, hardened SDK host-window type probing for the removed `UiWindow` type, and ensured the new `Wpf.Ui.Abstractions.dll` sidecar is copied into plugin build outputs / 将官方插件与独立 PluginWorkbench 更新到 WPF-UI `4.3.0`，迁移 WPF UI 图标与主题 API 用法，增强 SDK 对已移除 `UiWindow` 类型的宿主窗口探测兼容性，并确保新的 `Wpf.Ui.Abstractions.dll` sidecar 进入插件构建输出
- **Plugin Author Toolchain Rewrite / 插件作者工具链重构**: Added `plugin.manifest.json` as the unified authoring manifest, introduced VS Code-style `init`, `dev`, `test`, `package`, and `migrate` commands, synchronized legacy `plugin.json`/`store-entry.json` compatibility outputs, and strengthened validation/package checks for contributions and required ZIP contents / 新增 `plugin.manifest.json` 作为统一作者清单，引入接近 VS Code 插件开发流程的 `init`、`dev`、`test`、`package` 与 `migrate` 命令，同步旧版 `plugin.json`/`store-entry.json` 兼容输出，并加强贡献点与 ZIP 必需内容的校验和打包检查

## [1.0.37] - 2026-04-29

### Added / 添加
- **AI Agent Workflow / AI 代理工作流**: Added a thin `AGENTS.md` entry point and `Docs/AI_AGENT_WORKFLOW.md` with standard report paths, store generation checks, Workbench smoke commands, and dirty-worktree safety rules for automation agents / 新增轻量 `AGENTS.md` 入口与 `Docs/AI_AGENT_WORKFLOW.md`，记录标准报告路径、商店生成校验、Workbench 冒烟命令以及自动化代理的脏工作区安全规则
- **Batch Plugin Release Dispatch / 批量插件发布触发**: Expanded `build.yml` so `workflow_dispatch` now accepts preferred comma-separated `plugin_ids` for one batch publish while preserving the legacy single-plugin `plugin` + `version` path / 扩展 `build.yml` 的 `workflow_dispatch`，新增首选的逗号分隔 `plugin_ids` 批量发布输入，同时保留旧的单插件 `plugin` + `version` 兼容路径
- **Standalone Plugin Workbench / 独立插件工作台**: Added `Tools/PluginWorkbench` as a dedicated standalone host that can load plugin build outputs or local ZIPs, preview feature/settings/optimization UI without launching the main app repo, and switch explicitly into `Real Runtime` when live actions are needed / 新增 `Tools/PluginWorkbench` 独立插件工作台，可直接加载插件构建输出或本地 ZIP，在无需启动主程序源码仓库的情况下预览 Feature/Settings/Optimization 界面，并在需要真实动作时显式切换到 `Real Runtime`
- **PluginWorkbench Smoke / 插件工作台冒烟**: Added `Tools/PluginWorkbench.Smoke` to exercise the standalone host end to end against built plugin outputs, including the `Preview` to `Real Runtime` transition for `custom-mouse` / 新增 `Tools/PluginWorkbench.Smoke`，用于基于已构建插件输出对独立宿主做端到端冒烟验证，当前覆盖 `custom-mouse` 的 `Preview` 到 `Real Runtime` 切换路径
- **Plugin Tooling CLI / 插件工具链 CLI**: Added `Tools/PluginTooling.Core` and `Tools/PluginTooling.Cli` as the standard author workflow entry point, covering `doctor`, `new`, `build`, `preview`, `validate`, `pack`, and `promote` / 新增 `Tools/PluginTooling.Core` 与 `Tools/PluginTooling.Cli` 作为标准作者工作流入口，覆盖 `doctor`、`new`、`build`、`preview`、`validate`、`pack` 与 `promote`
- **Official Store Entry Files / 官方商店条目文件**: Added plugin-local `store-entry.json` metadata for official plugins so root `store.json` can be treated as release output instead of the first edit point for new contributors / 为官方插件新增插件目录内的 `store-entry.json` 元数据，使根 `store.json` 可以作为发布输出而不是新贡献者的第一编辑入口

### Fixed / 修复
- **CustomMouse UI Model Alignment / CustomMouse 界面模型对齐**: Restored `custom-mouse` to the settings-plus-Windows Optimization model by removing its standalone feature page, so the main app no longer treats it as a sidebar plugin page and `PluginWorkbench` now falls back to the first available tab when a plugin has no feature preview / 将 `custom-mouse` 恢复为“设置页 + 系统优化扩展”模型，移除误暴露的独立功能页，使主程序不再把它当作侧栏插件页面，同时让 `PluginWorkbench` 在插件没有 feature 预览时自动回退到首个可用标签页
- **PluginWorkbench DLL Selection / 插件工作台 DLL 选择**: Fixed `PluginWorkbenchSession` so standalone preview loads the actual plugin assembly instead of accidentally selecting `LenovoLegionToolkit.Plugins.Shared.dll` or `LenovoLegionToolkit.Plugins.SDK.dll` from build outputs, restoring `shell-integration` preview loading / 修复 `PluginWorkbenchSession` 的插件 DLL 选择逻辑，使独立预览宿主不再误把 `LenovoLegionToolkit.Plugins.Shared.dll` 或 `LenovoLegionToolkit.Plugins.SDK.dll` 当成主插件，从而恢复 `shell-integration` 的独立预览加载
- **Plugin Store Release Drift / 插件商店发布漂移**: Backfilled the missing GitHub releases for `network-acceleration v1.1.6` and `vive-tool v1.1.9` so published `store.json` metadata no longer points to 404 downloads during real online installs / 补齐 `network-acceleration v1.1.6` 与 `vive-tool v1.1.9` 缺失的 GitHub Release 资产，避免已发布的 `store.json` 在真实在线安装时指向 404 下载地址
- **Plugin Runtime Sidecar Packaging / 插件运行时依赖打包**: Plugin build outputs now copy lockfile sidecar assemblies into release ZIPs, so feature-page plugins like `vive-tool` and `network-acceleration` carry required dependencies such as `Microsoft.Extensions.Logging.Abstractions` and no longer fail at runtime after installation / 插件构建输出现在会把 lockfile sidecar 依赖程序集一并带入发布 ZIP，使 `vive-tool`、`network-acceleration` 这类 feature-page 插件随包携带 `Microsoft.Extensions.Logging.Abstractions` 等必需依赖，避免安装后运行时装载失败
- **Plugin Tooling dotnet Host Resolution / 插件工具链 dotnet 宿主解析**: Fixed `PluginTooling.Core` so nested build/test steps invoke the real `dotnet` host instead of accidentally re-launching the plugin CLI itself, which restores release-workflow validation and packaging on GitHub Actions / 修复 `PluginTooling.Core` 的 `dotnet` 宿主解析逻辑，使内部 build/test 步骤改为调用真实 `dotnet` 而不是误重启插件 CLI 本身，从而恢复 GitHub Actions 上的发布校验与打包链路
- **Official Validation Test Restore / 官方校验测试还原**: Fixed official candidate validation so plugin test projects restore their NuGet assets on clean GitHub Actions runners instead of assuming pre-existing `project.assets.json`, which unblocks multi-plugin release workflows from failing at the test step / 修复官方候选校验流程，使插件测试项目在干净的 GitHub Actions runner 上会正常还原 NuGet 资产，而不是错误依赖预先存在的 `project.assets.json`，从而解除批量插件发布在测试步骤上的阻塞
- **ViveTool Version Detection / ViveTool 版本识别**: Tightened version parsing so `ViveTool` only accepts real version-line formats instead of accidentally treating unrelated command-processor banners like Windows `10.0` as the tool version / 收紧 `ViveTool` 的版本解析逻辑，只接受真正的版本行格式，避免把 Windows 命令处理器横幅里的 `10.0` 之类无关数字误识别为工具版本
- **Network Acceleration Sampling Resilience / 网络加速采样韧性**: Isolate per-adapter statistics failures so one bad network interface no longer blocks the entire sampling loop, which keeps history and live updates flowing when other adapters still report successfully / 将统计异常隔离到单个网卡级别，避免单个异常适配器拖垮整轮采样，从而在其他网卡仍可正常读取时继续产出历史与实时更新
- **PluginWorkbench Smoke Optional Optimization / 插件工作台冒烟可选优化页**: Updated `PluginWorkbench.Smoke` so feature-only plugins without an optimization category no longer fail while waiting for an optimization action button / 更新 `PluginWorkbench.Smoke`，使没有优化分类的功能型插件不再因等待优化操作按钮而失败
- **Release Selection Normalization / 发布选择归一化**: Normalize manual release selection from plugin IDs or legacy folder names before validation, packaging, release publishing, and `store.json` updates so the selected plugin set is carried consistently across jobs / 在校验、打包、发布 release 和更新 `store.json` 之前，将手动发布选择从插件 ID 或旧目录名统一归一化，确保选中的插件集合在各个作业间保持一致
- **Plugin Version Metadata Alignment / 插件版本元数据对齐**: Align `Version`, `FileVersion`, and `AssemblyVersion` for the current target releases `custom-mouse 1.0.15`, `shell-integration 1.0.11`, `network-acceleration 1.1.8`, and `vive-tool 1.2.1` / 对齐当前目标发布 `custom-mouse 1.0.15`、`shell-integration 1.0.11`、`network-acceleration 1.1.8`、`vive-tool 1.2.1` 的 `Version`、`FileVersion` 与 `AssemblyVersion`

### Improved / 改进
- **Agent-Friendly Plugin Tooling / 代理友好插件工具链**: Extended `plugin-tooling` with `inspect` reports, doctor JSON output, deterministic `generate-store --check`, and `--release-date` support so agents and CI can reproduce plugin metadata without rewriting files / 扩展 `plugin-tooling`，新增 `inspect` 报告、doctor JSON 输出、确定性的 `generate-store --check` 以及 `--release-date` 支持，使代理和 CI 能在不改写文件的情况下复现插件元数据
- **Workbench Smoke Coverage / 工作台冒烟覆盖**: Updated `make.bat workbench-smoke` to pass through `--plugin-id` and `--theme`, and made smoke failures expand the Workbench log for easier automation triage / 更新 `make.bat workbench-smoke` 以透传 `--plugin-id` 与 `--theme`，并让冒烟失败时展开 Workbench 日志，便于自动化排查
- **Plugin UI Automation and Theme Safety / 插件界面自动化与主题可靠性**: Added stable AutomationIds and host-resource status colors across official plugin pages, ViveTool tables, and the Shell style dialog so Light/Dark reviews and UI automation can target controls reliably / 在官方插件页面、ViveTool 表格和 Shell 样式对话框中补充稳定 AutomationId 与宿主资源状态颜色，使深浅色审查和 UI 自动化都能可靠定位控件
- **Plugin Page Layout Density / 插件页面布局密度**: Reworked the official plugin feature and settings pages toward status bars, direct forms, command rows, and data-table-first layouts so they fit the host's de-carded plugin manager style with less nested card chrome / 将官方插件功能页与设置页收敛为状态条、直接表单、命令区和数据表优先布局，减少嵌套卡片装饰以匹配主程序去卡片化后的插件管理风格
- **ViveTool Smoke UI Parity / ViveTool 冒烟 UI 对齐**: Added stable AutomationIds and fallback UI parity for ViveTool feature/settings pages so smoke automation can target both XAML and fallback paths consistently / 为 ViveTool 功能页与设置页补充稳定 AutomationIds 并对齐 fallback UI，使冒烟自动化能一致定位 XAML 与回退路径
- **Feature Plugin UI Polish / 功能插件界面打磨**: Refined the `network-acceleration` and `vive-tool` feature pages with clearer icon-led actions, stronger warning/status affordances, and safer button sizing for host and standalone preview flows / 打磨 `network-acceleration` 与 `vive-tool` 功能页，补强带图标的关键操作、警告与状态提示，并调整按钮尺寸以适配主程序宿主和独立预览流程
- **Settings First-Screen Density / 设置页首屏密度**: Tightened the `custom-mouse` and `shell-integration` settings layouts so overview metrics now sit in a compact single-row summary and more actionable controls remain visible in the first viewport inside the host settings shell for both Light and Dark themes / 收紧 `custom-mouse` 与 `shell-integration` 设置页布局，使概览指标收敛为首屏单行摘要，并让更多可操作控件在宿主设置壳的首屏内保持可见，同时兼顾浅色与深色主题
- **Settings UI Polish / 设置界面打磨**: Refined the `custom-mouse` and `shell-integration` settings pages plus the Shell style fallback dialog to better match the host shell’s spacing, card hierarchy, and Dark/Light theme presentation during standalone preview and host-driven settings flows / 打磨 `custom-mouse`、`shell-integration` 的设置页以及 Shell 样式 fallback 弹窗，使其在独立预览和宿主设置流程中更贴近主程序的间距、卡片层级和深浅色表现
- **Workbench Theme Validation / 工作台主题验证**: Extended `PluginWorkbench.Smoke` so theme selection is parameter-driven and `shell-integration` now verifies the style-settings dialog path instead of stopping at the settings shell / 扩展 `PluginWorkbench.Smoke`：主题切换改为参数驱动，并让 `shell-integration` 在设置页之外继续验证样式设置弹窗路径
- **Plugin Test Output Reliability / 插件测试输出可靠性**: Ensure plugin test projects pre-create culture-specific output directories before copying localized plugin assemblies, reducing false-negative Windows test failures in release validation / 在复制本地化插件程序集前为测试项目预先创建语言输出目录，降低 Windows 发布校验中因目录缺失导致的假失败
- **Network Acceleration Test Defaults / 网络加速测试默认值**: Reset `NetworkAcceleration.Tests` snapshot assertions back to plugin defaults before verifying copy semantics, so release validation no longer inherits stale preferred-mode values from persisted configuration / 在校验快照复制语义前将 `NetworkAcceleration.Tests` 显式重置回插件默认设置，避免发布校验继承持久化配置中的过期首选模式值
- **Store Metadata Release Validation / 商店元数据发布校验**: Validate official `store.json` download and changelog URLs against live GitHub release assets before the workflow proceeds, so broken store metadata fails CI instead of reaching users / 在工作流继续执行前，将官方 `store.json` 的下载与更新日志链接与真实 GitHub Release 资产进行比对校验，使损坏的商店元数据在 CI 阶段失败而不是流到用户侧
- **Release Documentation Alignment / 发布文档对齐**: Update repository release instructions to document the preferred `plugin_ids` batch publish path, the legacy single-plugin compatibility path, and the current target release wave `custom-mouse 1.0.15`, `shell-integration 1.0.11`, `network-acceleration 1.1.8`, `vive-tool 1.2.1` / 更新仓库发布说明，记录首选的 `plugin_ids` 批量发布路径、旧单插件兼容路径，以及当前目标发布批次 `custom-mouse 1.0.15`、`shell-integration 1.0.11`、`network-acceleration 1.1.8`、`vive-tool 1.2.1`
- **Standalone Host Bootstrap / 独立宿主引导**: Standardized `Dependencies/Host/host-release.json`, `ensure-host-dependencies.ps1`, and the SDK host-context bridge so plugin development now works against either a sibling LLT build or the published `v3.6.15` host release without reintroducing direct source-tree coupling / 统一 `Dependencies/Host/host-release.json`、`ensure-host-dependencies.ps1` 与 SDK 宿主上下文桥接逻辑，使插件开发现在既可基于 sibling LLT 构建结果，也可基于已发布的 `v3.6.15` 宿主 release 工作，而不会重新引入对主程序源码树的直接耦合
- **Workbench Host-Fidelity Preview / 工作台宿主级预览**: Upgraded `PluginWorkbench` from a plain loader into a host-style preview shell with persisted `System / Light / Dark` theme selection, metadata side panel, safer Preview messaging, and host-style settings/dialog containers / 将 `PluginWorkbench` 从普通加载器升级为宿主风格预览壳，新增持久化的 `System / Light / Dark` 主题选择、元数据侧栏、更清晰的 Preview 安全提示以及宿主风格的设置页/对话框容器

## [1.0.36] - 2026-04-05

### Added / 添加
- **Architecture Documentation / 架构文档**: Added comprehensive `Docs/ARCHITECTURE.md` documenting system design, dependency relationships, localization architecture, lifecycle flow, test coverage (523 tests), security practices, and performance optimization strategies / 添加完整的 `Docs/ARCHITECTURE.md` 记录系统设计、依赖关系、本地化架构、生命周期流程、测试覆盖（523 测试）、安全实践和性能优化策略
- **Coding Standards Guide / 编码规范指南**: Added detailed `Docs/CODING_STANDARDS.md` covering naming conventions, async/await patterns, exception handling, resource management, process execution security, WPF UI patterns, localization rules, forbidden anti-patterns, with concrete examples / 添加详细的 `Docs/CODING_STANDARDS.md` 涵盖命名约定、async/await 模式、异常处理、资源管理、进程执行安全、WPF UI 模式、本地化规则、禁止的反模式，附带具体示例
- **EditorConfig Standardization / EditorConfig 标准化**: Added `.editorconfig` for automatic code style enforcement across IDEs, including naming rules (interfaces `I*`, private fields `_camelCase`), code quality rules, pattern matching preferences, and file-scoped namespace requirements / 添加 `.editorconfig` 以在 IDE 间自动强制代码风格，包括命名规则（接口 `I*`、私有字段 `_camelCase`）、代码质量规则、模式匹配偏好和文件作用域命名空间要求

### Changed / 变更
- **Documentation Index / 文档索引**: Updated `README.md` to reference new architecture and coding standards documentation / 更新 `README.md` 以引用新的架构和编码规范文档
- **Test Quality Improvements / 测试质量改进**: Removed `ConfigureAwait(false)` from xUnit tests (xUnit1030), replaced `Assert.True().EndsWith()` with `Assert.EndsWith()` (xUnit2009), converted blocking `.Result` calls to async `await` (xUnit1031), fixed unused theory parameters (xUnit1026/xUnit1011) / 移除 xUnit 测试中的 `ConfigureAwait(false)`（xUnit1030），将 `Assert.True().EndsWith()` 替换为 `Assert.EndsWith()`（xUnit2009），将阻塞的 `.Result` 调用转换为异步 `await`（xUnit1031），修复未使用的理论参数（xUnit1026/xUnit1011）

### Fixed / 修复
- **SDK Dependency Architecture / SDK 依赖架构**: Fixed circular dependency between SDK and Shared projects by reversing the dependency flow; SDK now correctly references Shared instead of Shared referencing SDK, enabling proper HttpClient singleton usage across all plugins / 修复 SDK 与 Shared 项目间的循环依赖，反转依赖流向；SDK 现正确引用 Shared 而非 Shared 引用 SDK，使所有插件能正确使用 HttpClient singleton
- **HttpClient Consolidation / HttpClient 合并**: Removed duplicate HttpClient singleton from SDK PluginBase; GetSharedHttpClient() now delegates to HttpClientManager.GetSharedClient() for centralized socket exhaustion prevention / 移除 SDK PluginBase 中的重复 HttpClient singleton；GetSharedHttpClient() 现委托给 HttpClientManager.GetSharedClient() 以集中防止 socket 耗尽
- **Runtime Token API / 运行时令牌 API**: Made GetRuntimeCancellationToken() virtual in SDK PluginBase to allow derived classes with runtime implementations to properly override instead of misleading placeholder that always returns CancellationToken.None / 将 GetRuntimeCancellationToken() 在 SDK PluginBase 中改为 virtual，使有运行时实现的派生类能正确覆写，而非总是返回 CancellationToken.None 的误导性占位符
- **Parameter Validation / 参数验证**: Added ArgumentNullException.ThrowIfNull() to WpfFallbackHelper.TryInitializeComponent() for proper fallbackBuilder parameter validation (CA1062) / 为 WpfFallbackHelper.TryInitializeComponent() 添加 ArgumentNullException.ThrowIfNull() 以正确验证 fallbackBuilder 参数（CA1062）

## [1.0.35] - 2026-03-31

### Changed / 变更
- **Version / 版本**: Bump shared repository/store version metadata to `1.0.35` for working-set release consistency / 将共享仓库与商店版本元数据提升到 `1.0.35` 以保持工作集发布一致性

### Verified / 验证
- **Working Set Release Validation / 工作集发布验证**: Ran plugin completion check against `custom-mouse`, `shell-integration`, `vive-tool` / 对 `custom-mouse`、`shell-integration`、`vive-tool` 执行插件完成检查
  - ✅ CustomMouse: PASS (0 failures, 1 warning) / 通过（0 失败，1 警告）
  - ❌ ShellIntegration: FAIL (4 failures, 1 warning) — blocked by upstream `shell.exe` fetch failure, documented since 1.0.31 / 失败（4 失败，1 警告）— 因上游 `shell.exe` 拉取失败阻塞，自 1.0.31 起已记录
  - ✅ ViveTool: PASS (0 failures, 1 warning) / 通过（0 失败，1 警告）
- **Evidence / 证据**: `artifacts/plugin-completion-check-latest.json`, `working-set-release-smoke-test-latest.log` / 见仓库 artifacts 与附件日志

## [1.0.34] - 2026-03-31

### Fixed / 修复
- **CustomMouse Async Lifecycle / CustomMouse 异步生命周期**: Converted INF-based cursor theme application to truly async pattern with proper timeout handling and process cleanup; lifecycle persistence operations now use deterministic execution via `RunLifecycleTask` instead of fire-and-forget / 将基于 INF 的光标主题应用转换为真正的异步模式，添加超时处理和进程清理；生命周期持久化操作现在通过 `RunLifecycleTask` 确定性执行而非 fire-and-forget
- **ShellIntegration Build Verification / ShellIntegration 构建验证**: Added hard build-time check for Shell runtime binaries to prevent packaging broken releases when shell.exe fetch fails / 添加 Shell 运行时二进制文件的硬构建时检查，防止在 shell.exe 拉取失败时打包损坏的发布包

### Changed / 变更
- **Version / 版本**: Bump shared repository/store version metadata to `1.0.34` for plugin lifecycle and build verification improvements / 将共享仓库与商店版本元数据提升到 `1.0.34` 以包含插件生命周期和构建验证改进

## [1.0.33] - 2026-03-31

### Fixed / 修复
- **Version Alignment / 版本对齐**: Bump shared repository/store version metadata to `1.0.33` for working-set release consistency / 将共享仓库与商店版本元数据提升到 `1.0.33` 以保持工作集发布一致性

## [1.0.32] - 2026-03-31

### Fixed / 修复
- **Plugin Completion Check Parameter Normalization / 插件完成检查参数规范化**: Fixed `plugin-completion-check.ps1` to accept both space-separated PowerShell array syntax and comma-separated single string for `-PluginIds` parameter, ensuring consistent behavior across README examples and actual usage / 修复 `plugin-completion-check.ps1` 以同时接受空格分隔的 PowerShell 数组语法和逗号分隔的单一字符串作为 `-PluginIds` 参数，确保 README 示例与实际使用行为一致
- **Documentation Plugin ID Consistency / 文档插件 ID 一致性**: Corrected plugin ID references in README and documentation from `vivetool` to the actual store.json ID `vive-tool` / 修正 README 和文档中的插件 ID 引用，从 `vivetool` 改为实际的 store.json ID `vive-tool`

### Changed / 变更
- **Version / 版本**: Bump shared repository/store version metadata to `1.0.32` for parameter handling fix and documentation alignment / 为参数处理修复和文档对齐将共享仓库与商店版本元数据提升到 `1.0.32`

## [1.0.31] - 2026-03-31

### Fixed / 修复
- **Release Workflow Consistency / 发布工作流一致性**: Fixed the GitHub Actions release job dependency so workflow-dispatch releases wait for the build job instead of self-referencing the release job, keeping the current ZIP/tag/store publication path runnable / 修复 GitHub Actions 发布任务依赖关系，使 workflow-dispatch 发布流程正确等待 build 作业而不是错误地自引用 release 作业，保持当前 ZIP/tag/store 发布链路可执行
- **Completion Report Flag Alignment / 完成检查报告参数对齐**: Switched the workflow invocation to the script's supported `-OutputJson` alias so release validation and local smoke commands generate JSON evidence consistently / 将 workflow 中的完成检查调用切换到脚本已支持的 `-OutputJson` 别名，使发布校验与本地冒烟命令都能稳定生成 JSON 证据
- **Working-Set Release Checklist / 当前工作集发布清单**: Updated README and plugin authoring guides so new-plugin follow-up, official smoke validation, and release preparation now all explicitly require syncing the repository root `CHANGELOG.md` alongside plugin/store metadata, and use the real manifest plugin IDs (`custom-mouse`, `shell-integration`, `vive-tool`) for completion-check examples / 更新 README 与插件开发文档，使 new-plugin 后续步骤、官方工作集冒烟校验与发布准备现在都明确要求在插件与 store 元数据之外同步维护仓库根 `CHANGELOG.md`，并在 completion-check 示例中统一使用真实清单插件 ID（`custom-mouse`、`shell-integration`、`vive-tool`）
- **Shell Binary Fetch Validation / Shell 二进制拉取校验**: Verified the current `fetch-shell-binaries.ps1` path still fails against the latest upstream release layout because the fallback source zipball no longer contains a packaged `shell.exe`, documenting the remaining Shell Integration blocker in the working-set release evidence / 已验证当前 `fetch-shell-binaries.ps1` 在最新上游发布布局下仍会失败：回退到 source zipball 后已不再包含可打包的 `shell.exe`，并将该 Shell Integration 剩余阻塞明确记录进当前工作集发布证据

### Changed / 变更
- **Version / 版本**: Bump shared repository/store version metadata to `1.0.31` for this release-workflow/completion-check consistency pass / 为本次 release workflow / completion-check 一致性修正将共享仓库与商店版本元数据提升到 `1.0.31`

## [1.0.28] - 2026-03-29

### Fixed / 修复
- **Build Output Hygiene / 构建产物卫生**: Ignore repository-local `Build/` and all project `obj/` directories so repeated plugin builds no longer leave untracked build artifacts in `git status` / 忽略仓库内 `Build/` 与各项目 `obj/` 目录，避免重复插件构建后在 `git status` 中残留未跟踪构建产物
- **Build Dependencies / 构建依赖**: Resolve host dependency lookup by falling back to the main LenovoLegionToolkit build outputs when `Dependencies\Host` is empty, preventing plugin smoke builds from failing in fresh checkouts / 当 `Dependencies\Host` 为空时回退到主仓库编译输出作为依赖，避免插件冒烟构建在新环境下直接失败
- **SDK Reference / SDK 引用**: Use the shared host dependency path for the SDK reference to avoid missing `LenovoLegionToolkit.Lib` during plugin builds / SDK 引用改用统一的主仓库依赖路径，避免插件构建时找不到 `LenovoLegionToolkit.Lib`

### Changed / 变更
- **Version / 版本**: Bump shared plugin version metadata to `1.0.8` after the repository build hygiene fix / 在修复仓库构建产物卫生后，将共享插件版本号提升到 `1.0.8`
- **Documentation / 文档**: Expanded plugin smoke build instructions with ShellIntegration guidance and host dependency notes / 补充插件冒烟构建说明（ShellIntegration 与依赖回退说明）
- **Evidence / 证据**: Verified by `C:\Users\96152\.openclaw\workspace\attachments\lenovolegiontoolkit-plugins\network-accel-build-clean.log` and `C:\Users\96152\.openclaw\workspace\attachments\lenovolegiontoolkit-plugins\network-accel-status-clean.txt` / 证据见上述 build 与 git status 日志
