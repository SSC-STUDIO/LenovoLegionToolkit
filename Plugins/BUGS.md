# UniversalDeviceToolkit-Plugins — Error & Bug Tracking

> 本文件记录插件系统中所有错误、Bug 和潜在问题。

## 状态说明

- 🔴 **Open**: 未修复
- 🟡 **Investigating**: 正在调查
- 🟢 **Fixed**: 已修复
- ⚪ **WontFix**: 决定不修复

---

## 2026-07-06 — Day1 审查

### 🔴 H-001: `ProcessRunner` 命令注入风险

- **文件**: `Plugins/Shared/ProcessRunner.cs:156-170`
- **严重程度**: High
- **类别**: 安全性
- **描述**: 通过 `cmd.exe /c "{command}"` 执行命令，如果 `command` 包含用户输入且含有 `&`, `|`, `>`, `<` 等 shell 元字符，可以执行任意命令。当前代码未对 `command` 进行转义。
- **复现步骤**: 任何使用 `ProcessRunner.Run()` 且 `command` 参数来源于外部输入的场景。
- **建议修复**: 避免通过 `cmd.exe /c` 执行；如需执行，使用 `ProcessStartInfo.ArgumentList`（.NET 5+）或 `ArgumentEscaper.EscapeAndConcat`。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🔴 H-002: `HttpClientManager` 每次调用创建新 `HttpClient` 实例

- **文件**: `Plugins/Shared/HttpClientManager.cs:38-55`
- **严重程度**: High
- **类别**: 性能 / 资源管理
- **描述**: `GetClient()` 每次调用 `new HttpClient(_sharedHandler, disposeHandler: false)`。虽然共享了 `HttpMessageHandler`，但频繁创建 `HttpClient` 对象本身开销较大，且 `HttpClient` 内部的连接池状态不共享。
- **建议修复**: 使用静态单例 `HttpClient` 或引入 `IHttpClientFactory`。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🟡 M-001: `SettingsManager<T>` 设置变更无版本兼容处理

- **文件**: `Plugins/Shared/SettingsManager.cs:120-145`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: JSON 序列化直接保存 / 加载。如果设置类的字段发生重命名、删除或类型变更，反序列化会失败或静默丢失数据。
- **建议修复**: 添加显式版本号；使用 `[JsonIgnoredOnDeserialization]` 标记废弃字段；考虑迁移逻辑。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-002: `NetworkAccelerationRuntime` Dispose 中 `PeriodicTimer` 取消不安全

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationRuntime.cs:85-120`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `PeriodicTimer.Dispose()` 会导致正在等待的 `WaitForNextTickAsync` 抛出 `ObjectDisposedException`，当前代码未协调此行为。
- **建议修复**: 在 Dispose 前先 `CancellationTokenSource.Cancel()`，并 catch `ObjectDisposedException`。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🔴 H-003: `NetworkAccelerationRuntime.Stop()` 中 `Task.Wait()` 可能死锁

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationRuntime.cs:93-95`
- **严重程度**: High
- **类别**: 线程安全
- **描述**: `Stop()` 方法调用 `_loopTask.Wait(TimeSpan.FromSeconds(2))`。如果 `Stop()` 在 UI 线程调用，而 `_loopTask` 内部有 `await` 且未正确使用 `ConfigureAwait(false)`，会导致死锁。此外 `Wait()` 阻塞调用线程最多 2 秒。
- **建议修复**: 将 `Stop()` 改为 `StopAsync()` 返回 `Task`；或在 `Task.Run` 中调用 `Wait()`。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🔴 H-004: `ProcessRunner` 安全方法名与实际行为不一致

