# Knowledge Base / 知识�?

This is a living ledger of lessons learned during development and maintenance of Universal Device Toolkit.
Agents and human maintainers MUST append new entries when solving non-trivial bugs, discovering OS quirks, or optimizing architecture.

---

## Entry Template

```markdown
### [YYYY-MM-DD] Topic / 主题
- **Symptom / 症状**: ...
- **Root Cause / 根因**: ...
- **Enforced Rule / 强制规则**: ...
- **OS / .NET Version / OS�?NET版本**: ...
```

---

### [2026-07-06] CardHeaderControl subtitle text overflow / 卡片副标题文本溢�?
- **Symptom / 症状**: Long Chinese translation strings caused CardControl subtitles to overflow their containers, bloating card heights and reducing UI aesthetics. Cards like "System Optimization", "Plugin Extensions", etc. showed truncated or overlapping text.
- **Root Cause / 根因**: `CardHeaderControl._subtitleTextBlock` had `TextWrapping="Wrap"` and `TextTrimming="CharacterEllipsis"` but no `MaxHeight` constraint. Chinese translations for many `Message`-suffixed resource keys were 50-80+ characters, causing 3-5 lines of wrapped text that pushed card layouts.
- **Fix / 修复**: (1) Added `MaxHeight = 60` (�? lines) to `_subtitleTextBlock` in `CardHeaderControl.cs`. (2) Shortened 10+ long Chinese strings in `Resource.zh-hans.resx` to �?0 chars, using `\n` line breaks for necessary multi-line content.
- **Enforced Rule / 强制规则**: All Chinese (zh-Hans) resource values in `Resource.zh-hans.resx` MUST be �?0 characters per line. Use `\n` for intentional line breaks. `CardHeaderControl` enforces `MaxHeight=60` on subtitle; do NOT remove this constraint.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] Language selector and main window popup simultaneously / 语言选择与主窗口同时弹出
- **Symptom / 症状**: On first launch (or when language selection is needed), both the language selector window AND the main window appeared simultaneously. The main window behind the language selector created visual confusion.
- **Root Cause / 根因**: In `LocalizationHelper.cs`, `window.Show()` was used to display the `LanguageSelectorWindow`. `Show()` is non-modal �?it returns immediately, so the startup flow continued and opened the main window behind the language selector.
- **Fix / 修复**: Changed `window.Show()` to `window.ShowDialog()` at line 153 of `LocalizationHelper.cs`. `ShowDialog()` is modal �?it blocks until the user finishes language selection, then the startup flow continues and opens the main window.
- **Enforced Rule / 强制规则**: Language selector and any "must-complete-first" dialogs during app startup MUST use `ShowDialog()`, never `Show()`. The startup orchestrator (`StartupOrchestrator`) depends on synchronous completion of language selection.
- **OS / .NET Version / OS�?NET版本**: Windows 11, .NET 10, WPF

---

### [2026-07-06] Chinese translation string optimization for card layouts / 中文翻译字符串针对卡片布局的优�?
- **Symptom / 症状**: Multiple settings cards and automation step controls had Chinese subtitles that were too long, causing card height bloat and text truncation with ellipsis that didn't convey full meaning.
- **Root Cause / 根因**: Initial machine translation from English to Chinese produced verbose strings. Chinese characters are denser than Latin characters in vertical space consumption when wrapped, but the original translations didn't account for WPF `TextBlock` wrapping behavior.
- **Fix / 修复**: Systematically shortened Chinese translations in `Resource.zh-hans.resx` for ~15 keys. Applied these principles:
  - Titles: �?2 characters
  - Subtitle/Message: �?0 characters per logical line, use `\n` for breaks
  - Action labels: �? characters
  - Error messages: �?0 characters
- **Enforced Rule / 强制规则**: When adding or modifying Chinese localization strings, always validate visual length in the actual WPF card layout. Use the 40-char rule for subtitles. Reference the English base (`Resource.resx`) but adapt for Chinese linguistic density.
- **OS / .NET Version / OS�?NET版本**: Windows 11, .NET 10, WPF

---

