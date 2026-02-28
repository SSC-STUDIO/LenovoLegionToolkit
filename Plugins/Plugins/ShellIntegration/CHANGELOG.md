# Shell Integration Plugin Changelog

All notable changes to this plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2026-02-26

### Fixed / 修复
- Fixed runtime plugin settings UI reliability by adding fallback code-built UI when XAML resources cannot be resolved at host runtime / 修复插件设置页运行时可靠性：当主程序运行时无法解析 XAML 资源时，自动回退到代码构建 UI
- Updated assembly/runtime metadata for stable plugin loading in marketplace install and configure flow / 更新程序集与运行时元数据，提升插件市场安装与配置流程中的加载稳定性

## [1.0.2] - 2026-02-26

### Added / 新增
- Added plugin settings page for shell registration control and style-editor entry point / 新增 Shell 插件设置页，可执行注册控制并打开样式编辑入口
- Added plugin-provided Windows Optimization category (`Nilesoft Shell`) with enable/disable actions / 新增插件提供的 Windows 优化分类（Nilesoft Shell），支持启用/禁用操作

### Improved / 改进
- Aligned plugin behavior with host navigation expectations by exposing optimization extension instead of sidebar feature page / 对齐主程序导航预期：通过系统优化扩展提供能力，而非侧边栏功能页

## [1.0.1] - 2026-02-25

### Fixed / 修复
- Added plugin metadata attribute for explicit plugin version/minimum host version validation / 添加插件元数据特性，显式声明插件版本与最低主程序版本校验
- Added packaged `plugin.json` manifest for local ZIP import/store metadata consistency / 添加随插件输出的 `plugin.json` 清单，提升本地 ZIP 导入与商店元数据一致性

### Improved / 改进
- Aligned plugin minimum supported LLT version to `3.6.1` / 将插件最低支持 LLT 版本统一到 `3.6.1`

## [1.0.0] - 2026-02-25

### Added
- Initial release
- Basic shell integration functionality

