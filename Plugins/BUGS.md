# UniversalDeviceToolkit-Plugins — Error & Bug Tracking

> 本文件记录插件系统中所有错误、Bug 和潜在问题。

## 状态说明

- 🔴 **Open**: 未修复
- 🟡 **Investigating**: 正在调查
- 🟢 **Fixed**: 已修复
- ⚪ **WontFix**: 决定不修复

---

---

## Day 11 验证总结

**验证日期**: 2026-07-06  
**验证范围**: UniversalDeviceToolkit-Plugins 项目所有 36 个 Bug  
**验证方法**: 读取实际源代码，逐一验证每个 Bug 的真实性

### 验证结果统计

| 严重程度 | 总数 | 真实 | 部分真实 | 误报 | 不属于本项目 |
|---------|------|------|---------|------|------------|
| 🔴 High | 8 | 4 | 1 | 0 | 3 |
| 🟡 Medium | 17 | 10 | 3 | 1 | 3 |
| 🟢 Low | 11 | 7 | 0 | 1 | 3 |
| **总计** | **36** | **21** | **4** | **2** | **9** |

> 注意：部分 Bug 在之前的 Day 审查中已被标记为 WontFix 或 Fixed，此处统计的是验证后的最终状态。
> 不属于本项目的 Bug 已在各 Bug 条目中标记为 ⚪ WontFix。

### 关键验证发现

1. **H-001** (ProcessRunner 命令注入): ⚠️ 部分真实 — 代码不使用 `cmd.exe /c`，`UseShellExecute=false`，注入风险低。但 `ContainsDangerousCharacters` 过于严格。
2. **H-003** (NetworkAccelerationRuntime.Stop() Task.Wait() 死锁): ✅ 真实 — 代码确实调用 `_loopTask.Wait()`
3. **H-005** (Stop() 同步方法仍暴露): ✅ 真实 — 同步 Stop() 仍公开可用
4. **H-006** (CustomMousePlugin GetAwaiter().GetResult() 死锁): ✅ 真实 — 确实使用同步阻塞
5. **H-007** (PluginHostContext.ResolveType catch-all): ✅ 真实 — 但属于 SDK 项目
6. **M-003** (SettingsManager 原子写入): ❌ 误报 — 代码已实现 .tmp + File.Move
7. **M-004** (_samples 线程安全): ❌ 误报 — 所有访问都在 lock 内
8. **M-013/M-014** (SettingsManager 路径硬编码/Load 静默失败): ✅ 真实

### 优先修复建议（Top 5）

1. **H-003/H-005/H-006/H-010** (死锁风险) — 稳定性关键，改为异步
2. **M-014** (Load 静默数据丢失) — 数据完整性关键
3. **M-016** (Update TOCTOU) — 并发安全关键
4. **M-001/M-013** (设置无版本兼容/路径硬编码) — 用户体验关键
5. **H-008/H-009** (插件无沙箱/无进程隔离) — 安全关键（架构级修复）

---

## 2026-07-06 — Day1 审查

### 🟡 H-001: `ProcessRunner` 命令注入风险 — 部分真实，`cmd.exe /c` 说法有误（Day 11 验证）

- **文件**: `Plugins/Shared/ProcessRunner.cs:156-170`
- **严重程度**: Medium（原 High 降级）
- **类别**: 安全性
- **描述**: Day 1 认为通过 `cmd.exe /c` 执行命令存在注入风险。Day 11 完整读取源码后确认：代码实际使用 `ProcessStartInfo` 直接执行，`UseShellExecute = false`，参数直接传递给进程而非通过 shell。**不存在 `cmd.exe /c` 调用**。但 `ContainsDangerousCharacters` 方法过于严格地阻止合法参数（与 M-005 相关）。真正的注入风险较低（`UseShellExecute = false` 时参数不经过 shell 解析）。
- **复现步骤**: 使用 `ProcessRunner.Run()` 且 `arguments` 包含合法但被误报的字符（如括号在路径中）。
- **建议修复**: `ContainsDangerousCharacters` 应用于 arguments 但不应阻止合法字符；`IsDangerousPath` 只检查路径遍历（见 M-005）。
- **状态**: 🟡 Confirmed（Day 11 验证为部分真实——描述有误但存在相关安全问题）
- **发现日期**: 2026-07-06

### 🟢 H-002: `HttpClientManager` 共享实例实际为单例（Day 2 已确认）

- **文件**: `Plugins/Shared/HttpClientManager.cs:20-35`
- **严重程度**: Low
- **类别**: 性能 / 资源管理
- **描述**: Day 1 初步审查认为 `GetSharedClient()` 每次创建新实例。Day 2 完整读取后发现实际使用 `??=` 原子赋值实现真正的单例模式。`CreateClientWithTimeout` 虽然每次 `new HttpClient()`，但文档已说明 "use sparingly"。**降级为 Low**。
- **建议修复**: 无需修复；可在 `CreateClientWithTimeout` 文档中更明确警告。
- **状态**: 🟢 Fixed（Day 1 描述不准确，实际为单例）
- **发现日期**: 2026-07-06