### [2026-07-06] WMI query deadlock protection in async methods / 异步方法中WMI查询死锁保护
- **Symptom / 症状**: (From prior sessions) WMI queries in `AbstractWmiFeature` and related classes could deadlock the UI thread when `ConfigureAwait(true)` was used or when synchronous WMI calls were made on the UI thread.
- **Root Cause / 根因**: WMI queries are inherently synchronous and blocking. When called from the UI thread without proper async wrapping or timeouts, they cause deadlocks. The original code had a mix of `.ConfigureAwait(true)` and `.ConfigureAwait(false)` usage.
- **Fix / 修复**: Applied `.ConfigureAwait(false)` to all `await` calls in Lib (12 places), WPF Controls (200+), Pages (75), Windows (120), Utils/Extensions/ViewModels (64). Wrapped WMI calls in `Task.Run()` with `CancellationToken` support and 2500-3000ms timeout.
- **Enforced Rule / 强制规则**: ALL WMI queries MUST be wrapped in async methods with `Task.Run()` and a `CancellationToken` with 3000ms maximum timeout. NEVER call WMI synchronously on the UI thread. ALL `await` calls in Lib/SDK code MUST use `.ConfigureAwait(false)`.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] Memory leak pattern: WPF controls not unsubscribing from singleton events / 内存泄漏模式：WPF控件未取消单例事件订�?
- **Symptom / 症状**: (From prior sessions) Navigating between pages caused memory to grow unbounded. Memory Analyzer showed multiple instances of controls like `PowerModeControl`, `SensorsControl`, `DiscreteGPUControl` remaining in memory after navigation away.
- **Root Cause / 根因**: Controls subscribed to `MessagingCenter`, `AbstractSettings.Changed`, `Listener.Changed` (registry/WM! watchers) in their constructor or `OnInitialized` but never unsubscribed. When the page was navigated away from, the controls were still referenced by the singleton event source, preventing garbage collection.
- **Fix / 修复**: Added `Unloaded` event handlers to 30+ controls/pages. In the `Unloaded` handler, explicitly unsubscribe from all singleton events. Implemented `IDisposable` on `SensorsControl`, `PowerModeControl`, `PackageControl`, etc. to dispose `CancellationTokenSource`, `ThrottleLastDispatcher`, and `Process` fields.
- **Enforced Rule / 强制规则**: Every WPF control/paje that subscribes to a singleton event (`MessagingCenter`, `AbstractSettings.Changed`, registry listeners, `IRefreshable.Refreshed`) MUST unsubscribe in an `Unloaded` handler. Controls that own `CancellationTokenSource`, timers, or `Process` objects MUST implement `IDisposable` and clean up in `Unloaded`.
- **OS / .NET Version / OS�?NET版本**: Windows 11, .NET 10, WPF

---

### [2026-07-06] WMI helper extension timeout ceiling violation (UDT-001/002)

- **Symptom / 症状**: WMI helper extensions defaulted to `5000ms` timeout and `WMI.CallInternalAsync` used a `10000ms` timeout, both exceeding the 3000ms ceiling defined in KNOWLEDGE_BASE.md.
- **Root Cause / 根因**: `ManagementObjectSearcherExtensions.GetAsyncWithTimeout` and `ManagementEventWatcherExtensions.StartAsyncWithTimeout` hardcoded default `timeoutMs = 5000`; `WMI.CallInternalAsync` used `Task.Delay(10000, cts.Token)` to bound `ManagementObject.InvokeMethod`. Callers relying on defaults allowed WMI to block up to 10s.
- **Fix / 修复**: (1) Lowered both WMI helper extension defaults from `5000` to `2500` ms. **Correction (2026-07-06, pass 6):** this 5000->2500 reduction never landed in the two extension files at the committed HEAD (`git show HEAD:UniversalDeviceToolkit.Lib/Extensions/ManagementObjectSearcherExtensions.cs` and `...ManagementEventWatcherExtensions.cs` still showed `timeoutMs = 5000`); it was actually delivered by [UDT-007] in pass 6, which also fixed a 3-space->4-space indentation regression. Clause (2) below DID land. (2) Added `WmiInvokeTimeoutMs = 2500` constant in `WMI.cs` and replaced the hardcoded `10000` with the constant.
- **Enforced Rule / 强制规则**: WMI helper extension default timeouts MUST NOT exceed `2500ms`. WMI method invokes MUST use a timeout constant <= `2500ms`.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] AmdOverclockingController synchronous WMI calls blocking UI (UDT-003)

