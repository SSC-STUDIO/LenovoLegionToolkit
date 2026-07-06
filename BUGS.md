# UniversalDeviceToolkit — Error & Bug Tracking

> 本文件记录代码审查中发现的所有错误、Bug 和潜在问题。
> 由 AI 代码审查持续更新，按发现日期和问题严重程度排列。

## 状态说明

- 🔴 **Open**: 未修复
- 🟡 **Investigating**: 正在调查
- 🟢 **Fixed**: 已修复
- ⚪ **WontFix**: 决定不修复

---

## 2026-07-06 — Day 1 审查

### 🔴 H-001: `GetAwaiter().GetResult()` 可能导致 UI 线程死锁

- **文件**: `UniversalDeviceToolkit.Lib/System/Management/ManagementEventWatcherExtensions.cs:24`
- **严重程度**: High
- **类别**: 正确性 / 线程安全
- **描述**: `StartWithTimeout` 方法中使用 `task.GetAwaiter().GetResult()` 阻塞等待异步操作完成。如果在 UI 线程调用，且异步操作需要同步回 UI 线程（捕获了 `SynchronizationContext`），会导致死锁。
- **复现步骤**: 在 UI 线程调用任何使用 `StartWithTimeout` 的 WMI 事件监听，且 WMI 操作需要同步回 UI 上下文时触发。
- **建议修复**: 将调用链改为纯异步，或使用 `ConfigureAwait(false)` 确保不捕获同步上下文。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🔴 H-002: IPC 服务端认证挑战-响应未正确实现

- **文件**: `UniversalDeviceToolkit.WPF/CLI/IpcServer.cs:395-420`
- **严重程度**: High
- **类别**: 安全性
- **描述**: 服务端用 `ProtectedData.Protect` 加密了挑战并发送给客户端，但验证时比较的是明文挑战和客户端返回的十六进制字符串。`ComputeAuthToken` 在客户端（`IpcClient.cs:254`）只是返回 `Convert.ToHexString(challenge)`，并未正确解密服务端发送的加密挑战。当前安全性实际依赖命名管道 ACL（仅当前用户 + Administrators），认证机制是虚假的。
- **建议修复**: 要么正确实现加密验证（客户端用 `ProtectedData.Unprotect` 解密），要么移除加密步骤并明确注释依赖管道 ACL。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🔴 H-003: Fire-and-Forget 异步调用异常未被观察

- **文件**: `UniversalDeviceToolkit.Lib/Controllers/AIController.cs:96,113,130`, `UniversalDeviceToolkit.Lib/Listeners/AbstractWMIListener.cs:115`
- **严重程度**: High
- **类别**: 正确性 / 可调试性
- **描述**: `_ = SomeAsyncMethod()` 模式会丢失异常。虽然 .NET 中未观察异常默认不会崩溃进程，但异常信息会完全丢失，导致调试极其困难。
- **建议修复**: 使用 `Task.Run` 包装并捕获异常，或改用 `await` + 后台队列。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🟡 M-001: `IoCContainer.Resolve<T>()` 缺少线程安全保护

- **文件**: `UniversalDeviceToolkit.Lib/IoCContainer.cs:35-45`
- **严重程度**: Medium
- **类别**: 线程安全
- **描述**: `Initialize` 方法使用 `lock (_lock)` 保护，但 `Resolve<T>()` 没有锁。如果初始化和多线程解析同时发生，可能读到部分构建的容器。
- **建议修复**: 在 `Resolve` 中也使用 `lock`，或改用 `Lazy<Container>`。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-002: `GPUProcessManager.KillProcess` 强制终止进程树无优雅关闭

- **文件**: `UniversalDeviceToolkit.Lib/Controllers/GPUProcessManager.cs:45`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `process.Kill(true)` 强制终止整个进程树，可能导致数据丢失或资源未清理。
- **建议修复**: 先尝试 `CloseMainWindow()` + 等待超时，失败后再强制终止。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-003: `DevicePackManager` 验证顺序 — 文件系统操作在完整验证之前

- **文件**: `UniversalDeviceToolkit.Lib/DeviceSupport/DevicePackManager.cs:189-210`
- **严重程度**: Medium
- **类别**: 安全性
- **描述**: `.pending` 目录的创建发生在完整 zip slip 验证之前。虽然提取时有二次检查，但中间状态的目录已被创建。
- **建议修复**: 将所有验证（zip slip、文件扩展名等）移到任何文件系统操作之前。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟢 L-001: `App.xaml.cs` Service Locator 反模式