- **文件**: `Plugins/Shared/ProcessRunner.cs`（完整读取后确认）
- **严重程度**: High
- **类别**: 安全性
- **描述**: 经过完整读取，`ProcessRunner.TryRunProcess` 和 `RunProcessAsync` 确实有 `ContainsDangerousCharacters` 检查。但该方法的具体实现需要确认是否足够严格（如是否检查了 `|`, `&&`, `||`, `\n`, `\r` 等）。此外，`TryRunProcess` 的 `IsDangerousPath` 检查可能不够全面（如短路径 `C:\PROGRA~1` 可能绕过）。
- **建议修复**: 完整读取 `ContainsDangerousCharacters` 和 `IsDangerousPath` 的实现；考虑使用 `ProcessStartInfo.ArgumentList` 替代字符串拼接。
- **状态**: 🔴 Open（需确认完整实现）
- **发现日期**: 2026-07-06

### 🟡 M-003: `SettingsManager<T>` 原子写入缺失

- **文件**: `Plugins/Shared/SettingsManager.cs`（完整读取后确认）
- **严重程度**: Medium
- **类别**: 数据完整性
- **描述**: 如果 `SaveAsync` 在写入过程中进程崩溃，设置文件可能损坏（JSON 不完整）。虽然使用了 `File.WriteAllTextAsync`（在 NTFS 上通常是原子的），但建议显式使用 `.tmp` + `File.Move` 模式。
- **建议修复**: `SaveAsync` 中先写 `.tmp` 文件，然后 `File.Move(..., ..., overwrite: true)`。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-004: `NetworkAccelerationRuntime._samples` 列表的线程安全问题

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationRuntime.cs:39-56`
- **严重程度**: Medium
- **类别**: 线程安全
- **描述**: `GetRecentSamples()` 在 `lock (_gate)` 中返回 `_samples.ToList()` 副本，但 `_samples` 是 `List<>`。如果 `RunAsync` 循环在 `lock` 外修改 `_samples`（如 `Add` 触发 `Capacity` 扩容），可能导致读取时 `InvalidOperationException`。
- **建议修复**: 确保对 `_samples` 的所有修改都在 `lock (_gate)` 内；或使用 `ImmutableList<NetworkAccelerationSample>`。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟢 L-001: `CustomMousePlugin` 使用 `rundll32.exe` 管理光标方案

- **文件**: `Plugins/CustomMouse/CustomMousePlugin.cs:450-480`
- **严重程度**: Low
- **类别**: 稳定性
- **描述**: 通过 `rundll32.exe shell32.dll,Control_RunDLL main.cpl @fonts` 调用控制面板，行为在不同 Windows 版本中可能变化，且无法获取操作结果。
- **建议修复**: 使用 `SystemParametersInfo` P/Invoke（`SPI_SETCURSORS`）直接设置。
- **状态**: 🟢 Open（低风险，暂不优先）
- **发现日期**: 2026-07-06

### 🟢 L-002: 插件依赖检查未递归解析

- **文件**: 插件加载逻辑（待定位具体文件）
- **严重程度**: Low
- **类别**: 正确性
- **描述**: `Dependencies` 属性声明了直接依赖，但加载时没有递归检查依赖链（A→B→C，若 C 缺失则 A 运行时出错）。
- **建议修复**: 在插件加载时构建依赖图，递归验证所有传递依赖。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

---

## 统计

- 🔴 Open (High): 4
- 🟡 Investigating (Medium): 4
- 🟢 Open (Low): 2
- **总计**: 10

## 待深入模块（后续天数）

- [ ] `Plugins/Shared/` — 完整读取 `ProcessRunner.cs`（确认安全方法实现）（Day 2）
- [ ] `Plugins/Shared/` — 完整读取 `HttpClientManager.cs`（Day 2）
- [ ] `Plugins/NetworkAcceleration/` — 完整审查（Day 2）
- [ ] `Plugins/CustomMouse/` — 完整审查（Day 3）
- [ ] 插件加载和 ABI 兼容性（Day 4）
- [ ] 插件沙箱边界审查（Day 5）
- [ ] 插件设置持久化完整性（Day 6）
- [ ] 插件进程外执行安全性（Day 7）
