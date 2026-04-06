# Changelog / 更新日志

All notable changes to this project will be documented in this file.
此项目的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

## [1.0.36] - 2026-04-05

### Added / 添加
- **Architecture Documentation / 架构文档**: Added comprehensive `Docs/ARCHITECTURE.md` documenting system design, dependency relationships, localization architecture, lifecycle flow, test coverage (523 tests), security practices, and performance optimization strategies / 添加完整的 `Docs/ARCHITECTURE.md` 记录系统设计、依赖关系、本地化架构、生命周期流程、测试覆盖（523 测试）、安全实践和性能优化策略
- **Coding Standards Guide / 编码规范指南**: Added detailed `Docs/CODING_STANDARDS.md` covering naming conventions, async/await patterns, exception handling, resource management, process execution security, WPF UI patterns, localization rules, forbidden anti-patterns, with concrete examples / 添加详细的 `Docs/CODING_STANDARDS.md` 涵盖命名约定、async/await 模式、异常处理、资源管理、进程执行安全、WPF UI 模式、本地化规则、禁止的反模式，附带具体示例
- **EditorConfig Standardization / EditorConfig 标准化**: Added `.editorconfig` for automatic code style enforcement across IDEs, including naming rules (interfaces `I*`, private fields `_camelCase`), code quality rules, pattern matching preferences, and file-scoped namespace requirements / 添加 `.editorconfig` 以在 IDE 间自动强制代码风格，包括命名规则（接口 `I*`、私有字段 `_camelCase`）、代码质量规则、模式匹配偏好和文件作用域命名空间要求

### Changed / 变更
- **Documentation Index / 文档索引**: Updated `README.md` to reference new architecture and coding standards documentation / 更新 `README.md` 以引用新的架构和编码规范文档
- **Test Quality Improvements / 测试质量改进**: Removed `ConfigureAwait(false)` from xUnit tests (xUnit1030), replaced `Assert.True().EndsWith()` with `Assert.EndsWith()` (xUnit2009), converted blocking `.Result` calls to async `await` (xUnit1031), fixed unused theory parameters (xUnit1026/xUnit1011) / 移除 xUnit 测试中的 `ConfigureAwait(false)`（xUnit1030），将 `Assert.True().EndsWith()` 替换为 `Assert.EndsWith()`（xUnit2009），将阻塞的 `.Result` 调用转换为异步 `await`（xUnit1031），修复未使用的理论参数（xUnit1026/xUnit1011）

### Fixed / 修复
- **SDK Dependency Architecture / SDK 依赖架构**: Fixed circular dependency between SDK and Shared projects by reversing the dependency flow; SDK now correctly references Shared instead of Shared referencing SDK, enabling proper HttpClient singleton usage across all plugins / 修复 SDK 与 Shared 项目间的循环依赖，反转依赖流向；SDK 现正确引用 Shared 而非 Shared 引用 SDK，使所有插件能正确使用 HttpClient singleton
- **HttpClient Consolidation / HttpClient 合并**: Removed duplicate HttpClient singleton from SDK PluginBase; GetSharedHttpClient() now delegates to HttpClientManager.GetSharedClient() for centralized socket exhaustion prevention / 移除 SDK PluginBase 中的重复 HttpClient singleton；GetSharedHttpClient() 现委托给 HttpClientManager.GetSharedClient() 以集中防止 socket 耗尽
- **Runtime Token API / 运行时令牌 API**: Made GetRuntimeCancellationToken() virtual in SDK PluginBase to allow derived classes with runtime implementations to properly override instead of misleading placeholder that always returns CancellationToken.None / 将 GetRuntimeCancellationToken() 在 SDK PluginBase 中改为 virtual，使有运行时实现的派生类能正确覆写，而非总是返回 CancellationToken.None 的误导性占位符
- **Parameter Validation / 参数验证**: Added ArgumentNullException.ThrowIfNull() to WpfFallbackHelper.TryInitializeComponent() for proper fallbackBuilder parameter validation (CA1062) / 为 WpfFallbackHelper.TryInitializeComponent() 添加 ArgumentNullException.ThrowIfNull() 以正确验证 fallbackBuilder 参数（CA1062）

