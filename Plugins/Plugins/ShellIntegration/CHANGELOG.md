# Shell Integration Plugin Changelog

All notable changes to this plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.12] - 2026-05-20

### Added / 新增
- Added managed Shell config sync/reset, profile import/export, managed-config folder access, and built-in style presets from the settings page / 在设置页新增托管 Shell 配置同步/重置、配置档导入/导出、托管配置目录入口和内置样式预设

### Improved / 改进
- Refined the settings status summary so registration, version, and config state wrap cleanly in narrower host settings windows while keeping every Shell action entry point stable / 优化设置页状态摘要，使注册状态、版本和配置状态在较窄的宿主设置窗口中也能自然换行，同时保持所有 Shell 操作入口稳定

## [1.0.11] - 2026-04-29

### Improved / 改进
- Reworked the settings page into a lightweight status strip, action row, and direct path/info sections so Shell Integration matches the host's de-carded settings shell with less nested chrome / 将设置页改为轻量状态条、操作行和直接路径/信息区，减少嵌套装饰以匹配主程序去卡片化后的设置壳
- Tightened the settings overview into a compact first-screen summary so registration, version, config, and quick actions stay readable inside the host settings window without wasting vertical space in Light and Dark themes / 将设置页总览收紧为首屏紧凑摘要，让注册状态、版本、配置和快速操作在宿主设置窗口内的浅色/深色主题下都更易阅读，减少无效纵向留白
- Reworked the settings page and fallback style dialog toward direct path rows, wrapped action rows, and a more host-consistent Dark/Light layout in both the main app and PluginWorkbench / 重做设置页与 fallback 样式弹窗，改为直接路径行、可换行的操作区，以及在主程序与 PluginWorkbench 中都更贴近宿主的深浅色布局
- Tightened standalone preview compatibility so PluginWorkbench now resolves the real Shell Integration assembly from build outputs and can keep the settings shell available in both Dark and Light reviews / 收紧独立预览兼容性，使 PluginWorkbench 现在会从构建输出中解析真正的 Shell Integration 程序集，并能在深浅色审查中稳定保留设置页宿主
- Added stable AutomationIds and host theme status colors to settings and style-dialog controls so Workbench smoke and manual reviews can target Shell actions consistently / 为设置页和样式对话框控件补充稳定 AutomationId 与宿主主题状态颜色，使 Workbench 冒烟和人工审查能一致定位 Shell 操作

## [1.0.10] - 2026-04-20

### Changed / 变更
- Advanced the official store package to `1.0.10` for the current release wave so the manifest, packaged asset, and marketplace metadata stay aligned / 将当前发布批次的官方商店包提升到 `1.0.10`，确保清单、打包资产与商店元数据保持一致

## [1.0.9] - 2026-03-28

### Fixed / 修复
- Turned missing Shell runtime binaries from a warning into a hard build error so broken Shell Integration packages can no longer be produced when `shell.exe` was not fetched successfully / 将 Shell 运行时二进制缺失从警告升级为硬错误，避免在 `shell.exe` 拉取失败时继续产出不可运行的 Shell Integration 包
- Routed the settings page registration-state probe through the plugin core's merged-classes check so the UI no longer reports a partially registered Shell state differently from the real action logic / 让设置页的 Shell 注册态检测直接复用插件核心的 merged-classes 检查逻辑，避免 UI 对“部分注册”的状态判断与真实动作逻辑不一致

## [1.0.6] - 2026-03-18

### Fixed / 修复
- Repaired simplified Chinese resource text in the marketplace card and settings page so Shell Integration no longer shows mojibake or partial English fallback in Chinese mode / 修复插件市场卡片和设置页中的简体中文资源文本，使 Shell Integration 在中文模式下不再出现乱码或部分英文回退

## [1.0.5] - 2026-03-16

### Improved / 改进
- Refreshed the settings page into a clearer overview layout with dedicated cards for detection state, version, config path, and install path / 重做设置页为更清晰的总览布局，单独展示检测状态、版本、配置路径与安装路径
- Kept the existing enable/disable/style/config actions while regrouping them into a richer quick-actions section / 保留现有启用、禁用、样式和配置操作，并重新整理为更完整的快速操作区

## [1.0.4] - 2026-03-15

### Fixed / 修复
- Bundled the verified Shell build that removes the abrupt black block during submenu opening while keeping submenu and main-menu transparency aligned / 打包已验证的 Shell 修复版本，去除二级菜单打开时突兀的黑块，同时保持子菜单与主菜单透明度一致
- Replaced the problematic marketplace payload built from the older Shell submenu animation path / 替换基于旧版二级菜单动画路径构建的问题市场包

### Added / 新增
- Added quick actions to open the Shell folder and config file from the settings page / 新增设置页快捷入口，可打开 Shell 文件夹与配置文件
- Documented Shell binary source links in plugin metadata/readme / 在插件元数据与说明中补充 Shell 二进制来源链接

### Improved / 改进
- Show detected Shell version in the settings status panel / 在状态区域显示检测到的 Shell 版本

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

