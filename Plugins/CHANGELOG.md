# Changelog / 更新日志

All notable changes to this project will be documented in this file.
此项目的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

## [Unreleased]

### Fixed / 修复
- **Release Workflow Consistency / 发布工作流一致性**: Reworked the plugin release workflow so ZIP names, GitHub release tags, generated `downloadUrl`, and `store.json` IDs all derive from each plugin's canonical `plugin.json` metadata instead of ZIP base names, preventing bad release paths and mismatched plugin IDs in generated store entries / 重构插件发布工作流，使 ZIP 文件名、GitHub Release 标签、生成的 `downloadUrl` 与 `store.json` 中的插件 ID 全部以各插件 `plugin.json` 的规范元数据为准，不再从 ZIP 基名反推，避免生成错误的发布路径和错配的插件 ID
- **Completion Checker Onboarding / 完成度校验接入**: Relaxed `plugin-completion-check.ps1` so non-official plugin IDs can be validated without tripping the hard-coded official-only gate, while still preserving an explicit `-OfficialOnly` mode for stricter repository audits / 放宽 `plugin-completion-check.ps1` 的官方插件硬编码限制，使非官方插件 ID 也能复用同一套校验流程，同时保留显式 `-OfficialOnly` 模式供仓库级严格审计使用
- **Store Release Ordering / 商店发布顺序**: Made `store.json` updates wait for successful release publication and append newly released plugin entries instead of silently dropping plugins that were not already present in the existing store ordering / 让 `store.json` 回写依赖成功的 release 发布，并在生成时追加新发布但尚未存在于旧排序中的插件条目，避免新插件被静默丢弃
- **Vendored Shell Runtime / 内置 Shell 运行时依赖**: Added the minimal `Dependencies/Shell` runtime payload required by `ShellIntegration` so repository validation and packaging no longer depend on the upstream `moudey/Shell` GitHub release layout continuing to ship downloadable binaries / 为 `ShellIntegration` 补入最小 `Dependencies/Shell` 运行时文件集，使仓库校验与打包不再依赖上游 `moudey/Shell` GitHub release 持续提供可直接下载的二进制布局
- **Workflow Dependency Paths / 工作流依赖路径**: Expanded the plugin build workflow path filters to include `Dependencies/**` so vendored runtime dependency updates trigger the same validation/build pipeline as plugin source changes / 扩展插件构建工作流的路径过滤规则，纳入 `Dependencies/**`，让受控运行时依赖更新也能像插件源码变更一样自动触发校验与构建流程
- **Workflow Runtime Maintenance / 工作流运行时维护**: Updated the plugin CI workflow to current official `actions/*` releases and switched ZIP packaging uploads to a dedicated `Build/artifacts` directory so successful builds no longer emit missing-artifact warnings from wildcard root matching / 将插件 CI 工作流升级到当前官方 `actions/*` 版本，并把 ZIP 打包产物统一输出到 `Build/artifacts` 目录，避免成功构建后仍因根目录通配匹配不到文件而产生缺失产物警告

### Improved / 改进
- **Plugin Scaffold And Docs / 插件脚手架与文档**: Added `Scripts/new-plugin.ps1` and rewrote the top-level README plus quick-start/development guides to document the real vendored-host build model, naming conventions, completion check usage, and workflow-dispatch release path instead of the removed legacy zip/tag flow / 新增 `Scripts/new-plugin.ps1`，并重写仓库 README 与快速开始/开发指南，改为说明当前真实使用的 vendored-host 构建模型、命名约定、完成度检查方式以及 `workflow_dispatch` 发布路径，替代已废弃的旧 zip/tag 流程

## [1.1.7] - 2026-03-17

### Fixed / 修复
- **Build Output Hygiene / 构建产物卫生**: Ignore repository-local `Build/` and all project `obj/` directories so repeated plugin builds no longer leave untracked build artifacts in `git status` / 忽略仓库内 `Build/` 与各项目 `obj/` 目录，避免重复插件构建后在 `git status` 中残留未跟踪构建产物
- **Build Dependencies / 构建依赖**: Resolve host dependency lookup by falling back to the main LenovoLegionToolkit build outputs when `Dependencies\Host` is empty, preventing plugin smoke builds from failing in fresh checkouts / 当 `Dependencies\Host` 为空时回退到主仓库编译输出作为依赖，避免插件冒烟构建在新环境下直接失败
- **SDK Reference / SDK 引用**: Use the shared host dependency path for the SDK reference to avoid missing `LenovoLegionToolkit.Lib` during plugin builds / SDK 引用改用统一的主仓库依赖路径，避免插件构建时找不到 `LenovoLegionToolkit.Lib`

### Changed / 变更
- **Version / 版本**: Bump shared plugin version metadata to `1.0.8` after the repository build hygiene fix / 在修复仓库构建产物卫生后，将共享插件版本号提升到 `1.0.8`
- **Documentation / 文档**: Expanded plugin smoke build instructions with ShellIntegration guidance and host dependency notes / 补充插件冒烟构建说明（ShellIntegration 与依赖回退说明）
- **Evidence / 证据**: Verified by `C:\Users\96152\.openclaw\workspace\attachments\lenovolegiontoolkit-plugins\network-accel-build-clean.log` and `C:\Users\96152\.openclaw\workspace\attachments\lenovolegiontoolkit-plugins\network-accel-status-clean.txt` / 证据见上述 build 与 git status 日志
