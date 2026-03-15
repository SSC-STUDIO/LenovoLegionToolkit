# Changelog / 更新日志

All notable changes to this project will be documented in this file.
此项目的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

## [Unreleased]

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
