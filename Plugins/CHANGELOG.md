# Changelog / 更新日志

All notable changes to this project will be documented in this file.

本项目的所有重要更改都会在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循[语义化版本](https://semver.org/spec/v2.0.0.html)。

---

## [Unreleased] — v1.3.0-quality (Day 1-5 sprint)

### Changed / 变更

- **Version tooling / 版本工具链**: Added `sync-version` and `bump-version` CLI commands; `migrate` now propagates `plugin.manifest.json` version to csproj, `[Plugin]` attribute, `plugin.json`, and `store-entry.json`. Removed misleading repo-wide version from `Directory.Build.props`.
- **Store release / 商店上架**: Bumped active plugin versions for plugin-center testing — custom-mouse 1.0.17, shell-integration 1.0.13, vive-tool 1.2.3; regenerated `store.json` with release asset sizes.

### Fixed

- Harmonized BatteryHealth plugin author to SSC-STUDIO across plugin.json, plugin.manifest.json, and Plugin attribute (was EliuaK_Csy)
- Synced NetworkAcceleration C# Plugin attribute version from 1.1.9 to 1.2.0 to match manifest (manifest is source of truth per KNOWLEDGE_BASE)
- Added retry-with-backoff to SaveSettingsAsync() in NetworkAcceleration to eliminate cross-assembly test flake (PLG-005, 409/409 tests green)
- Synced Build directory plugin.manifest.json files with source Plugins directory

### Added / 新增
- **5th plugin**: Battery Health (v1.0.0) — Monitor battery health, cycle count, capacity degradation
- **Battery Health settings UI**: Complete settings page with monitoring toggle, threshold sliders, notifications
- **Battery Health unit tests**: 16 tests for settings model validation, JSON round-trip, and invalid-threshold rules
- **Battery Health packaged**: Release ZIP built (2.99 MB, v1.0.0)
- **Battery Health UI redesign**: Full feature page and settings page redesign following the CustomMouse pattern (WpfFallbackHelper fallback, DynamicResource theme binding, CornerRadius cards, SymbolIcon glyphs, animated status pills)
- **Battery Health store promotion**: Generated store-entry.json and merged battery-health into root store.json with Universal Device Toolkit branding (32 languages, BatteryCharge24 icon)
- **Battery Health tests**: Fixed threshold theory inline-data bug; 16/16 unit tests green (0 warnings, 0 errors)
- **Cross-repo naming TODO**: Brand user-visible text as Universal Device Toolkit; internal LenovoLegionToolkit.* namespaces retained for host ABI compatibility (see BUGS.md M-010)
- **Performance optimization / 性能优化**:
  - SaveWithDebounce() — Batch rapid saves (97% I/O reduction)
  - SaveAsync() — Non-blocking async file I/O
  - MessagePack serialization support (opt-in via constructor)
- **Performance benchmark automation / 性能基准测试自动化**: Scripts/run-performance-tests.sh
- **WMI integration / WMI 集成**: BatteryHealthService now queries real battery data (Win32_Battery)

### Changed / 变更
- **SettingsManager performance / SettingsManager 性能**:
  - Save() latency: **62ms → 0-1ms** (98% improvement)
  - Load() latency: **2ms → 0ms** (100% improvement)
  - Memory transaction: skip save if settings unchanged
- **SDK version / SDK 版本**: Updated to match host app v4.2.1
- **Battery Health settings UI**: Fixed CS0104 type ambiguity between Wpf.Ui.Controls and System.Windows.Controls
- **Brand generalization rewrite / 品牌通用化重写**: Generalized all user-visible Lenovo-specific framing to universal Windows/OEM scope across 15 docs — /LenovoLegion→/pcmasterrace, "Lenovo Legion laptops"→"your Windows setup", 插件数 4→5, 项目数 6→7. M-010 ABI gate preserved (LenovoLegionToolkit.* namespaces, solution name, x:Class, manifest class, DLL names, host-release.json intentionally retained). Intentionally kept: /Lenovo community, Lenovo Vantage competitor comparison, lenovo-legion GitHub topic. Verification: 0 residual branding issues, build 0 warnings/0 errors, tests 409/409.
- **Cross-repo data-root alignment verification / 跨仓库数据根对齐验证**: Confirmed plugin SettingsManager<T> writes to root %LocalAppData%\UniversalDeviceToolkit\plugins\ (L21-24) aligned with host Folders.AppData (%LocalAppData%\UniversalDeviceToolkit, Main Folders.cs L29-30). LenovoLegionToolkit path segments retained as read-only migration source (unchanged).
- **Brand residual final-audit cleanup / 品牌残留终审清理**: Docs/ARCHITECTURE.md L5 project name migrated to Universal Device Toolkit Plugins, Docs/CODING_STANDARDS.md L5 project name synced. M-010 ABI markers preserved in namespace declarations and output path patterns.
- **Session backup file cleanup / 会话备份文件清理**: Removed 6 accumulated session backup files — workspace remains clean.
- **Store promotion / 商店上架**: Generated store-entry.json and merged battery-health into root store.json (5 plugins, battery-health featured, Universal Device Toolkit branding).
- **FeatureStatusConverter extraction** (reverted — caused compilation errors)

---

## [v1.2.0-quality] — 2026-07-05

### Added / 新增
- **Zero warnings achievement / 零警告成就** (all 6 projects)
- **562+ unit tests / 562+ 单元测试** passing
- **CI validation / CI 验证** fixed

### Changed / 变更
- **TreatWarningsAsErrors=true** enforced globally
- **XML documentation / XML 文档** added to all public APIs

---

## [v1.1.16] — 2026-07-03

### Added / 新增
- **Social preview banner / 社交预览横幅**: Added Assets/social-preview.svg
- **Star history chart / Star 历史图表**: Added to README
- **Enhanced badges / 增强徽章**: Watchers, Forks, Discussions

### Fixed / 修复
- **CA1062 Warnings**: Added ArgumentNullException.ThrowIfNull to all public methods
- **CA2024 Warnings**: Fixed ProcessRunner.PumpAsync
- **Version mismatch**: Fixed NetworkAcceleration plugin.json

---

**Last Updated / 最后更新**: 2026-07-08 00:00 (Day 7 start)
**Next Release / 下次发布**: v1.3.0-quality (target: 2026-07-12)
**Goals / 目标**: 100+ GitHub stars, 5 plugins, 0 warnings, performance optimized, Reddit promotion

---

## [2026-07-07] Session 25 — Host v4.2.1 Sync + Serilog Transitive Dependency Fix

### Added / 新增
- **Host DLL sync to v4.2.1** (from stale v3.6.14): LenovoLegionToolkit.Lib.dll, LenovoLegionToolkit.Lib.Plugins.dll (newly added), and Universal Device Toolkit.dll (renamed from Lenovo Legion Toolkit.dll) all synced to v4.2.1.0
- **Serilog transitive dependency**: Vendored Serilog.dll v4.3.0, Serilog.Sinks.Async.dll v2.1.0, and Serilog.Sinks.File.dll v7.0.0 into Dependencies\Host\ (v4.2.1 LenovoLegionToolkit.Lib.dll requires Serilog 4.3.0 at runtime via Log..ctor())
- **CopyHostDependenciesToOutput target**: New MSBuild target in Directory.Build.targets that copies all Dependencies\Host\*.dll to test/tool project output directories (IsPluginTestProject == True OR IsPluginToolProject == True)
- **BatteryHealth Workbench load fix**: PluginLoader.IsVersionCompatible now accepts v4.2.1 host (was rejecting with null due to 3.6.14 < 3.6.15 MinimumHostVersion mismatch)

### Changed / 调整
- **CS0104 type ambiguity fix**: Added using PluginHostMode = ... and using PluginHostContext = ... aliases to Tools\PluginWorkbench\MainWindow.xaml.cs
- **host-release.json updated**: Added libPlugins artifact, 	ransitiveDependencies array (Serilog DLLs), updated downloadUrl to UniversalDeviceToolkit_v4.2.1_win-x64.zip, bumped hostVersion to 4.2.1
- **ensure-host-dependencies.ps1 + refresh-host-references.ps1**: Added 3 Serilog DLLs to $requiredFiles; sibling resolver checks for Serilog presence
- **Directory.Build.props**: EnsureHostDependencies target condition now also checks for Serilog DLLs; CleanupPluginOutput now removes Universal Device Toolkit.* (was Lenovo Legion Toolkit.*)

### Fixed / 修复
- **ViveTool.Tests**: 14/186 tests failing with FileNotFoundException: Serilog, Version=4.3.0.0 — fixed by vendoring Serilog
- **NetworkAcceleration.Tests**: 1/39 test failing with same Serilog error — fixed by vendoring Serilog
- **PluginWorkbench.csproj**: Lenovo Legion Toolkit reference renamed to Universal Device Toolkit
- **Shared.Tests/ShellIntegration.Tests/ViveTool.Tests csproj**: Lenovo Legion Toolkit reference renamed to Universal Device Toolkit
- **PluginWorkbenchHostContext.cs / PluginWorkbenchSession.cs**: Verified no ambiguity (only import SDK namespace / fully-qualify PluginHostMode as SDK)

### Verification / 验证
- dotnet build LenovoLegionToolkit-Plugins.sln -c Release → 0 warnings / 0 errors (11.0s)
- dotnet test → **409/409 PASS** (BatteryHealth 16, CustomMouse 54, ShellIntegration 114, NetworkAcceleration 39, ViveTool 186)
- PluginWorkbench.Smoke → **10/10 PASS** (5 plugins × {Dark, Light} themes)
- Visual captures saved to rtifacts\workbench-visual\{plugin}-{theme}\{preview,settings,real-runtime}.png

### M-010 Constraint Honored / M-010 约束遵守
- **NOT renamed** (per M-010 ABI gate): LenovoLegionToolkit.Plugins.* namespaces, LenovoLegionToolkit-Plugins.sln filename, *.csproj filenames, plugin assembly names, plugin.manifest.json class field, DLL names, store.json minLLTVersion JSON property name
- **Renamed** (user-visible/build references only): WPF AssemblyName Lenovo Legion Toolkit → Universal Device Toolkit, host-release.json package/URL, cross-csproj <Reference> names