# Changelog / 更新日志

All notable changes to this tool will be documented in this file.  
此工具的所有重要变更将记录在此文件中。

The format is based on Keep a Changelog and this tool follows Semantic Versioning.  
格式遵循 Keep a Changelog，并使用语义化版本。

## [Unreleased]

### Improved / 改进
- Added explicit UI Automation IDs on primary controls to support deterministic desktop automation and smoke testing / 为主要控件补充显式 UI Automation ID，支持稳定的桌面自动化与冒烟测试

## [1.0.0] - 2026-02-26

### Added / 新增
- Added standalone WPF UI for plugin completion validation with repository path selection, plugin ID filtering, and run options (`Configuration`, `Skip Build`, `Skip Tests`) / 新增独立 WPF 可视化校验界面，支持仓库路径选择、插件 ID 筛选及运行选项（`Configuration`、`Skip Build`、`Skip Tests`）
- Added live process log panel for checker script output (stdout/stderr) / 新增校验脚本实时日志面板（stdout/stderr）
- Added JSON report parsing and tabular result views for plugin summary and step details / 新增 JSON 报告解析与结果表格展示（插件汇总与步骤明细）
- Added quick actions to open repository folder and generated report file / 新增快速操作：打开仓库目录与打开生成报告文件

### Improved / 改进
- Improved independence by marking tool project as `IsPluginToolProject=true` so it stays outside plugin-only build/publish cleanup targets / 通过设置 `IsPluginToolProject=true` 提升独立性，使工具项目不受插件专用构建/清理目标影响
- Improved integration with existing completion checker script by reusing `plugin-completion-check.ps1` and standardized JSON report path conventions / 改进与现有完成度检查脚本集成：复用 `plugin-completion-check.ps1` 并统一 JSON 报告路径约定
