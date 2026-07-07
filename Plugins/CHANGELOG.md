# Changelog / 更新日志

All notable changes to this project will be documented in this file.

此项目的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

---

## [Unreleased] — v1.3.0-quality (Day 1-5 sprint)

### ✅ Added / 新增
- **5th plugin**: Battery Health (v1.0.0) — Monitor battery health, cycle count, capacity degradation
- **Performance optimization / 性能优化**: 
  - `SaveWithDebounce()` — Batch rapid saves (97% I/O reduction)
  - `SaveAsync()` — Non-blocking async file I/O
  - MessagePack serialization support (opt-in via constructor)
- **Performance benchmark automation / 性能基准测试自动化**: `Scripts/run-performance-tests.sh`
- **WMI integration / WMI 集成**: BatteryHealthService now queries real battery data (Win32_Battery)

### ✅ Changed / 变更
- **SettingsManager performance / SettingsManager 性能**:
  - Save() latency: **62ms → 0-1ms** (98% improvement)
  - Load() latency: **2ms → 0ms** (100% improvement)
  - Memory transaction: skip save if settings unchanged
- **SDK version / SDK 版本**: Updated to match host app v4.2.1

### ✅ Fixed / 修复
- **Code quality / 代码质量**: 0 warnings, 0 errors across all 6 projects
- **IDE0011**: Added braces to 80+ if statements
- **IDE1006**: Fixed naming convention (private fields `_camelCase`)
- **CA1062**: Added null validation to all public methods
- **NetworkAcceleration**: Fixed version mismatch (plugin.json "1.1.9" → "1.2.0")

### ✅ Removed / 移除
- **FeatureStatusConverter extraction** (reverted — caused compilation errors)

---

## [v1.2.0-quality] — 2026-07-05

### ✅ Added / 新增
- **Zero warnings achievement / 零警告成就** (all 6 projects)
- **562+ unit tests / 562+ 单元测试** passing
- **CI validation / CI 验证** fixed

### ✅ Changed / 变更
- **TreatWarningsAsErrors=true** enforced globally
- **XML documentation / XML 文档** added to all public APIs

---

## [v1.1.16] — 2026-07-03

### ✅ Added / 新增
- **Social preview banner / 社交预览横幅**: Added `Assets/social-preview.svg`
- **Star history chart / Star 历史图表**: Added to README
- **Enhanced badges / 增强徽章**: Watchers, Forks, Discussions

### ✅ Fixed / 修复
- **CA1062 Warnings**: Added `ArgumentNullException.ThrowIfNull` to all public methods
- **CA2024 Warnings**: Fixed `ProcessRunner.PumpAsync`
- **Version mismatch**: Fixed NetworkAcceleration plugin.json

---

**Last Updated / 最后更新**: 2026-07-07 23:59 (Day 5 complete)  
**Next Release / 下次发布**: v1.3.0-quality (target: 2026-07-12)  
**Goals / 目标**: 100+ GitHub stars, 5 plugins, 0 warnings, performance optimized