- **Symptom / 症状**: `FetchCommands()` ran synchronous `new ManagementObject(...)` and `WMI.InvokeMethodAndGetValue(...)` without `Task.Run()` or timeout. Called from async `InitializeAsync`, this could stall the UI thread ~5-10s on AMD systems.
- **Root Cause / 根因**: ZenStates-Core `WMI.InvokeMethodAndGetValue` and `WMI.GetInstanceName` are synchronous NuGet-provided WMI methods. The original `FetchCommands()` invoked them directly on the calling thread with no async wrapping or timeout.
- **Fix / 修复**: Converted `public void FetchCommands()` to `public async Task FetchCommandsAsync(CancellationToken cancellationToken = default)`. Wrapped both synchronous WMI calls in `Task.Run(...).WaitAsync(TimeSpan.FromMilliseconds(2500), cancellationToken).ConfigureAwait(false)`.
- **Enforced Rule / 强制规则**: All WMI queries and method invokes -- including those from third-party NuGet packages -- MUST be wrapped in `Task.Run()` with a `CancellationToken` and 2500ms timeout. Lib code MUST use `.ConfigureAwait(false)`.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] Empty catch block swallowing exceptions in Registry teardown (UDT-004)

- **Symptom / 症状**: The `LambdaDisposable` returned by the Win32 registry listener used `try { task.Wait(1000); } catch { }` during teardown, silently discarding all exceptions.
- **Root Cause / 根因**: A bare `catch { }` discards every exception type with zero tracing, unlike the surrounding listener loop which funnels exceptions through `Log.Instance.Trace(...)`.
- **Fix / 修复**: Replaced `catch { }` with `catch (Exception ex) { if (Log.Instance.IsTraceEnabled) Log.Instance.Trace("Win32 registry listener dispose wait failed.", ex); }`.
- **Enforced Rule / 强制规则**: Empty `catch { }` blocks are FORBIDDEN in production code. All catch blocks MUST at minimum trace the exception via `Log.Instance.Trace(...)`.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] SmartFnLockController thread pool flooding / SmartFnLock线程池洪�?

- **Symptom / 症状**: Every keypress triggered `Task.Run(...)` to evaluate the Fn-Lock restore state, saturating the thread pool on systems with heavy keyboard input.
- **Root Cause / 根因**: `SmartFnLockController` unconditionally queued async work for every key event, including non-modifier keys that have no effect on Fn-Lock state.
- **Fix / 修复**: Added a synchronous filter before `Task.Run(...)`: only modifier key events or those where `_restoreFnLock` is true spawn async work; all other events are handled synchronously without entering the thread pool.
- **Enforced Rule / 强制规则**: High-frequency event handlers (keyboard, mouse, polling) MUST filter at the synchronous layer before dispatching async work. Only state-changing events should cross the sync/async boundary.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] MacroController WH_KEYBOARD_LL hook without message pump / 宏控制器键盘钩子无消息泵

- **Symptom / 症状**: `WH_KEYBOARD_LL` keyboard hook installed from a background thread had no message pump, causing the hook to silently fail to receive any keyboard events.
- **Root Cause / 根因**: `SetWindowsHookEx` installs the hook on the calling thread, but `WH_KEYBOARD_LL` requires the owning thread to run a message pump (`GetMessage`/`PeekMessage` loop) to process hook callbacks. The original `Start()` method called `SetWindowsHookEx` from a background thread without setting up a message loop.
- **Fix / 修复**: Added `IMainThreadDispatcher _mainThreadDispatcher` dependency. `Start()` now dispatches `SetWindowsHookEx` to the UI thread via `_mainThreadDispatcher.Dispatch(...)`, which already runs a WPF message pump. `IMainThreadDispatcher` is registered in the WPF `IoCModule.cs`.
- **Enforced Rule / 强制规则**: `WH_KEYBOARD_LL` hooks MUST be installed on a thread that owns a message pump (typically the UI thread). Use `IMainThreadDispatcher.Dispatch(...)` to marshal the `SetWindowsHookEx` call to the UI thread. NEVER install `WH_KEYBOARD_LL` on a background thread without a message loop.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---
### [2026-07-06] FlaUI admin-gated tests misclassified as failures / 提权门控 FlaUI 测试被误判为失败 (UDT-005)

