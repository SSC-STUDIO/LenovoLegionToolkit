# Network Acceleration Plugin Changelog

All notable changes to this plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

