# ViveTool Plugin Changelog / ViveTool插件更新日志

All notable changes to this plugin will be documented in this file.
此插件的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

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