- **Symptom / 症状**: Three FlaUI main-window tests (`FlaUIMainWindowTests`) hard-failed in a non-admin/non-desktop runner. The shared base `FlaUiTestBase.InitializeAsync()` throws `Xunit.SkipException` on elevation failure, but the methods were annotated `[Fact]`, and a plain `[Fact]` reports a thrown `SkipException` as a failure rather than a skip.
- **Root Cause / 根因**: `Xunit.SkipException` is only reclassified as a clean skip when the test method carries `[SkippableFact]` / `[SkippableTheory]`. The test project already references `Xunit.SkippableFact`, but these three methods used plain `[Fact]`, unlike their sibling FlaUI classes (`MainWindowSmokeTests`, `MainWindowVisualTests`).
- **Fix / 修复**: Switched all three methods from `[Fact]` to `[SkippableFact]` so `SkipException` is intercepted as a skip; added `MainWindow!` null-forgiving on two dereference sites to clear the pre-existing `CS8602`. Build => 0 errors / 0 warnings; `dotnet test --filter FlaUI.FlaUIMainWindowTests` => 0 failed / 3 skipped (previously 3 failed). Full suite => 2327 passed / 0 failed / 30 skipped.
- **Enforced Rule / 强制规则**: Admin/desktop/elevation-gated UI tests MUST use `[SkippableFact]` / `[SkippableTheory]`, never plain `[Fact]`. `Xunit.SkipException` only yields a clean skip under a skippable attribute; a plain `[Fact]` that throws `SkipException` is an unhandled failure.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] Lenovo Legion Toolkit -> Universal Device Toolkit brand migration: separate user-facing name from ABI identifier / 品牌/ABI 名称分离

- **Symptom / 症状**: After rebranding to Universal Device Toolkit, residual legacy-brand literals survived in Lib production code. The FPS self-monitoring blacklist (`FpsSensorController.InitializeBlacklist`) excluded only `"Lenovo Legion Toolkit"`, so the renamed `Universal Device Toolkit` process was counted as a monitored foreground app. The HWiNFO custom-sensor group (`HWiNFOIntegration.CUSTOM_SENSOR_GROUP_NAME`) registered under the old `"Lenovo Legion Toolkit"` registry key. The Plugin Workbench host-resource resolver (`HostResourceLookup.ResolveResourceType`) matched only the legacy WPF assembly name, so it failed to locate host resources under a UDT-named host.
- **Root Cause / 根因**: The rebrand migrated display metadata (`AppIdentity.DisplayName = "Universal Device Toolkit"`, WPF `<AssemblyName> = "Universal Device Toolkit"`) but left process-name matchers and external-tool registry keys keyed to the old name. Cross-repository ABI identifiers (Lib `<AssemblyName>LenovoLegionToolkit.Lib`, plugin SDK namespaces, `clr-namespace` tokens) were correctly PRESERVED as load contracts; the miss was consumer literals that match the *process name* or *external tool grouping*. `PluginHostContext` / `WpfHostNotifications` already carried dual-name lookup, but the PluginWorkbench tool did not.
- **Fix / 修复**: (1) Added `"Universal Device Toolkit"` next to the legacy entry in the FPS self-blacklist. (2) Renamed `HWiNFOIntegration.CUSTOM_SENSOR_GROUP_NAME` to `"Universal Device Toolkit"` and added `LEGACY_CUSTOM_SENSOR_GROUP_NAME`; `ClearValues()` now deletes both keys so stale legacy sensors are cleaned (`Registry.Delete` is a no-op if absent). (3) `HostResourceLookup.ResolveResourceType` now resolves the host assembly against both `"Universal Device Toolkit"` (first) and `"Lenovo Legion Toolkit"` (fallback), mirroring `WpfHostNotifications` / `PluginHostContext`. (4) Smoke mock copy migrated. ABI assembly/namespace identifiers intentionally left unchanged. Full main + plugin solution builds => 0 errors / 0 warnings.
- **Enforced Rule / 强制规则**: See Engineering Principle #12 (Brand/ABI Name Separation). Migrate user-facing process-matchers, registry keys, and external-tool group names; pair each migrated literal with the legacy one for backward compatibility. NEVER rename cross-repo ABI assembly/namespace/clr-namespace identifiers. When renaming a registry key, also delete the legacy key on stop.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] MacroController hook teardown orphaning WH_KEYBOARD_LL / 宏控制器钩子拆卸�?WH_KEYBOARD_LL 孤儿�?(UDT-006)

- **Symptom / 症状**: A throw from `_recorder.StopRecording()` or `_player.Stop()` inside `MacroController.Stop()` jumped to a bare comment-only `catch { // Ignore }`, skipping `UnhookWindowsHookEx`, while a `finally { _kbHook = default; }` still zeroed the handle. The OS-level `WH_KEYBOARD_LL` hook stayed installed but its handle was lost; the next `Start()` saw `_kbHook == default`, installed a second hook, and every keystroke fired the macro callback chain twice (duplicated macros + system-wide input latency). The orphaned hook could never be removed.