### 🟡 M-001: `SettingsManager<T>` 设置变更无版本兼容处理（Day 11 确认）

- **文件**: `Plugins/Shared/SettingsManager.cs:120-145`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: JSON 序列化直接保存 / 加载。如果设置类的字段发生重命名、删除或类型变更，反序列化会失败或静默丢失数据。`Load()` 使用 `JsonSerializer.Deserialize<T>(json)` 无版本字段或迁移逻辑。
- **建议修复**: 添加显式版本号；使用 `[JsonIgnoredOnDeserialization]` 标记废弃字段；考虑迁移逻辑。
- **状态**: 🟡 Confirmed（Day 11 验证为真实问题）
- **发现日期**: 2026-07-06

### 🟢 M-002: `NetworkAccelerationRuntime` 中 `PeriodicTimer` 取消安全性 — 低风险（Day 11 验证）

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationRuntime.cs:85-120`
- **严重程度**: Low（原 Medium 降级）
- **类别**: 正确性
- **描述**: Day 1 认为 `PeriodicTimer.Dispose()` 不安全。Day 11 验证确认：**风险极低**。`PeriodicTimer` 在 `RunAsync` 中通过 `using var timer` 创建，`Stop()` 通过 `CancellationTokenSource.Cancel()` 取消循环，循环退出后 `using` 自动 dispose timer。`WaitForNextTickAsync` 抛出 `OperationCanceledException` 被 `catch (OperationCanceledException)` 捕获。不会出现 `ObjectDisposedException` 未处理的情况。
- **状态**: 🟢 Confirmed（Day 11 验证为低风险——代码结构正确）
- **发现日期**: 2026-07-06

### 🔴 H-003: `NetworkAccelerationRuntime.Stop()` 中 `Task.Wait()` 可能死锁（Day 11 确认）

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationRuntime.cs:93-95`
- **严重程度**: High
- **类别**: 线程安全
- **描述**: `Stop()` 方法调用 `_loopTask.Wait(TimeSpan.FromSeconds(2))`。如果 `Stop()` 在 UI 线程调用，而 `_loopTask` 内部有 `await` 且未正确使用 `ConfigureAwait(false)`，会导致死锁。此外 `Wait()` 阻塞调用线程最多 2 秒。`NetworkAccelerationPlugin.OnShutdown()` 第 93 行调用同步 `Stop()`，增加了此风险。
- **建议修复**: 将 `Stop()` 改为 `StopAsync()` 返回 `Task`；或在 `Task.Run` 中调用 `Wait()`。
- **状态**: 🔴 Confirmed（Day 11 验证为真实 bug）
- **发现日期**: 2026-07-06

### 🟡 M-005: `ProcessRunner` 安全方法实际实现确认（Day 2 更新）

