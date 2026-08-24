# Custom Mouse Plugin Changelog / CustomMouse插件更新日志

All notable changes to this plugin will be documented in this file.
此插件的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2026-08-24

### Changed / 变更
- Promoted the 6.0 train to stable **2.0.0** on `plugin-catalog` (`minHostVersion` **6.0.0**). v5.0.2 hosts keep installing 1.x from the same catalog / 将 6.0 列车提升为稳定 **2.0.0** 并发布到 `plugin-catalog`（`minHostVersion` 为 **6.0.0**）。v5.0.2 宿主仍从同一目录安装 1.x

## [2.0.0-preview.1] - 2026-08-13

### Changed / 变更
- Major bump onto the 6.0 preview train: `minHostVersion` is **6.0.0**, and the package publishes to `plugin-catalog-preview` rather than the stable `plugin-catalog` 1.x feed / 进入 6.0 预览列车：`minHostVersion` 为 **6.0.0**，包发布到 `plugin-catalog-preview`，不覆盖稳定 `plugin-catalog` 的 1.x

## [1.0.16] - 2026-05-18

### Improved / 改进
- Refined the settings status summary so mouse speed, button layout, and cursor theme wrap cleanly in narrower host settings windows while preserving the existing controls and automation IDs / 优化设置页状态摘要，使鼠标速度、按键布局和光标主题在较窄的宿主设置窗口中也能自然换行，同时保留现有控件和自动化标识

- Rebuilt the fallback settings layout with grouped sections, live speed labels, and wrapping action buttons so host settings windows no longer clip Custom Mouse commands.
- Resolved cursor resources from local plugin installation directories when the host loads the plugin from memory, restoring the custom cursor apply button in smoke and host-style runs.

## [1.0.15] - 2026-04-29

### Improved / 改进
- Reworked the settings page into a lighter status strip, direct form, and action area so the host settings window avoids nested card chrome while keeping mouse controls visible / 将设置页改为更轻的状态条、直接表单和操作区，减少宿主设置窗口中的嵌套卡片装饰，同时保持鼠标控制项可见
- Tightened the settings overview into a compact single-row summary so the first viewport now keeps the key mouse controls and action buttons visible without scrolling in both host Light and Dark themes / 将设置页总览收紧为首屏单行摘要，使关键鼠标控件和操作按钮在宿主浅色/深色主题下都能尽量保持首屏可见，减少滚动后才能操作的情况
- Tightened the host settings presentation to use lighter nesting, calmer status chrome, and spacing that better matches the main app plugin settings shell in both Dark and Light themes / 收紧宿主设置页呈现，减轻嵌套层级、状态条视觉重量，并让深浅色下的间距与主程序插件设置壳更一致
- Hardened settings AutomationIds and status colors so fallback and XAML paths remain testable and readable in both Light and Dark themes / 加固设置页 AutomationId 与状态颜色，使 fallback 与 XAML 路径在浅色和深色主题下都更易测试且保持可读

### Fixed / 修复
- Removed the accidental standalone feature page so Custom Mouse again behaves like a settings-plus-Windows Optimization plugin in host surfaces / 移除误暴露的独立功能页，使 Custom Mouse 在宿主界面中重新回到“仅设置页 + 系统优化扩展”的插件模型

## [1.0.14] - 2026-04-20

### Changed / 变更
- Advanced the official store package to `1.0.14` for the current release wave so the manifest, packaged asset, and marketplace metadata stay aligned / 将当前发布批次的官方商店包提升到 `1.0.14`，确保清单、打包资产与商店元数据保持一致

## [1.0.13] - 2026-03-28

### Fixed / 修复
- Moved the INF-based cursor-theme apply path off the UI-blocking `WaitForExit` call and made lifecycle persistence/restore operations run deterministically instead of fire-and-forget so theme switching no longer freezes the settings page and install/uninstall state is less likely to drift / 将基于 INF 的光标主题应用流程从阻塞 UI 的 `WaitForExit` 调用改为真正异步等待，并让安装/卸载生命周期中的持久化与恢复操作改为确定性执行，减少设置页切换主题卡顿以及安装/卸载状态落盘漂移

## [1.0.9] - 2026-03-18

### Fixed / 修复
- Restored simplified Chinese cursor status and failure messages across plugin pages, settings, and optimization flows so Chinese UI no longer shows mojibake or English fallback / 修复插件页面、设置页与系统优化流程中的简体中文光标状态和失败消息，避免中文界面再出现乱码或英文回退

## [1.0.8] - 2026-03-16

### Improved / 改进
- Reworked the feature page into a card-based live profile dashboard with at-a-glance DPI and polling summaries / 重做功能页为卡片式实时配置仪表盘，提供 DPI 与回报率摘要
- Reworked the settings page into a richer Windows mouse overview with live pointer-speed, button-layout, and cursor-theme state summaries / 重做设置页为更完整的 Windows 鼠标总览，并增加指针速度、按键布局与光标主题状态摘要

## [1.0.7] - 2026-03-10

### Improved / 改进
- Expanded plugin localization coverage to match the host application's supported language set via resource-based translations / 通过资源化翻译将插件本地化覆盖范围扩展到与主程序一致的语言集合

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