- **文件**: `UniversalDeviceToolkit.WPF/App.xaml.cs:450-480`
- **严重程度**: Low
- **类别**: 代码质量
- **描述**: 使用 `ConcurrentDictionary<Type, object?>` 实现 Service Locator，隐藏了类的依赖关系。
- **建议修复**: 逐步迁移到构造函数注入。
- **状态**: ⚪ WontFix（大规模重构，收益有限）
- **发现日期**: 2026-07-06

### 🟢 L-002: `GameAutoListener` 进程 ID 重用风险

- **文件**: `UniversalDeviceToolkit.Lib/AutoListeners/GameAutoListener.cs:265-272`
- **严重程度**: Low
- **类别**: 正确性
- **描述**: `ProcessEqualityComparer` 仅比较 `Process.Id`，进程退出后 ID 可能被操作系统重用，导致错误匹配。
- **建议修复**: 定期清理已退出进程，或同时比较进程启动时间。
- **状态**: 🟢 Fixed（待确认）
- **发现日期**: 2026-07-06

### 🔴 H-004: `AbstractEventLogListener` 和 `AbstractWMIListener` 的 fire-and-forget 异常丢失

- **文件**: `UniversalDeviceToolkit.Lib/Listeners/AbstractEventLogListener.cs:52-53`, `UniversalDeviceToolkit.Lib/Listeners/AbstractWMIListener.cs:113-115`
- **严重程度**: High
- **类别**: 正确性 / 可调试性
- **描述**: 两个抽象 Listener 基类都使用了 `_ = HandlerAsync(...)` fire-and-forget 模式。`AbstractEventLogListener.Watcher_EventRecordWrittenAsync` 中的异常虽然被 try-catch 包裹，但 `AbstractWMIListener.HandlerAsync` 中的 `OnChangedAsync` 异常如果被吞噬，事件处理链会静默失败。
- **建议修复**: 至少记录异常到日志；考虑在 fire-and-forget 外层加 `ContinueWith(t => { if (t.IsFaulted) Log.Error(...); })`.
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🔴 H-005: `NativeWindowsMessageListener.LowLevelKeyboardProc` 中 `Marshal.StructureToPtr` 内存泄漏

- **文件**: `UniversalDeviceToolkit.Lib/Listeners/NativeWindowsMessageListener.cs:403-416`
- **严重程度**: High
- **类别**: 资源泄漏
- **描述**: `RegisterDeviceNotification` 方法中，`Marshal.AllocHGlobal` 分配了非托管内存，然后在 `finally` 中 `Marshal.FreeHGlobal(ptr)` 释放。但 `RegisterDeviceNotification` 的 Windows API 文档要求调用方**不要释放** `DEV_BROADCAST_DEVICEINTERFACE_W` 结构的内存——系统在需要时自己读取。当前代码在 `finally` 中释放了内存，可能导致 `RegisterDeviceNotification` 在后续访问已释放内存，造成随机崩溃。
- **建议修复**: 不要在 `finally` 中释放 `ptr`。应该将 `ptr` 保存为字段，在 `StopAsync` / `Dispose` 中再释放。或者直接使用栈分配（`stackalloc`）避免这个问题。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🟡 M-004: `HybridModeFeature.SetStateAsync` 中 `IGPUModeChangeException` 处理不完整

- **文件**: `UniversalDeviceToolkit.Lib/Features/Hybrid/HybridModeFeature.cs:100-113`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: 当 `igpuModeFeature.SetStateAsync` 抛出 `IGPUModeChangeException` 时，仅当 `gSyncChanged` 为 false 时才重新抛出。但如果 `gSyncChanged` 为 true，异常被静默吞掉，且只调用了 `dgpuNotify.NotifyLaterIfNeededAsync()`。这意味着混合模式的部分设置失败被静默忽略，用户可能处于不一致的状态。
- **建议修复**: 至少记录警告日志；考虑回滚 `gSync` 设置或向用户显示需要重启的通知。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-005: `GPUOverclockController` NVAPI 生命周期管理不安全