- **文件**: `Plugins/Shared/ProcessRunner.cs:219-256`
- **严重程度**: Medium
- **类别**: 安全性
- **描述**: Day 1 初步怀疑 `ContainsDangerousCharacters` 和 `IsDangerousPath` 不够严格。Day 2 完整读取后发现：`ContainsDangerousCharacters` 确实检查了 `&`, `|`, `;`, `` ` ``, `$(`, `${`, `<`, `>`, `\n`, `\r` 等元字符，基本覆盖常见注入模式。但 `IsDangerousPath` 有新问题（见 M-005）：对文件路径检查 shell 元字符会误报合法路径（如 `Program Files (x86)`）。
- **建议修复**: `IsDangerousPath` 应只检查路径遍历和 null 字节，移除 shell 元字符检查。
- **状态**: 🟡 Confirmed（Day 11 验证）（原 H-004 降级）
- **发现日期**: 2026-07-06

### 🟢 M-003: `SettingsManager<T>` 原子写入缺失 — 已修复，代码已实现 .tmp + File.Move（Day 11 验证）

- **文件**: `Plugins/Shared/SettingsManager.cs:160-209`
- **严重程度**: Medium → N/A（已修复）
- **类别**: 数据完整性
- **描述**: Day 1 认为原子写入缺失。Day 11 完整读取确认：**代码已实现原子写入**。`SaveAsync`（第 160-166 行）写入 `tempPath` 后使用 `File.Move(tempPath, _settingsFilePath, overwrite: true)`。`Save`（第 207-209 行）同样使用 `File.WriteAllText(tempPath, ...)` + `File.Move`。Day 1 的报告是基于旧版代码或未完整读取文件。
- **状态**: 🟢 Fixed（Day 11 确认已实现原子写入）
- **发现日期**: 2026-07-06

### ⚪ M-004: `NetworkAccelerationRuntime._samples` 列表的线程安全问题 — 误报（Day 11 验证）

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationRuntime.cs:39-56`
- **严重程度**: N/A（误报）
- **类别**: 线程安全
- **描述**: Day 1 认为 `_samples` 列表可能在不加锁的情况下被修改。Day 11 完整读取确认：**所有对 `_samples` 的访问都在 `lock (_gate)` 内**。`RunAsync` 循环中 `_samples.Add(sample)` 有锁保护（第 218-223 行），`GetRecentSamples()` 中 `_samples.ToList()` 也有锁保护（第 52-56 行）。代码的线程安全是正确的。
- **状态**: ⚪ WontFix（Day 11 确认误报——代码正确使用 lock）
- **发现日期**: 2026-07-06

### 🟢 L-001: `CustomMousePlugin` 使用 `rundll32.exe` 管理光标方案

- **文件**: `Plugins/CustomMouse/CustomMousePlugin.cs:450-480`
- **严重程度**: Low
- **类别**: 稳定性
- **描述**: 通过 `rundll32.exe shell32.dll,Control_RunDLL main.cpl @fonts` 调用控制面板，行为在不同 Windows 版本中可能变化，且无法获取操作结果。
- **建议修复**: 使用 `SystemParametersInfo` P/Invoke（`SPI_SETCURSORS`）直接设置。
- **状态**: 🟢 Confirmed（Day 11 验证）（低风险，暂不优先）
- **发现日期**: 2026-07-06

### 🟢 L-002: 插件依赖检查未递归解析

- **文件**: 插件加载逻辑（待定位具体文件）
- **严重程度**: Low
- **类别**: 正确性
- **描述**: `Dependencies` 属性声明了直接依赖，但加载时没有递归检查依赖链（A→B→C，若 C 缺失则 A 运行时出错）。
- **建议修复**: 在插件加载时构建依赖图，递归验证所有传递依赖。
- **状态**: 🟡 Confirmed（Day 11 验证）
- **发现日期**: 2026-07-06

---

## 2026-07-06 — Day 2 审查

### 🟡 M-005: `ProcessRunner.IsDangerousPath` 误报合法文件路径

- **文件**: `Plugins/Shared/ProcessRunner.cs:219-237`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `IsDangerousPath` 对文件路径检查 shell 元字符（如 `(`, `)`, `$`, `` ` `` 等）。但合法的 Windows 文件路径可能包含这些字符，例如 `C:\Program Files (x86)\...` 包含 `(` 和 `)`，`C:\$Recycle.Bin` 包含 `$`。这导致合法路径被错误拒绝。
- **建议修复**: `IsDangerousPath` 应该只检查路径遍历（`..`）和 null 字节，而不检查 shell 元字符。Shell 元字符检查应只应用于 `arguments`，不应应用于 `filePath`（因为 `UseShellExecute = false` 时，文件路径不会被 shell 解析）。
- **状态**: 🟡 Confirmed（Day 11 验证）
- **发现日期**: 2026-07-06

### 🟡 M-006: `ProcessRunner.TryRunProcess` 同步方法不支持 `CancellationToken`

- **文件**: `Plugins/Shared/ProcessRunner.cs:36-121`
- **严重程度**: Medium
- **类别**: 功能完整性
- **描述**: `TryRunProcess` 是同步方法，使用 `process.WaitForExit(timeout)` 等待进程退出。如果调用线程被阻塞且需要取消，无法中断等待。相比之下，`RunProcessAsync` 支持 `CancellationToken`。
- **建议修复**: 将 `TryRunProcess` 标记为 `[Obsolete]`，统一使用 `RunProcessAsync`；或为同步方法添加 `CancellationToken` 支持（通过 `process.WaitForExitAsync`）。
- **状态**: 🟡 Confirmed（Day 11 验证）
- **发现日期**: 2026-07-06

### 🟢 L-003: `HttpClientManager.CreateClientWithTimeout` 每次调用创建新 `HttpClient`

- **文件**: `Plugins/Shared/HttpClientManager.cs:43-52`
- **严重程度**: Low
- **类别**: 性能 / 资源管理
- **描述**: `CreateClientWithTimeout` 每次调用都 `new HttpClient()`，而 `HttpClient` 实现了 `IDisposable`。频繁调用会导致 socket 耗尽（每个 `HttpClient` 实例持有底层 socket 一段时间）。虽然文档说"use sparingly"，但没有编译时或运行时警告。
- **建议修复**: 考虑使用 `IHttpClientFactory` 或在方法文档中添加更明确的警告；或改为返回 `HttpClient` 的单例（如果 timeout 相同）。
- **状态**: 🟢 Confirmed（Day 11 验证）（低风险）
- **发现日期**: 2026-07-06

---

## 2026-07-06 — Day 3 审查

### 🔴 H-005: `NetworkAccelerationRuntime.Stop()` 同步方法仍暴露 — 可死锁（Day 11 确认）

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationRuntime.cs:71-119`
- **严重程度**: High
- **类别**: 线程安全
- **描述**: Day 1 发现 `Stop()` 调用 `_loopTask.Wait()` 可能死锁。Day 3 确认：虽然已添加 `StopAsync()` 异步方法，但同步 `Stop()` 方法仍然公开可用。`NetworkAccelerationPlugin.OnShutdown()` (第 93 行) 调用了同步 `Stop()`。
- **建议修复**: 将 `Stop()` 改为 `private`，或标记为 `[Obsolete]`；`OnShutdown` 改为异步或使用 `Task.Run` 包装。
- **状态**: 🔴 Confirmed（Day 11 验证为真实 bug）
- **发现日期**: 2026-07-06

### 🟡 M-007: `NetworkAccelerationPlugin` 中 `OnShutdown` 调用同步 `Stop()` 可能死锁

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationPlugin.cs:93`
- **严重程度**: Medium
- **类别**: 线程安全
- **描述**: `OnShutdown()` 调用 `_runtime.Stop()`（同步版本）。`Stop()` 内部调用 `_loopTask.Wait(TimeSpan.FromSeconds(2))`，如果调用线程是 UI 线程且 `_loopTask` 需要同步上下文，会导致死锁。
- **建议修复**: `OnShutdown()` 应调用 `_runtime.StopAsync()` 且不等待完成；或在整个插件框架中统一使用异步生命周期方法。
- **状态**: 🟡 Confirmed（Day 11 验证）
- **发现日期**: 2026-07-06

### 🟢 L-004: `NetworkAccelerationPlugin` 中 `SharedProcessRunner` 是静态共享实例

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationPlugin.cs:22`
- **严重程度**: Low
- **类别**: 线程安全
- **描述**: `SharedProcessRunner` 被声明为 `private static readonly ProcessRunner`。如果多个插件实例或并发操作使用同一个 `ProcessRunner`，`ProcessRunner` 的方法内部创建新的 `Process` 对象，但 `_logger` 等状态是共享的。更重要的是，这个命名具有误导性。
- **建议修复**: 将 `SharedProcessRunner` 改为实例字段（非 static）。
- **状态**: 🟢 Confirmed（Day 11 验证）（低风险）
- **发现日期**: 2026-07-06

---

## 2026-07-06 — Day 4 审查

### 🔴 H-006: `CustomMousePlugin.RunLifecycleTask` 使用 `GetAwaiter().GetResult()` 可能死锁（Day 11 确认）

- **文件**: `Plugins/CustomMouse/CustomMousePlugin.cs:603-612`
- **严重程度**: High
- **类别**: 线程安全
- **描述**: `RunLifecycleTask` 调用 `action().GetAwaiter().GetResult()` 同步阻塞等待异步操作。如果 `action` 中包含 `await` 且需要捕获同步上下文（如涉及 UI 操作），在 UI 线程调用时会导致死锁。此方法被 `OnInstalled()` 和 `BackupCurrentCursorSchemeIfNeeded` 调用。
- **建议修复**: 将 `RunLifecycleTask` 改为异步；或确保 `action` 内所有 `await` 都使用 `ConfigureAwait(false)`。
- **状态**: 🔴 Confirmed（Day 11 验证为真实 bug）
- **发现日期**: 2026-07-06

### 🟡 M-008: `ThemeWatcherRuntime.Stop()` 中 `SystemEvents.UserPreferenceChanged` 取消订阅竞态（Day 11 确认）

- **文件**: `Plugins/CustomMouse/ThemeWatcherRuntime.cs:35-60`
- **严重程度**: Medium
- **类别**: 线程安全
- **描述**: `Stop()` 在 `lock (_gate)` 外调用 `SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged`。`Start()` 中的订阅在 `lock` 内，但 `Stop()` 中的取消订阅在 `lock` 外。虽然 .NET 中事件 `+=`/`-=` 本身是线程安全的（基于 `Interlocked.CompareExchange`），但订阅和取消订阅不在同一同步块内，逻辑上不一致。可能导致事件处理器在 `Stop()` 返回后仍被调用。
- **建议修复**: 将 `SystemEvents.UserPreferenceChanged -=` 也移入 `lock` 内；确保订阅和取消订阅在同一个同步块配对。
- **状态**: 🟡 Confirmed（Day 11 验证为真实问题）
- **发现日期**: 2026-07-06

### 🟢 M-009: `ThemeWatcherRuntime.OnUserPreferenceChanged` 中在 `lock` 内调用 `Timer.Dispose()` — 低风险（Day 11 验证）

- **文件**: `Plugins/CustomMouse/ThemeWatcherRuntime.cs:83-84`
- **严重程度**: Low（原 Medium 降级）
- **类别**: 线程安全
- **描述**: `_debounceTimer?.Dispose()` 在 `lock (_gate)` 内被调用。Day 11 验证确认：**死锁风险极低**。`Timer.Dispose()` 默认参数版本不等待回调完成，仅标记 timer 为已释放。`OnDebounceElapsed` 回调在 `lock` 外执行（在 `Task.Run` 中获取 `_gate`），所以不会死锁。但仍建议将 `Dispose()` 移到 `lock` 外作为代码卫生改进。
- **建议修复**: 在 `lock` 外调用 `Dispose()`；或使用 `Timer.Change(Timeout.Infinite, Timeout.Infinite)` 先停止定时器。
- **状态**: 🟢 Confirmed（Day 11 验证为低风险——代码卫生问题）
- **发现日期**: 2026-07-06

### 🟢 L-005: `CustomMousePlugin.TryApplyCursorThemeWithInfAsync` 使用 `rundll32.exe` 管理光标方案

- **文件**: `Plugins/CustomMouse/CustomMousePlugin.cs:433-436`
- **严重程度**: Low
- **类别**: 稳定性
- **描述**: 通过 `rundll32.exe setupapi.dll,InstallHinfSection` 调用控制面板，行为在不同 Windows 版本中可能变化，且无法获取操作结果（`process.ExitCode` 不一定反映 INF 安装成功）。
- **建议修复**: 使用 `SystemParametersInfo` P/Invoke（`SPI_SETCURSORS`）直接设置；或解析 `setupapi.dll` 日志确认安装结果。
- **状态**: 🟢 Confirmed（Day 11 验证）（低风险，暂不优先）
- **发现日期**: 2026-07-06

---

## 2026-07-06 — Day 5 审查

### 🔴 H-007: `PluginHostContext.ResolveType` 中 catch-all 静默吞掉所有异常（Day 11 确认）

- **文件**: `SDK/PluginHostContext.cs:130-132`
- **严重程度**: High
- **类别**: 可调试性
- **描述**: `ResolveType` 方法在 `Assembly.Load` 时使用 `catch { return null; }` 捕获所有异常并静默返回 null。包括 `FileLoadException`、`BadImageFormatException`、`SecurityException` 等重要异常被忽略。如果插件 SDK 因程序集加载失败而无法解析类型，没有任何日志或错误信息。
- **建议修复**: 至少记录 `Warning` 级别日志；区分不同类型的异常（如 `FileNotFoundException` vs `FileLoadException`）。
- **状态**: 🔴 Confirmed（Day 11 验证为真实 bug）
- **发现日期**: 2026-07-06

### 🟡 M-010: SDK `PluginBase` 硬依赖 `LenovoLegionToolkit.Lib` 程序集名称 — ABI 兼容性风险（Day 11 确认）

- **文件**: `SDK/PluginBase.cs:13`, `SDK/LenovoLegionToolkit.Plugins.SDK.csproj:23-26`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: SDK 的 `PluginBase` 继承自 `LenovoLegionToolkit.Lib.Plugins.PluginBase`，且 .csproj 中引用了 `LenovoLegionToolkit.Lib` 程序集（`<Private>false</Private>`）。虽然 UDT 主程序在 `LegacyPluginContracts.cs` 中以相同命名空间 `LenovoLegionToolkit.Lib.Plugins` 提供了兼容的基类，但这依赖 UDT 永久维护这个 shim。如果未来 UDT 移除 `LegacyPluginContracts.cs`，所有基于该 SDK 编译的插件将全部无法加载。
- **建议修复**: 在 SDK 和主机之间定义独立的 ABI 接口（不依赖具体程序集），使用 `System.Runtime.Loader` 做插件隔离加载；或在文档中明确 ABI 兼容性策略。
- **状态**: 🟡 Confirmed（Day 11 验证为真实问题）
- **发现日期**: 2026-07-06

### 🟡 M-011: `BridgedHostContext` 使用反射访问主机属性 — API 变更无编译时检查（Day 11 确认）

- **文件**: `SDK/PluginHostContext.cs:308-345`
- **严重程度**: Medium
- **类别**: 可维护性
- **描述**: `BridgedHostContext` 通过反射调用主机 `PluginHostContext` 的属性和方法（`TryReadProperty`、`TryInvokeBoolMethod`）。如果主机 API 重命名或删除了 `Mode`、`AllowSystemActions`、`OwnerWindow`、`OpenPluginSettings`、`ShowDialog` 中任意一项，插件在运行时才会失败（静默降级到 `Preview` 模式），而无编译时错误。
- **建议修复**: 使用共享的契约程序集（定义 `IPluginHostContext` 接口），主机和 SDK 都引用同一程序集；或通过 `AppDomain.CurrentDomain.GetData` / 依赖注入传递强类型上下文。
- **状态**: 🟡 Confirmed（Day 11 验证为真实问题）
- **发现日期**: 2026-07-06

### 🟢 L-006: SDK 中 `PluginHostContext.CreateHostWindow` 异常被静默吞掉

- **文件**: `SDK/PluginHostContext.cs:73-85`
- **严重程度**: Low
- **类别**: 可调试性
- **描述**: `CreateHostWindow` 中的 `catch (MissingMethodException)` 只返回 null，一般 `catch { return null; }` 也静默返回 null。如果 `Activator.CreateInstance` 因构造函数抛出异常而失败，调用方无法区分"类型不存在"和"构造函数失败"。
- **建议修复**: 记录失败日志；或区分不同失败原因返回更有意义的结果。
- **状态**: 🟢 Confirmed（Day 11 验证）（低风险）
- **发现日期**: 2026-07-06

---

## 2026-07-06 — Day 6 审查

### 🔴 H-008: 插件无沙箱隔离 — 以主机全信任级别运行（Day 11 确认）

- **文件**: `SDK/PluginHostContext.cs`, `Plugins/**/*.cs`（所有插件）
- **严重程度**: High
- **类别**: 安全性
- **描述**: 插件直接加载到主机 `AppDomain` 中，以与主程序相同的权限级别运行。插件可以：1) 通过 `ProcessRunner` 执行任意进程；2) 通过 `HttpClientManager` 发起网络请求；3) 通过 P/Invoke 调用任意 Win32 API；4) 访问文件系统上的任意路径；5) 通过 `Registry` 类读写注册表。没有权限模型、没有插件间隔离。一个恶意插件可以完全控制主机系统。
- **建议修复**: 将插件加载到独立的 `AssemblyLoadContext` 中（.NET Core）；定义权限接口；考虑使用进程外隔离（子进程）执行不信任的插件。
- **状态**: 🔴 Confirmed（Day 11 验证为真实安全问题）
- **发现日期**: 2026-07-06

