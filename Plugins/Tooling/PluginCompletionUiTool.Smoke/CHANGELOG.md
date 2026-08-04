# Changelog / 更新日志

All notable changes to this tool will be documented in this file.  
此工具的所有重要变更将记录在此文件中。

## [Unreleased]

## [1.0.0] - 2026-02-26

### Added / 新增
- Added standalone desktop UI smoke runner for `PluginCompletionUiTool` based on Windows UI Automation / 新增基于 Windows UI Automation 的 `PluginCompletionUiTool` 独立桌面 UI 冒烟运行器
- Added end-to-end automated flow: launch UI, set repo path, toggle options, click run, wait for completion, and validate generated JSON report / 新增端到端自动化流程：启动 UI、设置仓库路径、切换选项、点击运行、等待完成并校验生成的 JSON 报告
- Added cleanup handling for generated smoke report artifacts after successful validation / 新增成功校验后的冒烟报告产物清理逻辑