## [1.0.35] - 2026-03-31

### Changed / 变更
- **Version / 版本**: Bump shared repository/store version metadata to `1.0.35` for working-set release consistency / 将共享仓库与商店版本元数据提升到 `1.0.35` 以保持工作集发布一致性

### Verified / 验证
- **Working Set Release Validation / 工作集发布验证**: Ran plugin completion check against `custom-mouse`, `shell-integration`, `vive-tool` / 对 `custom-mouse`、`shell-integration`、`vive-tool` 执行插件完成检查
  - ✅ CustomMouse: PASS (0 failures, 1 warning) / 通过（0 失败，1 警告）
  - ❌ ShellIntegration: FAIL (4 failures, 1 warning) — blocked by upstream `shell.exe` fetch failure, documented since 1.0.31 / 失败（4 失败，1 警告）— 因上游 `shell.exe` 拉取失败阻塞，自 1.0.31 起已记录
  - ✅ ViveTool: PASS (0 failures, 1 warning) / 通过（0 失败，1 警告）
- **Evidence / 证据**: `artifacts/plugin-completion-check-latest.json`, `working-set-release-smoke-test-latest.log` / 见仓库 artifacts 与附件日志

## [1.0.34] - 2026-03-31

### Fixed / 修复
- **CustomMouse Async Lifecycle / CustomMouse 异步生命周期**: Converted INF-based cursor theme application to truly async pattern with proper timeout handling and process cleanup; lifecycle persistence operations now use deterministic execution via `RunLifecycleTask` instead of fire-and-forget / 将基于 INF 的光标主题应用转换为真正的异步模式，添加超时处理和进程清理；生命周期持久化操作现在通过 `RunLifecycleTask` 确定性执行而非 fire-and-forget
- **ShellIntegration Build Verification / ShellIntegration 构建验证**: Added hard build-time check for Shell runtime binaries to prevent packaging broken releases when shell.exe fetch fails / 添加 Shell 运行时二进制文件的硬构建时检查，防止在 shell.exe 拉取失败时打包损坏的发布包

### Changed / 变更
- **Version / 版本**: Bump shared repository/store version metadata to `1.0.34` for plugin lifecycle and build verification improvements / 将共享仓库与商店版本元数据提升到 `1.0.34` 以包含插件生命周期和构建验证改进

## [1.0.33] - 2026-03-31

### Fixed / 修复
- **Version Alignment / 版本对齐**: Bump shared repository/store version metadata to `1.0.33` for working-set release consistency / 将共享仓库与商店版本元数据提升到 `1.0.33` 以保持工作集发布一致性

## [1.0.32] - 2026-03-31

### Fixed / 修复
- **Plugin Completion Check Parameter Normalization / 插件完成检查参数规范化**: Fixed `plugin-completion-check.ps1` to accept both space-separated PowerShell array syntax and comma-separated single string for `-PluginIds` parameter, ensuring consistent behavior across README examples and actual usage / 修复 `plugin-completion-check.ps1` 以同时接受空格分隔的 PowerShell 数组语法和逗号分隔的单一字符串作为 `-PluginIds` 参数，确保 README 示例与实际使用行为一致
- **Documentation Plugin ID Consistency / 文档插件 ID 一致性**: Corrected plugin ID references in README and documentation from `vivetool` to the actual store.json ID `vive-tool` / 修正 README 和文档中的插件 ID 引用，从 `vivetool` 改为实际的 store.json ID `vive-tool`

### Changed / 变更
- **Version / 版本**: Bump shared repository/store version metadata to `1.0.32` for parameter handling fix and documentation alignment / 为参数处理修复和文档对齐将共享仓库与商店版本元数据提升到 `1.0.32`

## [1.0.31] - 2026-03-31