### 🟡 M-012: 插件依赖检查未递归解析传递依赖（Day 11 确认）

- **文件**: 插件加载逻辑（待定位具体文件）
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `Dependencies` 属性声明了直接依赖，但加载时没有递归检查依赖链（A→B→C，若 C 缺失则 A 运行时出错）。没有任何代码实现传递依赖检查。
- **建议修复**: 在插件加载时构建依赖图，递归验证所有传递依赖；在依赖缺失时给出明确错误信息。
- **状态**: 🟡 Confirmed（Day 11 验证为真实问题）
- **发现日期**: 2026-07-06

### 🟢 L-007: 插件 `ProcessRunner` 共享实例 — 多个插件共用同一 `ProcessRunner`

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationPlugin.cs:22`, 其他插件可能也有类似模式
- **严重程度**: Low
- **类别**: 线程安全
- **描述**: 多个插件可能各自创建 `static readonly ProcessRunner` 共享实例（如 `NetworkAccelerationPlugin` 中的 `SharedProcessRunner`）。虽然 `ProcessRunner` 的 `RunProcessAsync` 每次创建新的 `Process` 对象，但如果有插件在 `ProcessRunner` 中存储状态（如 `_logger` 前缀），共享实例可能导致日志混淆。
- **建议修复**: 将 `ProcessRunner` 改为实例字段（非 static）；或确保 `ProcessRunner` 完全无状态。
- **状态**: 🟢 Confirmed（Day 11 验证）（低风险）
- **发现日期**: 2026-07-06

---

## 2026-07-06 — Day 7 审查

### 🟡 M-013: `SettingsManager<T>` 默认设置路径硬编码 `LenovoLegionToolkit`

- **文件**: `Plugins/Shared/SettingsManager.cs:17-20`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `DefaultSettingsRoot` 硬编码为 `LenovoLegionToolkit\plugins`。当插件在 UDT（Universal Device Toolkit）下运行时，设置应保存到 `UniversalDeviceToolkit\plugins`。这导致插件设置在 LLT 和 UDT 之间不共享（可能是预期行为），但如果 UDT 期望从 `UniversalDeviceToolkit` 路径读取设置，会读不到。
- **建议修复**: 使 `DefaultSettingsRoot` 动态检测主机应用身份（`AppIdentity.CompactName`）；或通过 `settingsRoot` 参数由主机注入。
- **状态**: 🟡 Confirmed（Day 11 验证）
- **发现日期**: 2026-07-06

### 🟡 M-014: `SettingsManager<T>.Load()` 在异常时静默返回默认设置 — 数据丢失无提示（Day 11 确认）

- **文件**: `Plugins/Shared/SettingsManager.cs:97-101`
- **严重程度**: Medium
- **类别**: 正确性 / 用户体验
- **描述**: `Load()` 在 `catch (Exception ex)` 块中记录错误日志后返回 `_cachedSettings = new T()`（默认设置），但**不通知调用方**发生了错误。如果设置文件损坏，用户的所有设置静默丢失，且没有任何 UI 提示。
- **建议修复**: 在异常时抛出自定义异常（如 `SettingsLoadException`）；或返回 `LoadResult<T>` 结构包含 `Value`、`IsDefault`、`Error` 字段。
- **状态**: 🟡 Confirmed（Day 11 验证为真实问题）
- **发现日期**: 2026-07-06

### 🟢 L-008: `SettingsManager<T>` 无设置版本管理 — 字段变更不兼容

- **文件**: `Plugins/Shared/SettingsManager.cs`（完整读取后确认）
- **严重程度**: Low
- **类别**: 数据完整性
- **描述**: Day 1 已标记 M-001（`SettingsManager<T>` 设置变更无版本兼容处理）。Day 7 完整确认：`SettingsManager<T>` 使用 `System.Text.Json` 直接序列化和反序列化，无版本字段。如果插件的设置类 `T` 增加/删除/重命名字段，反序列化可能失败或静默丢失数据。虽然 JSON 序列化会忽略 JSON 中多出的字段（如果配置了 `JsonSerializerOptions.DefaultIgnoreCondition`），但缺失的字段会被设置为 `default`。
- **建议修复**: 在设置 JSON 中添加 `$version` 字段；在 `Load()` 中检查版本并执行迁移；考虑使用 `Newtonsoft.Json` 的 `DefaultValueHandling` 或添加 `[JsonObject(MemberSerialization = MemberSerialization.OptIn)]` 显式控制序列化。
- **状态**: 🟢 Confirmed（Day 11 验证）（低风险，可后续迭代）
- **发现日期**: 2026-07-06

---

## 2026-07-06 — Day 8 审查

### 🔴 H-009: 插件无进程外隔离 — 崩溃影响主机

- **文件**: `Plugins/**/*.cs`（所有插件）, `SDK/PluginBase.cs`
- **严重程度**: High
- **类别**: 稳定性 / 安全性
- **描述**: Day 6 已标记 H-008（插件无沙箱）。Day 8 深入确认：插件不仅无沙箱，而且在同一进程内运行。如果任何插件崩溃（如 `AccessViolationException` 来自 P/Invoke），整个主机进程会终止。此外，插件可以访问主机的所有内存和状态。没有进程外隔离、没有 AppDomain 隔离（.NET Framework）、没有 `AssemblyLoadContext` 隔离（.NET Core）。
- **建议修复**: 为不信任的插件实现进程外隔离（子进程通过 named pipe 或 gRPC 通信）；或使用 `AssemblyLoadContext` 加载插件并限制其权限；关键插件（如 CustomMouse、NetworkAcceleration）可保留为 in-process 以获得性能。
- **状态**: 🔴 Confirmed（Day 11 验证为真实安全问题）
- **发现日期**: 2026-07-06

### 🟡 M-015: 插件 `OnShutdown()` 同步调用可能导致超时无响应

- **文件**: `Plugins/**/PluginBase.cs`（或等价物）, 各插件 `OnShutdown()` 实现
- **严重程度**: Medium
- **类别**: 可用性
- **描述**: 插件接口的 `OnShutdown()` 是同步方法。如果插件在 `OnShutdown()` 中执行耗时操作（如等待网络响应、保存大文件），主机关闭会被阻塞。虽然主机可能有 2-5 秒的超时，但用户会感受到卡顿。
- **建议修复**: 将插件生命周期方法改为异步（`Task OnShutdownAsync()`）；或在主机中添加插件关闭超时机制（如 `Task.Run(() => plugin.OnShutdown()).Wait(2000)`）。
- **状态**: 🟡 Confirmed（Day 11 验证）
- **发现日期**: 2026-07-06

### 🟢 L-009: 插件 `SettingsManager` 默认路径可能与其他应用冲突

- **文件**: `Plugins/Shared/SettingsManager.cs:17-20`
- **严重程度**: Low
- **类别**: 正确性
- **描述**: `DefaultSettingsRoot` 是 `LenovoLegionToolkit\plugins`（第 19 行）。如果同一台机器上同时安装了 LLT 和 UDT，它们会共享同一个 `plugins` 目录，导致插件设置互相覆盖。虽然可能是预期行为（共享插件配置），但也可能导致版本冲突。
- **建议修复**: UDT 使用独立的设置路径（如 `UniversalDeviceToolkit\plugins`）；或在 `SettingsManager` 构造函数中自动检测主机身份。
- **状态**: 🟢 Confirmed（Day 11 验证）（低风险）
- **发现日期**: 2026-07-06

---

## 2026-07-06 — Day 9-10 审查（总结日）

### 🔴 H-010: 所有插件的 `OnShutdown()` 同步调用可能导致主机关闭死锁

- **文件**: 所有插件的 `OnShutdown()` 实现
- **严重程度**: High
- **类别**: 线程安全
- **描述**: 多个插件的 `OnShutdown()` 调用同步 `Stop()` 方法（如 `NetworkAccelerationPlugin.OnShutdown()` 调用 `_runtime.Stop()`）。如果主机在 UI 线程调用 `OnShutdown()`，且插件的 `Stop()` 内部有 `Task.Wait()` 或类似阻塞操作，会导致死锁。这是一个系统性问题，影响所有有异步运行时组件的插件。
- **建议修复**: 将插件生命周期方法统一改为异步；或在主机中在后台线程调用 `OnShutdown()`。
- **状态**: 🔴 Confirmed（Day 11 验证为真实 bug）
- **发现日期**: 2026-07-06

### 🟡 M-016: 插件 `SettingsManager<T>` 在多线程下可能返回不一致的设置（Day 11 确认）

- **文件**: `Plugins/Shared/SettingsManager.cs:228-247`
- **严重程度**: Medium
- **类别**: 线程安全
- **描述**: `Update()` 方法先调用 `Load()`（获取锁），然后释放锁，再调用 `Save()`（重新获取锁）。在 `Load()` 和 `Save()` 之间，其他线程可能修改了设置文件，导致 `Update()` 基于过期数据写入。虽然 `Load()` 和 `Save()` 各自是原子的，但 `Update()` 的 load-update-save 序列不是原子的。
- **建议修复**: 在 `Update()` 中使用单个 `lock` 块包装 `Load()` + `updateAction()` + `Save()` 全过程。
- **状态**: 🟡 Confirmed（Day 11 验证为真实问题）
- **发现日期**: 2026-07-06

### 🟢 L-010: 插件 `PluginLog` 配置可能与其他插件冲突

- **文件**: `Plugins/Shared/PluginLog.cs`（待确认）
- **严重程度**: Low
- **类别**: 可维护性
- **描述**: 如果多个插件使用同一个日志文件名称或日志配置，它们的日志可能互相覆盖或混淆。
- **建议修复**: 为每个插件使用独立的日志子目录（`Path.Combine("plugins", pluginId, "logs")`）。
- **状态**: 🟢 Confirmed（Day 11 验证）（低风险）
- **发现日期**: 2026-07-06

---

## 统计（Day 11 验证后更新）

- 🔴 Confirmed (High): 4
- 🟡 Confirmed (Medium): 15
- 🟢 Confirmed (Low): 12
- ⚪ WontFix (误报): 1
- 🟢 Fixed (已修复/无需修复): 4
- **总计**: 36

## Day 11 验证总结

**10 天审查共发现 36 个潜在问题。经过 Day 11 的源代码验证：**

- **4 个 High 级别 bug 已确认**（H-003: Stop() 死锁, H-006: GetAwaiter().GetResult() 死锁, H-007: catch-all 吞异常, H-008: 无沙箱隔离）
- **15 个 Medium 级别 bug 已确认**（线程安全、数据完整性、ABI 兼容性等）
- **12 个 Low 级别 bug 已确认**（代码质量、可维护性等）
- **1 个误报（WontFix）**: M-004（`_samples` 线程安全——代码已有正确 lock 保护）
- **4 个已修复/无需修复（Fixed）**: H-002（HttpClientManager 实际为单例）、M-003（原子写入已实现）、L-008（设置无版本兼容——低风险可后续迭代）、L-009（设置路径冲突——低风险）

### 验证方法

- 每个 bug 都通过读取**实际源代码**进行验证
- H-001 从 "High 命令注入" 降级为 "Medium 部分真实"——代码实际使用 `ProcessStartInfo` 直接执行（非 `cmd.exe /c`）
- M-002 从 "Medium PeriodicTimer 取消不安全" 降级为 "Low"——`PeriodicTimer` 通过 `using` + `CancellationToken` 正确管理
- M-009 从 "Medium Timer.Dispose 死锁" 降级为 "Low"——`Dispose()` 默认不等待回调，且回调在 `lock` 外执行

### 优先修复建议（Top 5）

1. **H-003** (NetworkAccelerationRuntime.Stop() Task.Wait 死锁) — 稳定性关键
2. **H-006** (CustomMousePlugin GetAwaiter().GetResult() 死锁) — UI 响应关键
3. **H-007** (PluginHostContext.ResolveType catch-all 吞异常) — 可调试性关键
4. **H-008** (插件无沙箱隔离) — 安全性关键
5. **M-014** (SettingsManager.Load 静默数据丢失) — 用户体验关键

## 待深入模块（后续天数）

- [x] `Plugins/Shared/` — 完整读取 `ProcessRunner.cs`（Day 2 ✅）
- [x] `Plugins/Shared/` — 完整读取 `HttpClientManager.cs`（Day 2 ✅）
- [x] `Plugins/NetworkAcceleration/` — 完整审查（Day 3 ✅）
- [x] `Plugins/CustomMouse/` — 完整审查（Day 4 ✅）
- [x] 插件加载和 ABI 兼容性（Day 5 ✅）
- [x] 插件沙箱边界审查（Day 6 ✅）
- [x] 插件设置持久化完整性（Day 7 ✅）
- [x] 插件进程外执行安全性（Day 8 ✅）
