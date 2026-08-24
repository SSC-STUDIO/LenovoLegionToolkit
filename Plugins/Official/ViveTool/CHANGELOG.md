# ViveTool Plugin Changelog / ViveTool插件更新日志

All notable changes to this plugin will be documented in this file.
此插件的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

## [Unreleased]

## [2.0.0] - 2026-08-24

### Changed / 变更
- Promoted the 6.0 train to stable **2.0.0** on `plugin-catalog` (`minHostVersion` **6.0.0**). v5.0.2 hosts keep installing 1.x from the same catalog / 将 6.0 列车提升为稳定 **2.0.0** 并发布到 `plugin-catalog`（`minHostVersion` 为 **6.0.0**）。v5.0.2 宿主仍从同一目录安装 1.x

### Fixed / 修复
- Rejected path-traversal, device, and Windows-directory paths for ViVeTool path assignment, feature import, and feature export / 拒绝路径穿越、设备路径和 Windows 目录路径用于 ViVeTool 路径设置、功能导入与导出
- Pinned ViVeTool download to the v0.3.4 release asset, enforced ZIP/file hash and size limits, and extracted only required runtime files / 将 ViVeTool 下载固定到 v0.3.4 发布资源，强制 ZIP/文件哈希与体积限制，并只解压必需运行时文件
- Merged imported features into the in-memory cache without duplicating same-collection entries or dropping existing searchable flags / 将导入的功能合并进内存缓存，避免同一集合重复添加，也不丢弃已有可搜索标志

## [2.0.0-preview.1] - 2026-08-13

### Changed / 变更
- Major bump onto the 6.0 preview train: `minHostVersion` is **6.0.0**, and the package publishes to `plugin-catalog-preview` rather than the stable `plugin-catalog` 1.x feed / 进入 6.0 预览列车：`minHostVersion` 为 **6.0.0**，包发布到 `plugin-catalog-preview`，不覆盖稳定 `plugin-catalog` 的 1.x

## [1.2.2] - 2026-05-20

### Added / 新增
- Added feature-list export and a status filter with a live enabled/disabled/default/unknown summary for faster feature-flag review / 新增功能列表导出与状态筛选，并显示启用、禁用、默认、未知状态的实时摘要，便于快速审查功能标志

### Improved / 改进
- Reworked the feature toolbar into separate search/count and command rows, and tightened the DataGrid header and row styling so feature-flag management is less cramped in the host page / 将功能页工具栏拆分为搜索/计数和命令两行，并收紧 DataGrid 表头与行样式，使功能标志管理在宿主页中不再拥挤
- Polished the code-built fallback feature page with host-aligned cards, warning/status blocks, toolbar controls, and a full feature DataGrid so the page remains usable if XAML resource loading falls back / 优化代码构建的功能页回退界面，补齐与宿主一致的卡片、警告/状态区、工具栏控件和完整功能表格，使 XAML 资源回退时界面仍可正常使用

### Fixed / 修复
- Delayed feature-filter initialization until the host page controls are ready, preventing a null reference when the status filter raises its first selection event during page creation / 将功能筛选初始化延后到宿主页控件就绪后执行，避免状态筛选框在页面创建时首次触发选择事件导致空引用
- Resolved bundled ViVeTool discovery when the host loads the plugin from the local plugin directory or a stream-loaded assembly, without depending on host-internal plugin path types / 修复宿主从本地插件目录或流加载程序集时无法发现内置 ViVeTool 的问题，并避免依赖宿主内部插件路径类型
- Tolerated duplicate configured feature IDs when overlaying `/query` output and fixed search filtering to stay on the UI dispatcher / 合并 `/query` 输出时容忍重复功能 ID，并修复搜索筛选过程中的 UI 线程访问问题
- Cancelled pending delayed settings saves before explicit saves so user-selected ViVeTool paths are not overwritten by stale background writes / 显式保存设置前取消挂起的延迟保存，避免用户选择的 ViVeTool 路径被旧的后台写入覆盖

## [1.2.1] - 2026-04-29

### Improved / 改进
- Reworked the feature page around a DataGrid-first layout and simplified settings into one direct form with inline status and command rows, reducing nested card chrome while keeping smoke AutomationIds stable / 将功能页改为以 DataGrid 为主体的布局，并把设置页简化为单表单、内联状态和命令区，减少嵌套卡片同时保留冒烟 AutomationId 稳定性
- Added stable AutomationIds to the feature/settings pages and kept fallback UI controls in parity so smoke automation can verify both render paths / 为功能页与设置页补充稳定 AutomationIds，并保持 fallback UI 控件对齐，使冒烟自动化可验证两种渲染路径
- Refined the feature management page with icon-led import, refresh, settings, enable, and disable actions plus a clearer warning block for safer feature-flag workflows / 打磨功能管理页，为导入、刷新、设置、启用与禁用操作补充图标，并强化警告区呈现，让功能标志操作流程更清晰安全
- Switched feature status row colors and settings status colors to host theme resources for reliable Light and Dark rendering / 将功能状态行颜色和设置页状态颜色切换为宿主主题资源，提升浅色与深色渲染可靠性