- **Root Cause / 根因**: `Stop()` ran recorder/player teardown in the same `try` as `UnhookWindowsHookEx`, so a teardown throw short-circuited the unhook. The bare comment-only `catch { }` (KB #8 violation) silently swallowed it, and the unconditional `finally` cleared the handle field despite the hook still being installed �?the asymmetry versus `Start()'s` dispatcher-guarded install means `_kbHook` was cleared even when the unhook had not run, coupling handle cleanup to teardown success.

- **Fix / 修复**: Reworked `Stop()` to (1) capture and clear the field first (`var hook = _kbHook; _kbHook = default;`) so a concurrent `Start()` cannot double-install; (2) `UnhookWindowsHookEx(hook)` FIRST, isolated in its own `try/catch`; (3) `_recorder.StopRecording()` and `_player.Stop()` each in their own `try/catch`. Every catch traces via `Log.Instance.IsTraceEnabled`-guarded `Log.Instance.Trace(...)` (KB #8, #9). Build => 0 errors / 0 warnings; full `dotnet test` => 2327 + 119 passed / 0 failed.

- **Enforced Rule / 强制规则**: For system-wide Win32 hooks (`WH_KEYBOARD_LL` etc.), clear the handle field BEFORE teardown and unhook FIRST; isolate each teardown step in its own `try/catch` with tracing. NEVER clear a hook handle in a single `finally` whose cleanup also depends on earlier steps succeeding. See Engineering Principle #13.

- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

### [2026-07-06] Phantom UDT-001 archive fix: WMI extension default timeouts never actually lowered (UDT-007) / UDT-001 幽灵归档记录修复：WMI 扩展默认超时实际从未下调 (UDT-007)

- **Symptom / 症状**: The UDT-001 `**Fixed**` record in `.bugs/4_ARCHIVED.md` claimed both WMI helper extension defaults were lowered from `5000ms` to `2500ms`, but `git show HEAD:...` proved the two extensions still hardcoded `timeoutMs = 5000`; only `WMI.WmiInvokeTimeoutMs` (UDT-002) actually shipped. Seven bare production callers (`WMI.cs:36/70/101/145`, `WMIWrapper.cs:28/78/115`) inherited the unbounded 5000ms default, violating the 2500ms ceiling.
- **Root Cause / 根因**: The UDT-001 archive `**Fixed**` record diverged from the committed code. The `= 2500` rewrite landed for UDT-002 (`WMI.cs`) and UDT-003 (`AmdOverclockingController.cs`) but never reached `ManagementObjectSearcherExtensions.cs` and `ManagementEventWatcherExtensions.cs`, so the false archive record hid a live ceiling violation for multiple passes.
- **Fix / 修复**: (1) Lowered all four `timeoutMs = 5000` default arguments to `= 2500` in `ManagementObjectSearcherExtensions` (`GetAsyncWithTimeout`, `GetAsync`) and `ManagementEventWatcherExtensions` (`StartAsyncWithTimeout`, `StartWithTimeout`). (2) Corrected a 3-space to 4-space indentation regression on the four declaration lines. (3) Added a dated `**Correction**` note to the UDT-001 archive record rather than silently rewriting it.
- **Enforced Rule / 强制规则**: See Engineering Principle #14 (Archive Verification). Bug archive `**Fixed**` records MUST be verified against the actual code state with `rg`/`git show` before a ticket is closed; a phantom-fix must be corrected with a dated note, never silently rewritten.
- **OS / .NET Version / OS�?NET版本**: Windows 11 24H2, .NET 10

---

## Adopted Engineering Principles / 已采纳的工程原则

1. **WPF Thread Safety**: Never use `.ConfigureAwait(true)` in Lib/SDK code. UI updates must go through `Dispatcher.InvokeAsync()`. (`CardHeaderControl` is already correct.)
2. **WMI Timeout Protection**: All WMI/process execution must have 2500-3000ms timeout with `CancellationToken`.
3. **Zero-Spam Polling**: High-frequency monitoring (500-2000ms) must NOT serialize JSON or write trace logs on every tick.
4. **Modular UI**: All XAML must use rounded cards (`CornerRadius="8"`), responsive layouts (`Grid` star-sizing, `WrapPanel`), and 100% host theme brush binding �?never hardcoded hex colors.
5. **Chinese Localization**: Subtitle strings �?0 chars. Use `\n` for line breaks. Validate in actual card layout.
6. **Startup Modal Dialogs**: Language selector and similar first-run dialogs must use `ShowDialog()`, not `Show()`.
7. **Memory Leak Prevention**: Unsubscribe singleton events in `Unloaded`. Dispose `IDisposable` resources. Null out event handlers in `OnDestroy`/`Unloaded`.

8. **Empty Catch Prohibition**: Empty `catch { }` blocks are FORBIDDEN in production code. All catch blocks MUST trace the exception via `Log.Instance.Trace(...)`.
9. **WH_KEYBOARD_LL Hook Message Pump**: `WH_KEYBOARD_LL` hooks MUST be installed on a thread with a message pump (UI thread). Use `IMainThreadDispatcher.Dispatch(...)` to marshal `SetWindowsHookEx`.
10. **Thread Pool Flooding Prevention**: High-frequency event handlers (keyboard, mouse, polling) MUST filter at the synchronous layer before dispatching async work to the thread pool.

11. **Admin/Elevation-Gated Tests Use SkippableFact**: Admin/desktop/elevation-gated UI tests MUST use `[SkippableFact]` / `[SkippableTheory]`, never plain `[Fact]`. `Xunit.SkipException` only yields a clean skip under a skippable attribute; a plain `[Fact]` that throws `SkipException` is an unhandled failure.
12. **Brand/ABI Name Separation**: When rebranding, migrate user-facing strings matched by process name, external-tool registry keys (e.g. HWiNFO sensor groups), and resource-resolver assembly-name lookups, and pair each migrated literal with the legacy one for backward compatibility. Cross-repository ABI identifiers -- assembly names (`LenovoLegionToolkit.Lib`, plugin `<AssemblyName>`), namespaces, and `clr-namespace` tokens -- MUST NOT be renamed to match the brand, because plugin loading depends on them as load contracts. When renaming a registry key, also delete the legacy key on stop (`Registry.Delete` is a no-op if absent).
13. **Win32 Hook Teardown Ordering**: For system-wide Win32 hooks (`WH_KEYBOARD_LL` etc.), capture and clear the handle field BEFORE teardown (`var hook = _kbHook; _kbHook = default;`), then call the unhook API (`UnhookWindowsHookEx`) FIRST so a throw from recorder/player/sensor teardown can never leave the hook installed. Isolate each teardown step in its own `try/catch` traced via `Log.Instance.IsTraceEnabled`-guarded `Log.Instance.Trace(...)` (KB #8). NEVER clear a hook handle in a single `finally` whose cleanup also depends on earlier steps succeeding �?that orphans the hook and lets the next install duplicate callbacks.
14. **Archive Verification**: Bug archive `**Fixed**` records MUST be verified against the actual code state with `rg`/`git show` before a ticket is closed. When a past `**Fixed**` claim is found to have never landed, add a dated `**Correction**` note to the archive entry; do NOT silently rewrite or delete the false record. This keeps the audit trail honest so a phantom-fix can never hide a live defect.

15. **Theme-Bound Vector/Shape Colors**: All colored shapes (`Ellipse.Fill`, `Border.Background`, `Path.Stroke`, etc.) in XAML MUST bind to `{DynamicResource <brush>}` tokens from `DesignTokens.xaml` (e.g., `StatusSuccessBrush`, `StatusWarningBrush`, `StatusCriticalBrush`), never raw hex literals �?raw hex ignores light/dark theme swaps and drifts from the shared chart-keyed color vocabulary. The hardcode-color audit MUST sweep non-text vector/shape properties (`rg -n '(Fill|Background|Stroke|Foreground)="#' **/*.xaml`), not just text properties; a "0 hardcoded UI text" sweep does not cover `Ellipse.Fill`.

16. **Brand/ABI Boundary Audit (Pass-14 Confirmation)**: The pass-14 rebrand audit re-confirmed that the `LenovoLegionToolkit.*` ABI surface is an intentional cross-repo plugin load contract, NOT a stale leftover, and must NOT be renamed: assembly simple-names (`LenovoLegionToolkit.Lib` / `LenovoLegionToolkit.Lib.Plugins`), `<RootNamespace>`, C# namespaces, `.xaml` `clr-namespace` tokens, plugin `<AssemblyName>` prefixes (`LenovoLegionToolkit.Plugins.*`), the named-pipe contract (`LenovoLegionToolkit-IPC-0`), the trusted GitHub repository owner / legacy repo-id fallback in `UpdateChecker` / `PluginRepositoryService`, and line-1 upstream-sync copyright headers. After passes 4-5 migrated every user-facing surface (78+ `.resx` locales at 0 `LenovoLegionToolkit` hits, dual-name `MainAppBaseNames` smoke-test recognition of BOTH old + new window titles, `AppIdentity.LegacyDisplayName` intentional legacy constant, `ProcessAutoListener` / `FpsSensorController` self-window exclusion blacklists using legacy + new names for backward-compat fallback), the ONLY remaining auditable renamable brand surface was exactly one process-internal Win32 window-class `Caption` string in `NativeLayeredWindow.cs:118` (`LenovoLegionToolkit-NativeLayeredWindow` -> `UniversalDeviceToolkit-NativeLayeredWindow`; safe because the single subclass `NotificationAoTWindow` reuses the fixed caption for `RegisterClassEx` and no `FindWindow`/`FindWindowEx` call keys off the class name). Any future `rg LenovoLegionToolkit` sweep MUST exempt the documented ABI contract sites above, never attempt to rename them. See KB #12 (dual-name migration pairing) and UDT-010 archive entry.

### [2026-07-07] OSDev Tooling: Resource.Designer.cs must be manually updated for new .resx keys (UDT-009)

17. **Resource.Designer.cs Manual Sync (UDT-009)**: The UDT WPF project uses `PublicResXFileCodeGenerator` for `Resources/Resource.resx`, which generates `Resource.Designer.cs` as a committed file that does NOT auto-regenerate during `dotnet build`. When adding a new resource key to `Resource.resx`, the corresponding `public static string` property MUST be manually added to `Resource.Designer.cs` (following the alphabetical-order convention used by the generator: `/// <summary>` doc comment, `public static string <KeyName> { get { return ResourceManager.GetString("<KeyName>", resourceCulture); } }`). Without this manual sync, `dotnet build` succeeds (the .resx has the entry) but the WPF XAML compiler fails with `error MC3011: Cannot find the static member '<KeyName>' on type 'Resource'` because the `{x:Static}` markup extension resolves against the compiled Designer.cs class at XAML compile time. ALWAYS pair a new `.resx` `<data>` entry with a matching Designer.cs property for `dotnet build` to pass. Additionally: `apply_patch` strips one leading space from context lines (the first character is the format marker), so for XAML edits with deep indentation, prefer PowerShell string replacement to preserve exact whitespace.

### Entry: [UDT-022] CTS leak on Start/Stop re-entrancy in DriverKeyListener (2026-07-09)
- **Symptom / Pitfall**: Repeated StartAsync()/StopAsync() cycles on an IListener owning a CancellationTokenSource leaked the old CTS (and let a new one be created while the previous listen task was still in flight).
- **Root Cause**: StopAsync() referenced the nullable _cancellationTokenSource field directly (no local snapshot). Between the null-check and Dispose(), a concurrent pair could observe a partially-disposed CTS or orphan the old one.
- **Enforced Rule**: For any IListener/long-running background task owning a CancellationTokenSource, StopAsync MUST snapshot the CTS field into a local, null the field, then CancelAsync() + Dispose() on the local. StartAsync MUST reject re-entry when the listen task is still alive (if (_listenTask is not null) return Task.CompletedTask;).
- **Version**: .NET 10 / Windows x64, post-commit 1e0f3cda.

### [2026-07-09] ReflectionTypeLoadException in static constructors / ��̬���캯���е� ReflectionTypeLoadException
- **Symptom**: Calling Assembly.GetTypes() in a static constructor throws ReflectionTypeLoadException when any dependency type fails to load (e.g. missing plugin DLL). Since the exception fires inside a static constructor, it wraps as TypeInitializationException, making the entire converter class (and all JSON serialization of automation pipelines) permanently unusable for the process lifetime.
- **Root Cause**: Bare Assembly.GetTypes() returns ALL or NOTHING; no partial-load fallback. Six type-discovery sites in AutomationJsonConverters.cs originally used raw GetTypes().
- **Enforced Rule**: Always use the project's SafeGetTypes() extension (in AssemblyTypeLoaderExtensions.cs) which catches ReflectionTypeLoadException, logs each loader exception, and returns only the successfully loaded types via ex.Types.Where(t => t is not null).OfType<Type>().ToArray(). Never call bare Assembly.GetTypes() in static constructors or anywhere a type-load failure should not crash the host.
- **OS / .NET Version**: Windows 11, .NET 10, WPF

### [2026-07-09] Solution Any-CPU mapping gaps cause MSB4121 + duplicate build targets (UDT-030)
- **Symptom**: `dotnet build UniversalDeviceToolkit.sln` emitted MSB4121 warnings ("The project configuration ... is not mapped") for x64-only projects, and MSBuild ran duplicate task lines per project.
- **Root Cause**: The solution declared `Debug|Any CPU` / `Release|Any CPU` solution configurations, but several x64-only project GUIDs were missing the corresponding `ProjectConfigurationPlatforms` mappings AND had duplicate `Debug|x64`/`Release|x64` line pairs, so MSBuild fell back / emitted warnings and re-ran targets.
- **Enforced Rule**: For every project GUID in `UniversalDeviceToolkit.sln` (excluding pure Solution Folder virtual GUIDs), the `GlobalSection(ProjectConfigurationPlatforms)` block MUST contain exactly six mappings: `Debug|x64`, `Debug|x86`, `Debug|Any CPU`, `Release|x64`, `Release|x86`, `Release|Any CPU` (each with `ActiveCfg` + `Build.0`). There must be NO duplicate `GUID.Config|Platform.*` lines. When a project is x64-only, map `Any CPU` and `x86` to the `x64` config (e.g. `Debug|Any CPU.ActiveCfg = Debug|x64`). Verify with `Get-Content .sln | Group-Object | ? Count -gt 1` �� expect zero config-line duplicates.
- **OS / .NET Version**: Windows 11 24H2, .NET 10
### [2026-07-09] Synchronous disk I/O inside a process-wide lock blocks UI / unhandled-exception threads (BUG-2026-07-09-003)
- **Symptom**: Log.ErrorReport called File.AppendAllLines synchronously inside lock (_emergencyLock). The caller (often the WPF Dispatcher or an unhandled-exception handler) blocked on disk I/O, and ALL error reports serialized behind one global monitor �� degrading crash handling and freezing the UI during panic logging. Timestamp-only filenames (error_{UtcNow:...fff}.txt) also collided under concurrent reports.
- **Root Cause**: Synchronous file I/O executed on the caller's thread guarded by a process-wide monitor; no async offload, no uniqueness guard on filenames.
- **Enforced Rule**: Emergency/error-report write paths MUST NOT do synchronous disk I/O on the caller thread. Offload via Task.Run(...) fire-and-forget (or expose an *Async overload) and guard concurrency with an async-safe SemaphoreSlim(1,1) (WaitAsync/Release), never a lock. Crash-dump filenames MUST append a short unique suffix (e.g. 8-char GUID N) beyond the timestamp to avoid concurrent collisions. The write path MUST swallow exceptions �� a failed crash-dump write must never throw during unhandled-exception handling. The semaphore is disposed in Dispose.
- **OS / .NET Version**: Windows 11, .NET 10, WPF

### [2026-07-09] Split-path teardown causes SemaphoreSlim handle leak + double-dispose race (BUG-2026-07-09-004)
- **Symptom**: Log.Shutdown() / Log.ShutdownAsync() flipped _disposed (via Interlocked.CompareExchange) and disposed _logger but never disposed the SemaphoreSlim _emergencyLock. A subsequent Dispose() short-circuited via the same _disposed CAS guard, so the native semaphore handle was leaked for the process lifetime. With teardown logic duplicated across three entry points (Shutdown, ShutdownAsync, Dispose), a concurrent ShutdownAsync vs Dispose could also touch _logger a second time.
- **Root Cause**: Teardown was split across three entry points with duplicated disposal logic. _emergencyLock.Dispose() lived ONLY in Dispose, which was gated by the SAME _disposed flag that Shutdown had already set, making the lock's disposal unreachable after a Shutdown. Split-path disposal also let two paths unconditionally touch the same resources.
- **Enforced Rule**: For any IDisposable owning multiple disposable fields (logger + semaphore, etc.), ALWAYS centralize all teardown into a SINGLE private DisposeCore/DisposeCoreAsync invoked by every public teardown entry point (Shutdown, ShutdownAsync, Dispose). Guard entry with exactly ONE Interlocked.CompareExchange(ref _disposed, 1, 0) CAS; the winner captures each disposable field into a LOCAL and disposes each exactly once. Never place field disposal in a path that a sibling entry point can bypass via a shared disposed flag. Synchronous Shutdown delegates to the async core via ShutdownAsync().GetAwaiter().GetResult() (safe because the core offloads blocking work via Task.Run(...).ConfigureAwait(false), avoiding UI Dispatcher deadlock). Regression tests MUST use isolated instances (internal test ctor under UDT_TEST_HOOKS + UDT_APPDATA_OVERRIDE env var) �� never mutate the Log.Instance singleton, which is permanently disposed once Shutdown runs.
- **OS / .NET Version**: Windows 11 24H2, .NET 10, WPF