### Fixed / 修复
- **Release Workflow Consistency / 发布工作流一致性**: Fixed the GitHub Actions release job dependency so workflow-dispatch releases wait for the build job instead of self-referencing the release job, keeping the current ZIP/tag/store publication path runnable / 修复 GitHub Actions 发布任务依赖关系，使 workflow-dispatch 发布流程正确等待 build 作业而不是错误地自引用 release 作业，保持当前 ZIP/tag/store 发布链路可执行
- **Completion Report Flag Alignment / 完成检查报告参数对齐**: Switched the workflow invocation to the script's supported `-OutputJson` alias so release validation and local smoke commands generate JSON evidence consistently / 将 workflow 中的完成检查调用切换到脚本已支持的 `-OutputJson` 别名，使发布校验与本地冒烟命令都能稳定生成 JSON 证据
- **Working-Set Release Checklist / 当前工作集发布清单**: Updated README and plugin authoring guides so new-plugin follow-up, official smoke validation, and release preparation now all explicitly require syncing the repository root `CHANGELOG.md` alongside plugin/store metadata, and use the real manifest plugin IDs (`custom-mouse`, `shell-integration`, `vive-tool`) for completion-check examples / 更新 README 与插件开发文档，使 new-plugin 后续步骤、官方工作集冒烟校验与发布准备现在都明确要求在插件与 store 元数据之外同步维护仓库根 `CHANGELOG.md`，并在 completion-check 示例中统一使用真实清单插件 ID（`custom-mouse`、`shell-integration`、`vive-tool`）
- **Shell Binary Fetch Validation / Shell 二进制拉取校验**: Verified the current `fetch-shell-binaries.ps1` path still fails against the latest upstream release layout because the fallback source zipball no longer contains a packaged `shell.exe`, documenting the remaining Shell Integration blocker in the working-set release evidence / 已验证当前 `fetch-shell-binaries.ps1` 在最新上游发布布局下仍会失败：回退到 source zipball 后已不再包含可打包的 `shell.exe`，并将该 Shell Integration 剩余阻塞明确记录进当前工作集发布证据

### Changed / 变更
- **Version / 版本**: Bump shared repository/store version metadata to `1.0.31` for this release-workflow/completion-check consistency pass / 为本次 release workflow / completion-check 一致性修正将共享仓库与商店版本元数据提升到 `1.0.31`

## [1.0.28] - 2026-03-29

### Fixed / 修复
- **Build Output Hygiene / 构建产物卫生**: Ignore repository-local `Build/` and all project `obj/` directories so repeated plugin builds no longer leave untracked build artifacts in `git status` / 忽略仓库内 `Build/` 与各项目 `obj/` 目录，避免重复插件构建后在 `git status` 中残留未跟踪构建产物
- **Build Dependencies / 构建依赖**: Resolve host dependency lookup by falling back to the main LenovoLegionToolkit build outputs when `Dependencies\Host` is empty, preventing plugin smoke builds from failing in fresh checkouts / 当 `Dependencies\Host` 为空时回退到主仓库编译输出作为依赖，避免插件冒烟构建在新环境下直接失败
- **SDK Reference / SDK 引用**: Use the shared host dependency path for the SDK reference to avoid missing `LenovoLegionToolkit.Lib` during plugin builds / SDK 引用改用统一的主仓库依赖路径，避免插件构建时找不到 `LenovoLegionToolkit.Lib`

### Changed / 变更
- **Version / 版本**: Bump shared plugin version metadata to `1.0.8` after the repository build hygiene fix / 在修复仓库构建产物卫生后，将共享插件版本号提升到 `1.0.8`
- **Documentation / 文档**: Expanded plugin smoke build instructions with ShellIntegration guidance and host dependency notes / 补充插件冒烟构建说明（ShellIntegration 与依赖回退说明）
- **Evidence / 证据**: Verified by `C:\Users\96152\.openclaw\workspace\attachments\lenovolegiontoolkit-plugins\network-accel-build-clean.log` and `C:\Users\96152\.openclaw\workspace\attachments\lenovolegiontoolkit-plugins\network-accel-status-clean.txt` / 证据见上述 build 与 git status 日志