- **文件**: `UniversalDeviceToolkit.Lib/Controllers/GPUOverclockController.cs:261-296`
- **严重程度**: Medium
- **类别**: 资源管理
- **描述**: `ApplyStateAsync` 中 `NVAPI.Initialize()` 在 try 块中调用，但如果在 `SetOverclockInfo` 过程中抛出异常，`NVAPI.Unload()` 在 finally 中被调用。然而 `GetMaxMemoryDeltaMhz()` 方法（第 43-58 行）也有自己的 `Initialize/Unload` 对。如果并发调用 `GetMaxMemoryDeltaMhz` 和 `ApplyStateAsync`，NVAPI 可能被重复 `Initialize`（虽不一定崩溃，但是未定义行为）。NVAPI 不是线程安全的。
- **建议修复**: 使用 `SemaphoreSlim` 或 `AsyncLock` 确保 NVAPI 操作串行化；或使用 `AsyncLazy` 确保只初始化一次。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-006: `DisplayConfigurationListener` 未处理 `SystemEvents` 的 `UserPreferenceChanged` 或会话切换

- **文件**: `UniversalDeviceToolkit.Lib/Listeners/DisplayConfigurationListener.cs:32`
- **严重程度**: Medium
- **类别**: 功能完整性
- **描述**: 仅监听了 `DisplaySettingsChanged`，但 HDR 状态变化也可能通过 `UserPreferenceChanged` 或更底层的显示通知触发。在某些显示器热插拔场景下，`DisplaySettingsChanged` 可能不会触发，导致 HDR 状态不同步。
- **建议修复**: 同时监听 `SystemEvents.DisplaySettingsChanging` 和 `NativeWindowsMessageListener` 的 `MonitorConnected/Disconnected` 事件来刷新 HDR 状态。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟢 L-003: `SensorsControllerV3` 与 `SensorsControllerV5` 传感器 ID 不一致

- **文件**: `UniversalDeviceToolkit.Lib/Controllers/Sensors/SensorsControllerV3.cs:10-13`, `SensorsControllerV5.cs:10-15`
- **严重程度**: Low
- **类别**: 可维护性
- **描述**: V3 使用 `CPU_SENSOR_ID=4, GPU_SENSOR_ID=5`，而 V5 使用 `CPU_SENSOR_ID=1, GPU_SENSOR_ID=5, PCH_SENSOR_ID=4`。不同版本的传感器 ID 不同，但没有文档说明哪些机器使用哪个版本，也没有在 `IsSupportedAsync` 中验证传感器 ID 的存在性（V5 的 `IsSupportedAsync` 没有检查 PCH 传感器是否存在）。
- **建议修复**: 在 V5 的 `IsSupportedAsync` 中也检查 PCH 传感器 ID 的存在；添加 XML 文档注释说明版本差异。
- **状态**: 🟢 Open
- **发现日期**: 2026-07-06

### 🟢 L-004: `GodModeControllerV2.ApplyStateAsync` 中 `failAllowedSettings` 硬编码

- **文件**: `UniversalDeviceToolkit.Lib/Controllers/GodMode/GodModeControllerV2.cs:78-85`
- **严重程度**: Low
- **类别**: 代码质量
- **描述**: `failAllowedSettings` 数组硬编码了 5 个 GPU 相关的 `CapabilityID`。如果未来增加新的 GPU 相关设置，容易忘记加到这个列表里，导致新设置失败时也抛出异常（而实际上应该允许失败）。
- **建议修复**: 使用命名约定或属性标记来区分"允许失败"的设置，而非硬编码列表。例如，可以在 `CapabilityID` 枚举上加 `[FailAllowed]` 属性。
- **状态**: 🟢 Open
- **发现日期**: 2026-07-06

### 🔴 H-006: `SmartFnLockController` 键状态跟踪不准确

- **文件**: `UniversalDeviceToolkit.Lib/Controllers/SmartFnLockController.cs:69-101`
- **严重程度**: High
- **类别**: 正确性
- **描述**: `_ctrlDepressed`、`_shiftDepressed`、`_altDepressed` 三个标志位在 `IsModifierKeyPressed` 中根据按键的 `WM_KEYDOWN`/`WM_KEYUP` 更新。但问题在于：1) 如果用户按下一个修饰键后，另一个修饰键先触发 `WM_KEYDOWN` 再触发 `WM_SYSKEYDOWN`，状态可能不同步；2) 最重要的是，如果 `OnKeyboardEvent` 被调用时 `wParam` 指示的是 `WM_KEYUP`，但 `IsModifierKeyPressed` 先更新了标志位（设为 false），然后返回 `false`——但如果这是修饰键本身的 `KEYUP`，应该先检查是否是修饰键+普通键的组合已完成，再清除标志。
- **复现步骤**: 用户按下 Ctrl 键（不释放），然后按 F 功能键——预期 SmartFnLock 暂时关闭 FnLock，但当前逻辑可能在 Ctrl 按下后第一个普通键就重置了 `_restoreFnLock`。
- **建议修复**: 重新设计修饰键状态跟踪——使用 `GetKeyState` API 获取真实的异步键状态，而非依赖事件顺序。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🔴 H-007: `NetworkAccelerationRuntime.Stop()` 中 `_loopTask.Wait()` 在 UI 线程可能死锁

