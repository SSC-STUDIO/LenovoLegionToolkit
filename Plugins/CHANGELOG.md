# Changelog / 更新日志

All notable changes to this project will be documented in this file.
此项目的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

## [Unreleased]

### Improved / 改进
- **Custom Mouse**: Refreshed both the profile and Windows-settings pages into a card-based dashboard with live summary tiles for DPI, polling rate, pointer speed, button layout, and cursor-theme mode / 重构 Custom Mouse 的配置页与 Windows 设置页，改为卡片式仪表盘，并增加 DPI、回报率、指针速度、按键布局和光标主题模式的实时摘要
- **Network Acceleration**: Expanded the telemetry dashboard with traffic-mix bars and a burst-history chart while keeping the existing live throughput graph and fallback UI intact / 扩展 Network Acceleration 遥测面板，新增流量占比条和突发历史图，同时保留原有吞吐曲线与回退 UI
- **Shell Integration**: Promoted installation, version, config, and path details into richer overview cards while preserving the same registration actions / 提升 Shell Integration 的安装、版本、配置和路径信息展示，改为概览卡片，同时保留原有注册控制动作
- **ViveTool**: Reworked the settings surface into a stronger hero-plus-actions layout so runtime status, download progress, and binary path management read more cleanly / 重做 ViveTool 设置页布局，采用更清晰的头图区加操作区结构，使运行状态、下载进度和二进制路径管理更易读

### Fixed / 修复
- **Shell Integration / Shell 集成**: Prepared a new `1.0.4` marketplace payload that bundles the verified sibling `Shell` build removing the submenu black-block animation while preserving submenu transparency parity / 准备新的 `1.0.4` 市场包，内置已验证的 sibling `Shell` 修复版本，移除二级菜单黑块动画并保持子菜单透明度一致

### Fixed / 修复
- **Build Output Hygiene / 构建产物卫生**: Ignore repository-local `Build/` and all project `obj/` directories so repeated plugin builds no longer leave untracked build artifacts in `git status` / 忽略仓库内 `Build/` 与各项目 `obj/` 目录，避免重复插件构建后在 `git status` 中残留未跟踪构建产物
- **Build Dependencies / 构建依赖**: Resolve host dependency lookup by falling back to the main LenovoLegionToolkit build outputs when `Dependencies\Host` is empty, preventing plugin smoke builds from failing in fresh checkouts / 当 `Dependencies\Host` 为空时回退到主仓库编译输出作为依赖，避免插件冒烟构建在新环境下直接失败
- **SDK Reference / SDK 引用**: Use the shared host dependency path for the SDK reference to avoid missing `LenovoLegionToolkit.Lib` during plugin builds / SDK 引用改用统一的主仓库依赖路径，避免插件构建时找不到 `LenovoLegionToolkit.Lib`

### Changed / 变更
- **Version / 版本**: Bump shared plugin version metadata to `1.0.8` after the repository build hygiene fix / 在修复仓库构建产物卫生后，将共享插件版本号提升到 `1.0.8`
- **Documentation / 文档**: Expanded plugin smoke build instructions with ShellIntegration guidance and host dependency notes / 补充插件冒烟构建说明（ShellIntegration 与依赖回退说明）
- **Evidence / 证据**: Verified by `C:\Users\96152\.openclaw\workspace\attachments\lenovolegiontoolkit-plugins\network-accel-build-clean.log` and `C:\Users\96152\.openclaw\workspace\attachments\lenovolegiontoolkit-plugins\network-accel-status-clean.txt` / 证据见上述 build 与 git status 日志
