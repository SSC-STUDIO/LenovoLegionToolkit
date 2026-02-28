# Custom Mouse Plugin Changelog / CustomMouse插件更新日志

All notable changes to this plugin will be documented in this file.
此插件的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.6] - 2026-02-27

### Added / 新增
- Restored legacy cursor resource pack (`W11-CC-V2.2-HDPI`) including bundled `Install.inf` workflow and missing light-theme classic assets / 恢复历史鼠标资源包（`W11-CC-V2.2-HDPI`），包含 `Install.inf` 自动配置流程及缺失的浅色主题 classic 资源
- Added runtime cursor-theme apply path that detects current Windows light/dark mode and applies matching cursor scheme / 新增运行时光标主题应用链路：检测当前 Windows 明暗模式并应用对应光标方案

### Changed / 变更
- Upgraded settings page to expose auto-theme cursor option and one-click \"Apply Cursor Theme Now\" operation / 升级设置页，提供“主题跟随光标样式”开关与“立即应用光标主题”按钮
- Auto-theme disable action now restores previously backed-up cursor scheme / 禁用主题跟随动作时会恢复此前备份的原始光标方案

## [1.0.5] - 2026-02-27

### Changed / 变更
- Converted Custom Mouse to a System Optimization extension entry (no standalone feature page) so `Open` routes users to its optimization category in host / 将 Custom Mouse 转为系统优化扩展入口（不再提供独立功能页），主程序中点击 `Open` 会进入对应系统优化分类
- Added plugin-provided optimization category and actions for cursor auto-theme mode enable/disable state management / 新增插件系统优化分类与“鼠标样式跟随系统主题”启用/停用动作状态管理

### Improved / 改进
- Updated persisted settings with `AutoThemeCursorStyle` flag to support extension-mode workflow and automated checks / 配置持久化新增 `AutoThemeCursorStyle` 标志，支持扩展模式流程与自动化校验

## [1.0.4] - 2026-02-26

### Fixed / 修复
- Fixed runtime plugin page/settings blank-content failures by adding fallback code-built UI when WPF XAML resource loading fails in host plugin context / 修复运行时插件功能页与设置页空白问题：当主程序插件上下文中 WPF XAML 资源加载失败时，自动回退到代码构建 UI
- Fixed assembly metadata consistency for stable plugin runtime loading and page initialization / 修复程序集元数据一致性，提升插件运行时加载与页面初始化稳定性

## [1.0.3] - 2026-02-26

### Added / 新增
- Added plugin feature page and settings page so installed plugin entries open real UI instead of blank placeholders / 新增插件功能页和设置页，避免已安装插件打开空白界面
- Added Windows mouse integration for pointer speed and left/right button swap with persisted plugin configuration / 新增 Windows 鼠标参数集成（指针速度、左右键交换）并持久化插件配置

### Improved / 改进
- Improved runtime behavior by loading/saving plugin configuration through `PluginBase.Configuration` for stable session-to-session values / 通过 `PluginBase.Configuration` 读写配置，提升跨会话配置稳定性

## [1.0.2] - 2026-02-25

### Fixed / 修复
- Added plugin metadata attribute for version/minimum host checks so host-side compatibility detection works consistently / 补充插件元数据特性（版本与最低主程序版本），确保主程序兼容性检测行为一致
- Added packaged `plugin.json` manifest to improve ZIP import/store metadata consistency / 添加随插件输出的 `plugin.json` 清单文件，改进 ZIP 导入与商店元数据一致性

### Improved / 改进
- Aligned plugin minimum supported LLT version to `3.6.1` / 将插件最低支持 LLT 版本统一到 `3.6.1`

## [1.0.1] - 2026-02-25

### Fixed / 修复
- Fixed CustomMouse test project references and target framework to match current plugin project structure / 修复 CustomMouse 测试项目引用与目标框架，使其与当前插件工程结构一致
- Fixed test runtime asset cleanup conflict that caused `dotnet test` host startup failures / 修复测试运行时文件被清理导致 `dotnet test` 启动失败的问题

### Improved / 改进
- Updated CustomMouse automated tests to align with the current plugin API behavior / 更新 CustomMouse 自动化测试以匹配当前插件 API 行为

## [1.0.0] - 2026-02-25

### Added
- Initial release
- Basic mouse settings support