- **文件**: `UniversalDeviceToolkit-Plugins/Plugins/NetworkAcceleration/NetworkAccelerationRuntime.cs:95`
- **严重程度**: High
- **类别**: 线程安全
- **描述**: `Stop()` 方法调用 `_loopTask.Wait(TimeSpan.FromSeconds(2))`。如果 `Stop()` 在 UI 线程调用（如插件卸载时），而 `_loopTask` 内部有 `await` 且同步上下文未被 `ConfigureAwait(false)` 正确隔离，会导致死锁。此外，`Wait()` 会阻塞调用线程 2 秒。
- **建议修复**: `Stop()` 应改为异步方法 `StopAsync()`，或在后台线程调用 `Wait()`。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🟡 M-007: `PowerModeListener.ChangeDependenciesAsync` 无错误处理

- **文件**: `UniversalDeviceToolkit.Lib/Listeners/PowerModeListener.cs:43-50`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `ChangeDependenciesAsync` 中连续调用 `godModeController.ApplyStateAsync()`、`windowsPowerModeController.SetPowerModeAsync()` 和 `windowsPowerPlanController.SetPowerPlanAsync()`，没有任何错误处理。如果 `SetPowerPlanAsync` 失败（如无管理员权限），用户不会收到任何错误提示，且电源模式状态会与实际不一致。
- **建议修复**: 为每个操作添加 try-catch，记录错误并通过 `MessagingCenter` 发布错误通知。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-008: `GPUOverclockController.EnsureProfiles()` 中 `store.Profiles` 被直接修改后重新赋值

- **文件**: `UniversalDeviceToolkit.Lib/Controllers/GPUOverclockController.cs:370-400`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `EnsureProfiles()` 中 `store.Profiles` 是 `IReadOnlyDictionary`，但代码直接对其赋值（`store.Profiles = new Dictionary<...>`）。如果 `GPUOverclockSettings.SynchronizeStore()` 的序列化是线程不安全的（多个线程同时调用 `SaveProfile`/`DeleteProfile`），可能导致设置丢失。
- **建议修复**: 在 `GPUOverclockSettings` 的所有公共方法中使用 `lock` 或 `SemaphoreSlim` 保护。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟢 L-005: `AbstractWmiFeature<T>` 中 `getValue` / `setValue` 委托的异常处理

- **文件**: `UniversalDeviceToolkit.Lib/Features/AbstractWmiFeature.cs:43-65`
- **严重程度**: Low
- **类别**: 健壮性
- **描述**: `GetStateAsync` 和 `SetStateAsync` 中的 WMI 调用如果抛出异常，会被传播到调用方。`IsSupportedAsync` 中有 try-catch，但 `GetStateAsync`/`SetStateAsync` 没有。如果 WMI 在运行时变得不可用（如系统进入睡眠后恢复），调用方需要自己处理异常。
- **建议修复**: 在 `GetStateAsync`/`SetStateAsync` 中添加 try-catch，记录日志并抛出有意义的异常（或返回默认值）。
- **状态**: 🟢 Open
- **发现日期**: 2026-07-06

---

---

## 2026-07-06 — Day 2 审查

### ⚪ H-008: `WMIWrapper.cs` 调用 `GetAsyncWithTimeout()` — 误报，扩展方法实际存在（Day 4 修正）

- **文件**: `UniversalDeviceToolkit.Lib/System/Management/WMIWrapper.cs:28,115`
- **严重程度**: N/A（误报）
- **类别**: 正确性
- **描述**: Day 2 初步报告认为 `GetAsyncWithTimeout()` 不存在会导致编译失败。Day 4 完整确认：`GetAsyncWithTimeout()` 是定义在 `Extensions/ManagementObjectSearcherExtensions.cs:13` 中的扩展方法，`WMIWrapper.cs` 第 4 行已有 `using LenovoLegionToolkit.Lib.Extensions;`，`WMI.cs` 第 7 行也有同样的 using。项目可以正常编译。
- **状态**: ⚪ WontFix（Day 4 确认误报）
- **发现日期**: 2026-07-06
- **修正日期**: 2026-07-06

