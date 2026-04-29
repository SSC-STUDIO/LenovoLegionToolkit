# Network Acceleration Plugin Changelog

All notable changes to this plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.8] - 2026-04-29

### Improved / 改进
- Reworked the settings page and fallback path into a status strip, direct settings form, and command area so the host settings window uses less nested card chrome while preserving existing controls / 将设置页及回退路径改为状态条、直接设置表单和操作区，减少宿主设置窗口中的嵌套卡片装饰，同时保留现有控制项
- Reworked the feature dashboard into a compact status strip, split preset/detail columns, direct telemetry rows, and a plain status footer so the embedded page matches the host's de-carded plugin style / 将功能仪表盘改为紧凑状态条、预设/详情分栏、直接遥测行和普通状态页脚，使嵌入式页面匹配主程序去卡片化后的插件风格
- Switched status colors to host theme resources so feature and settings pages stay readable during Light and Dark automation reviews / 将状态颜色切换为宿主主题资源，使功能页和设置页在浅色与深色自动化审查中都保持可读

## [1.1.7] - 2026-04-20

### Changed / 变更
- Advanced the official store package to `1.1.7` for the current release wave so the manifest, packaged asset, and marketplace metadata stay aligned / 将当前发布批次的官方商店包提升到 `1.1.7`，确保清单、打包资产与商店元数据保持一致

## [1.1.4] - 2026-04-05

### Fixed / 修复
- Removed hardcoded Chinese strings from NetworkAccelerationText.cs fallback values, ensuring all UI text uses proper resource-based localization / 移除 NetworkAccelerationText.cs 中的硬编码中文字符串回退值，确保所有 UI 文本使用正确的资源化本地化

## [1.1.3] - 2026-03-18

### Fixed / 修复
- Repaired simplified Chinese telemetry, mode, and status strings so the Network Acceleration dashboard and settings page now render clean Chinese copy instead of placeholder or mojibake text / 修复简体中文遥测、模式和状态文案，使 Network Acceleration 仪表盘与设置页不再显示占位符或乱码文本

## [1.1.2] - 2026-03-16

### Improved / 改进
- Expanded the telemetry page with traffic-mix progress bars and a burst-history chart driven by the existing download/upload sample stream / 扩展遥测页面，新增基于现有上下行采样流的流量占比进度条与突发历史图
- Tightened telemetry hierarchy so live totals, burst peak, rolling average, mode controls, and quick actions read as one continuous dashboard / 收紧遥测信息层级，将实时总量、突发峰值、滚动平均、模式控制与快速操作整理为连续仪表盘

## [1.1.1] - 2026-03-12

### Fixed / 修复
- Added missing localization keys so the network acceleration UI text resources resolve correctly after recent layout updates / 补齐缺失的本地化键，确保网络加速界面在近期布局更新后能正确显示文本

## [1.1.0] - 2026-03-10

### Added / 新增
- Added a redesigned network dashboard with live throughput metrics, active-adapter summary cards, and a built-in traffic chart / 新增重设计的网络仪表盘，提供实时吞吐指标、活动网卡摘要卡片和内置流量图表

### Improved / 改进
- Reworked the settings page into a clearer policy snapshot layout while keeping the existing optimization controls / 重做设置页为更清晰的策略概览布局，同时保留现有优化控制项
- Expanded plugin localization coverage to match the host application's supported language set via resource-based translations / 通过资源化翻译将插件本地化覆盖范围扩展到与主程序一致的语言集合

## [1.0.4] - 2026-02-28

### Improved / 改进
- Refined feature-page and settings-page UI to match host System Optimization visual language with cleaner card layout, spacing, and status presentation / 优化功能页与设置页界面，采用与主程序系统优化一致的轻量卡片风格、间距与状态展示
- Kept interaction flow simple while preserving all existing quick-action controls and automation IDs / 在保持简洁交互的同时保留现有快速操作控件与自动化测试标识

## [1.0.3] - 2026-02-26

### Fixed / 修复
- Fixed runtime plugin feature/settings UI reliability by adding fallback code-built UI when XAML resources cannot be resolved at host runtime / 修复插件功能页与设置页运行时可靠性：当主程序运行时无法解析 XAML 资源时，自动回退到代码构建 UI
- Updated assembly/runtime metadata for stable plugin loading in marketplace install flow / 更新程序集与运行时元数据，提升插件市场安装流程中的加载稳定性

## [1.0.2] - 2026-02-26

### Added / 新增
- Added plugin feature page and dedicated settings page with runtime controls for quick optimization/reset actions / 新增插件功能页与独立设置页，提供快速优化与网络栈重置操作
- Added persisted plugin options (`AutoOptimizeOnStartup`, `ResetWinsockOnOptimize`, `ResetTcpIpOnOptimize`, preferred mode) / 新增可持久化的插件选项（启动自动优化、Winsock 重置、TCP/IP 重置、偏好模式）

### Fixed / 修复
- Fixed missing plugin settings entry so installed plugin no longer reports "does not provide a settings page" / 修复插件缺少设置入口的问题，避免安装后提示“没有设置页”

## [1.0.1] - 2026-02-25

### Fixed / 修复
- Added plugin metadata attribute for explicit plugin version/minimum host version validation / 添加插件元数据特性，显式声明插件版本与最低主程序版本校验
- Added packaged `plugin.json` manifest for local ZIP import/store metadata consistency / 添加随插件输出的 `plugin.json` 清单，提升本地 ZIP 导入与商店元数据一致性

### Improved / 改进
- Aligned plugin minimum supported LLT version to `3.6.1` / 将插件最低支持 LLT 版本统一到 `3.6.1`

## [1.0.0] - 2026-02-25

### Added
- Initial release
- Basic network acceleration functionality