## [1.2.0] - 2026-04-20

### Changed / 变更
- Advanced the official store package to `1.2.0` for the current release wave so the manifest, packaged asset, and marketplace metadata stay aligned / 将当前发布批次的官方商店包提升到 `1.2.0`，确保清单、打包资产与商店元数据保持一致

## [1.1.7] - 2026-03-17

### Fixed / 修复
- Replaced the host-facing feature page and settings page roots with embeddable controls so ViVeTool can open inside the LLT plugin wrapper and settings dialog without `Page must have Window or Frame parent` exceptions / 将面向宿主的功能页与设置页根元素改为可嵌入控件，使 ViVeTool 可以在 LLT 插件包装页和设置对话框中正常打开，不再触发 `Page must have Window or Frame parent` 异常
- Moved feature-list loading state updates back onto the UI dispatcher to avoid cross-thread access errors while refreshing ViVeTool status and flags / 将功能列表加载过程中的状态更新切回 UI Dispatcher，避免刷新 ViVeTool 状态与功能标志时出现跨线程访问错误

## [1.1.6] - 2026-03-16

### Improved / 改进
- Reworked the settings page into a stronger hero-plus-actions layout so current runtime status, download progress, and external actions are easier to scan / 重做设置页为更清晰的头图区加操作区布局，使运行状态、下载进度和外部操作更易读
- Tightened the binary path management block with clearer hierarchy for bundled-path guidance, browse, and config import actions / 收紧二进制路径管理区的信息层级，更清晰地展示内置路径说明、浏览与配置导入动作

## [1.1.5] - 2026-03-10

### Improved / 改进
- Expanded plugin localization coverage to match the host application's supported language set by adding satellite resource files for the remaining host locales / 通过为主程序其余语言补齐卫星资源文件，将插件本地化覆盖范围扩展到与主程序一致

## [1.1.4] - 2026-02-28

### Added / 新增
- Bundled ViVeTool runtime files are now shipped inside the plugin package by default (`Bundled/ViVeTool.exe` and required dependencies) / 现在插件默认内置 ViVeTool 运行时文件（`Bundled/ViVeTool.exe` 及必需依赖）

### Changed / 变更
- Runtime resolution order updated to prioritize user custom path first, then bundled runtime, and finally fallback discovery paths / 运行时路径解析顺序调整为：用户自定义路径优先，其次插件内置运行时，最后再走回退查找路径
- Settings page path description now clearly states bundled default + custom override behavior / 设置页路径说明已明确“默认内置 + 可自定义覆盖”行为

## [1.1.3] - 2026-02-28

### Improved / 改进
- Polished ViVeTool settings UI with cleaner host-aligned card layout, clearer action grouping, and simplified visual hierarchy / 优化 ViVeTool 设置界面，采用与主程序一致的简洁卡片布局、更清晰的操作分组与更简化的信息层级
- Updated fallback settings page layout to maintain similar appearance when XAML resource loading falls back to code-built UI / 同步升级设置页回退 UI（代码构建路径），确保 XAML 失败时仍保持接近的界面风格

## [1.1.2] - 2026-02-26

### Improved / 改进
- Standardized assembly naming and version metadata for more stable plugin loading behavior in the host runtime / 统一程序集命名与版本元数据，提升主程序运行时中的插件加载稳定性

## [1.1.1] - 2026-02-25

### Fixed / 修复
- Unified plugin ID to `vive-tool` across plugin attribute and manifest to match store/install identity / 在插件特性与清单中统一插件 ID 为 `vive-tool`，确保商店与安装标识一致
- Updated `Plugin.json` metadata (version/minLLTVersion/repository/author) to match current repository and host compatibility / 更新 `Plugin.json` 元数据（版本、minLLTVersion、仓库地址、作者）以匹配当前仓库和主程序兼容要求

### Improved / 改进
- Raised minimum supported LLT version to `3.6.1` for consistent plugin ecosystem compatibility / 将最低支持 LLT 版本提升到 `3.6.1`，提升插件生态兼容一致性

## [1.1.0] - 2026-02-25

### Added / 新增
- Updated for LLT v3.6.0 / 更新适配 LLT v3.6.0

## [1.0.0] - 2026-02-05

### Added / 新增
- Initial release / 初始发布
- ViveTool feature configuration interface / ViveTool功能配置界面
- Advanced feature management / 高级功能管理
- ViveTool functionality for Windows feature management / 用于Windows功能管理的ViveTool功能