### 🔴 H-009: `WMIWrapper.Subscribe` 中 `watcher.StartWithTimeout()` 可能阻塞

- **文件**: `UniversalDeviceToolkit.Lib/System/Management/WMIWrapper.cs:78`
- **严重程度**: High
- **类别**: 线程安全 / 可用性
- **描述**: `Subscribe` 方法调用 `watcher.StartWithTimeout()`，这是一个同步阻塞调用（根据 Day 1 发现的 `StartWithTimeout` 实现，它使用 `task.GetAwaiter().GetResult()`）。如果在 UI 线程调用 `Subscribe`，会导致死锁。
- **建议修复**: 将 `Subscribe` 改为异步方法，或使用 `Task.Run` 包装 `StartWithTimeout` 调用。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🟡 M-009: `NotificationsManager` 中 `_windows` 列表可能泄漏通知窗口引用

- **文件**: `UniversalDeviceToolkit.WPF/Utils/NotificationsManager.cs:26,340-345`
- **严重程度**: Medium
- **类别**: 资源管理
- **描述**: `_windows` 是 `List<INotificationWindow?>`。在 `ShowOnScreen` 中，窗口被添加到 `_windows`（`_windows.Add(nw)` 或 `_windows.Add(nwaot)`）。虽然 `Dispose()` 中调用了 `window?.Close(true)` 和 `_windows.Clear()`，但如果通知窗口在非正常路径下关闭（如用户手动关闭），`_windows` 中的引用不会被移除，导致内存泄漏。
- **建议修复**: 使用 `WeakReference` 或在窗口 `Closed` 事件中从 `_windows` 移除引用。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-010: `Power.IsBatterySaverEnabled()` 使用 `SystemStatusFlag` 语义不完整

- **文件**: `UniversalDeviceToolkit.Lib/System/Power.cs:29-33`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `IsBatterySaverEnabled()` 返回 `sps.SystemStatusFlag == 1`。根据 Windows API 文档，`SystemStatusFlag` 的值语义在不同 Windows 版本中可能不同。某些文档指出 `SystemStatusFlag` 的位 0 表示电池节电模式，但还有其他位表示不同状态。直接比较 `== 1` 可能在未来 Windows 版本中失效。
- **建议修复**: 使用 `PInvoke.DevicePowerEnumerateDevices` 或 `Guid` 查询电池节电状态的官方 API，而非解析 `SystemPowerStatus` 的非公开字段。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟢 L-006: `WMIWrapper.ConvertManagementObject` 使用 `Convert.ChangeType` 可能抛出异常

- **文件**: `UniversalDeviceToolkit.Lib/System/Management/WMIWrapper.cs:126-152`
- **严重程度**: Low
- **类别**: 健壮性
- **描述**: `ConvertManagementObject` 使用 `Convert.ChangeType(managementProp.Value, prop.PropertyType)` 进行类型转换。如果 WMI 属性的类型无法转换为目标类型（如 `ulong` 转为 `int`），会抛出 `InvalidCastException`。虽然有 try-catch，但转换失败会被静默忽略，导致返回的 `T` 对象包含默认值。
- **建议修复**: 使用更精确的类型转换逻辑；记录转换失败的详细信息以帮助调试。
- **状态**: 🟢 Open
- **发现日期**: 2026-07-06

---

---

## 2026-07-06 — Day 3 审查

### 🔴 H-010: `IpcClient.ComputeAuthToken` 认证逻辑虚假 — 仅 Hex 编码未解密

- **文件**: `UniversalDeviceToolkit.CLI/IpcClient.cs:254`
- **严重程度**: High
- **类别**: 安全性
- **描述**: Day 1 初步怀疑 IPC 认证机制有问题。Day 3 完整读取确认：`ComputeAuthToken` 仅对服务端发送的挑战字节做 `Convert.ToHexString(challenge)`，并未使用 `ProtectedData.Unprotect` 解密。这意味着任何能连接命名管道的进程（同一用户 + Administrators）都可以伪造认证。`IpcServer` 端（Day 1 H-002）也比较明文挑战，整个认证是虚假的。
- **建议修复**: 正确实现挑战-响应：服务端用 `ProtectedData.Protect(challenge, null, DataProtectionScope.CurrentUser)` 加密后发送；客户端用 `ProtectedData.Unprotect` 解密后返回；服务端验证解密结果。或者，明确注释依赖管道 ACL 安全，移除加密步骤。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🔴 H-011: `ConsoleLoadingAnimation.Dispose` 中 `GetAwaiter().GetResult()` 可能死锁

- **文件**: `UniversalDeviceToolkit.CLI/ConsoleLoadingAnimation.cs:43`
- **严重程度**: High
- **类别**: 线程安全
- **描述**: `Dispose()` 调用 `_task?.GetAwaiter().GetResult()` 阻塞等待动画渲染任务完成。如果 `RenderLoopAsync` 正在 `await Task.Delay(...)` 且同步上下文未被 `ConfigureAwait(false)` 正确隔离，`GetResult()` 可能导致死锁。虽然 `RenderLoopAsync` 中使用了 `ConfigureAwait(false)`，但 `Dispose()` 本身可能在 UI 上下文或特殊同步上下文中被调用。
- **建议修复**: 使用 `await _task` 替代 `GetAwaiter().GetResult()`；或将 `Dispose` 改为异步（实现 `IAsyncDisposable`）。
- **状态**: 🔴 Open
- **发现日期**: 2026-07-06

### 🟡 M-011: `Flags.StringValue` 在参数无值时崩溃

- **文件**: `UniversalDeviceToolkit.CLI/Flags.cs:37-38`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `StringValue` 使用 `value.Remove(0, key.Length + 1)` 提取值。如果参数格式为 `--quickAction`（没有 `=` 或空格后的值），`value` 等于 `key`，`Remove(0, key.Length + 1)` 会抛出 `ArgumentOutOfRangeException`（因为长度不够）。CLI 参数解析应该支持 `--name value` 和 `--name=value` 两种格式。
- **建议修复**: 检查 `value.Length` 是否足够；或使用 `System.CommandLine` 库替代手写解析。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-012: `NetworkAccelerationPlugin` 中 `OnShutdown` 调用同步 `Stop()` 可能死锁

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationPlugin.cs:93`
- **严重程度**: Medium
- **类别**: 线程安全
- **描述**: `OnShutdown()` 调用 `_runtime.Stop()`（同步版本）。`Stop()` 内部调用 `_loopTask.Wait(TimeSpan.FromSeconds(2))`，如果调用线程是 UI 线程且 `_loopTask` 需要同步上下文，会导致死锁。虽然 `NetworkAccelerationRuntime` 也有 `StopAsync()` 方法，但 `OnShutdown` 没有使用它。
- **建议修复**: `OnShutdown()` 应调用 `_runtime.StopAsync()` 且不等待完成（fire-and-forget with error handling）；或在整个插件框架中统一使用异步生命周期方法。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟢 L-007: `NetworkAccelerationPlugin` 中 `SharedProcessRunner` 是静态共享实例

- **文件**: `Plugins/NetworkAcceleration/NetworkAccelerationPlugin.cs:22`
- **严重程度**: Low
- **类别**: 线程安全
- **描述**: `SharedProcessRunner` 被声明为 `private static readonly ProcessRunner`。如果多个插件实例或并发操作使用同一个 `ProcessRunner`，虽然 `ProcessRunner` 的方法内部创建新的 `Process` 对象，但 `SharedProcessRunner` 的 `_logger` 等状态是共享的。更重要的是，这个命名具有误导性——它暗示这是一个共享的、线程安全的管理器。
- **建议修复**: 将 `SharedProcessRunner` 改为实例字段（非 static）；或确认 `ProcessRunner` 的所有字段都是线程安全的。
- **状态**: 🟢 Open（低风险）
- **发现日期**: 2026-07-06

---

## 2026-07-06 — Day 4 审查

### 🟡 M-013: `ManagementObjectSearcherExtensions.GetAsync` 新建 `ManagementObjectSearcher` 未复制 `Options`

- **文件**: `UniversalDeviceToolkit.Lib/Extensions/ManagementObjectSearcherExtensions.cs:21-28`
- **严重程度**: Medium
- **类别**: 正确性
- **描述**: `GetAsync` 在 `Task.Run` 内部新建了 `ManagementObjectSearcher(scopePath, queryString)`，但没有复制原 `mos.Options`（如 `Timeout`、`Context`、`Impersonation` 等）。如果调用方对原 `ManagementObjectSearcher` 设置了自定义 `Options`，这些设置会在异步查询中丢失。
- **建议修复**: 在新建 `ManagementObjectSearcher` 后复制 `Options` 属性；或直接在新线程中使用传入的 `mos` 对象（需注意 `ManagementObjectSearcher` 是否线程安全）。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-014: `AbstractWMIListener.Dispose()` 中 `StopAsync()` 未等待完成

- **文件**: `UniversalDeviceToolkit.Lib/Listeners/AbstractWMIListener.cs:118-136`
- **严重程度**: Medium
- **类别**: 资源管理
- **描述**: `Dispose(bool disposing)` 中调用 `_ = StopAsync()` 但未 await。`StopAsync` 中的 `_disposable?.Dispose()` 可能在实际释放完成前就已返回。如果 `Dispose` 被调用后随即进行垃圾回收，WMI 事件监听器可能仍然活跃。
- **建议修复**: 将 `Dispose` 改为异步（实现 `IAsyncDisposable`）；或在 `Dispose(bool)` 中使用 `StopAsync().GetAwaiter().GetResult()`（需注意死锁风险）。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟡 M-015: `FpsSensorController` 中 `Blacklist` 公开可变且非线程安全

- **文件**: `UniversalDeviceToolkit.Lib/Controllers/Sensors/FpsSensorController.cs:27`
- **严重程度**: Medium
- **类别**: 线程安全
- **描述**: `Blacklist` 是 `public List<string>`，可从外部修改。但 `IsProcessBlacklisted` 在 `GetForegroundProcess()` 中读取（可能在任意线程），而外部可能在另一线程修改 `Blacklist`。`List<>` 不是线程安全的，并发读写可能导致 `InvalidOperationException`。
- **建议修复**: 将 `Blacklist` 改为 `IReadOnlyList<string>` 或使用 `ImmutableList<string>`；或在使用时创建副本。
- **状态**: 🟡 Investigating
- **发现日期**: 2026-07-06

### 🟢 L-008: `ManagementObjectSearcherExtensions.GetAsync` 中 `searcher.Get()` 不取消 — 超时实现不彻底

- **文件**: `UniversalDeviceToolkit.Lib/Extensions/ManagementObjectSearcherExtensions.cs:21-28`
- **严重程度**: Low
- **类别**: 正确性
- **描述**: `GetAsync` 使用 `Task.Run(() => searcher.Get())` 在线程池线程执行 WMI 查询，然后用 `Task.WhenAny` 实现超时。但如果查询超时，`Task.Run` 中的查询仍会继续执行（直到完成或失败），只是结果被丢弃。对于耗时较长的 WMI 查询，这会浪费线程池线程。
- **建议修复**: 使用 `ManagementObjectSearcher.GetAsync()`（.NET 8+）或 `CancellationToken` 注册取消回调；或在超时后主动忽略结果。
- **状态**: 🟢 Open（低风险）
- **发现日期**: 2026-07-06

---

## 统计

- 🔴 Open (High): 10
- 🟡 Investigating (Medium): 15
- 🟢 Open / ⚪ WontFix (Low): 7
- **总计**: 32

## 待深入模块（后续天数）

- [x] `UniversalDeviceToolkit.Lib/Features/` — 所有 Feature 实现（Day 1 ✅）
- [x] `UniversalDeviceToolkit.Lib/Listeners/` — 所有 WMI Listener（Day 1 ✅）
- [x] `UniversalDeviceToolkit.Lib/System/` — P/Invoke 调用（Day 2 ✅）
- [x] `UniversalDeviceToolkit.WPF/Utils/` — UI 工具类（Day 2 ✅）
- [x] `UniversalDeviceToolkit.WPF/Pages/` — WPF 页面（Day 3 ✅）
- [x] `UniversalDeviceToolkit.CLI/` — CLI 实现（Day 3 ✅）
- [x] 资源泄漏审计（所有 `IDisposable` 实现）（Day 4 ✅）
- [ ] 异常处理完整性审计（Day 5）
- [ ] 线程安全审计（所有共享可变状态）（Day 6）
- [ ] 安全审计（所有用户输入点、文件路径操作）（Day 7）
