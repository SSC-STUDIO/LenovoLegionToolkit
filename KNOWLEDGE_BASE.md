# Knowledge Base / 知识库

This is a living ledger of lessons learned during development and maintenance of Universal Device Toolkit.
Agents and human maintainers MUST append new entries when solving non-trivial bugs, discovering OS quirks, or optimizing architecture.

---

## Entry Template

```markdown
### [YYYY-MM-DD] Topic / 主题
- **Symptom / 症状**: ...
- **Root Cause / 根因**: ...
- **Enforced Rule / 强制规则**: ...
- **OS / .NET Version / OS及.NET版本**: ...
```

---

### [2026-07-06] CardHeaderControl subtitle text overflow / 卡片副标题文本溢出
- **Symptom / 症状**: Long Chinese translation strings caused CardControl subtitles to overflow their containers, bloating card heights and reducing UI aesthetics. Cards like "System Optimization", "Plugin Extensions", etc. showed truncated or overlapping text.
- **Root Cause / 根因**: `CardHeaderControl._subtitleTextBlock` had `TextWrapping="Wrap"` and `TextTrimming="CharacterEllipsis"` but no `MaxHeight` constraint. Chinese translations for many `Message`-suffixed resource keys were 50-80+ characters, causing 3-5 lines of wrapped text that pushed card layouts.
- **Fix / 修复**: (1) Added `MaxHeight = 60` (≈3 lines) to `_subtitleTextBlock` in `CardHeaderControl.cs`. (2) Shortened 10+ long Chinese strings in `Resource.zh-hans.resx` to ≤40 chars, using `\n` line breaks for necessary multi-line content.
- **Enforced Rule / 强制规则**: All Chinese (zh-Hans) resource values in `Resource.zh-hans.resx` MUST be ≤50 characters per line. Use `\n` for intentional line breaks. `CardHeaderControl` enforces `MaxHeight=60` on subtitle; do NOT remove this constraint.
- **OS / .NET Version / OS及.NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] Language selector and main window popup simultaneously / 语言选择与主窗口同时弹出
- **Symptom / 症状**: On first launch (or when language selection is needed), both the language selector window AND the main window appeared simultaneously. The main window behind the language selector created visual confusion.
- **Root Cause / 根因**: In `LocalizationHelper.cs`, `window.Show()` was used to display the `LanguageSelectorWindow`. `Show()` is non-modal — it returns immediately, so the startup flow continued and opened the main window behind the language selector.
- **Fix / 修复**: Changed `window.Show()` to `window.ShowDialog()` at line 153 of `LocalizationHelper.cs`. `ShowDialog()` is modal — it blocks until the user finishes language selection, then the startup flow continues and opens the main window.
- **Enforced Rule / 强制规则**: Language selector and any "must-complete-first" dialogs during app startup MUST use `ShowDialog()`, never `Show()`. The startup orchestrator (`StartupOrchestrator`) depends on synchronous completion of language selection.
- **OS / .NET Version / OS及.NET版本**: Windows 11, .NET 10, WPF

---

### [2026-07-06] Chinese translation string optimization for card layouts / 中文翻译字符串针对卡片布局的优化
- **Symptom / 症状**: Multiple settings cards and automation step controls had Chinese subtitles that were too long, causing card height bloat and text truncation with ellipsis that didn't convey full meaning.
- **Root Cause / 根因**: Initial machine translation from English to Chinese produced verbose strings. Chinese characters are denser than Latin characters in vertical space consumption when wrapped, but the original translations didn't account for WPF `TextBlock` wrapping behavior.
- **Fix / 修复**: Systematically shortened Chinese translations in `Resource.zh-hans.resx` for ~15 keys. Applied these principles:
  - Titles: ≤12 characters
  - Subtitle/Message: ≤40 characters per logical line, use `\n` for breaks
  - Action labels: ≤8 characters
  - Error messages: ≤30 characters
- **Enforced Rule / 强制规则**: When adding or modifying Chinese localization strings, always validate visual length in the actual WPF card layout. Use the 40-char rule for subtitles. Reference the English base (`Resource.resx`) but adapt for Chinese linguistic density.
- **OS / .NET Version / OS及.NET版本**: Windows 11, .NET 10, WPF

---

### [2026-07-06] WMI query deadlock protection in async methods / 异步方法中WMI查询死锁保护
- **Symptom / 症状**: (From prior sessions) WMI queries in `AbstractWmiFeature` and related classes could deadlock the UI thread when `ConfigureAwait(true)` was used or when synchronous WMI calls were made on the UI thread.
- **Root Cause / 根因**: WMI queries are inherently synchronous and blocking. When called from the UI thread without proper async wrapping or timeouts, they cause deadlocks. The original code had a mix of `.ConfigureAwait(true)` and `.ConfigureAwait(false)` usage.
- **Fix / 修复**: Applied `.ConfigureAwait(false)` to all `await` calls in Lib (12 places), WPF Controls (200+), Pages (75), Windows (120), Utils/Extensions/ViewModels (64). Wrapped WMI calls in `Task.Run()` with `CancellationToken` support and 2500-3000ms timeout.
- **Enforced Rule / 强制规则**: ALL WMI queries MUST be wrapped in async methods with `Task.Run()` and a `CancellationToken` with 3000ms maximum timeout. NEVER call WMI synchronously on the UI thread. ALL `await` calls in Lib/SDK code MUST use `.ConfigureAwait(false)`.
- **OS / .NET Version / OS及.NET版本**: Windows 11 24H2, .NET 10

---

### [2026-07-06] Memory leak pattern: WPF controls not unsubscribing from singleton events / 内存泄漏模式：WPF控件未取消单例事件订阅
- **Symptom / 症状**: (From prior sessions) Navigating between pages caused memory to grow unbounded. Memory Analyzer showed multiple instances of controls like `PowerModeControl`, `SensorsControl`, `DiscreteGPUControl` remaining in memory after navigation away.
- **Root Cause / 根因**: Controls subscribed to `MessagingCenter`, `AbstractSettings.Changed`, `Listener.Changed` (registry/WM! watchers) in their constructor or `OnInitialized` but never unsubscribed. When the page was navigated away from, the controls were still referenced by the singleton event source, preventing garbage collection.
- **Fix / 修复**: Added `Unloaded` event handlers to 30+ controls/pages. In the `Unloaded` handler, explicitly unsubscribe from all singleton events. Implemented `IDisposable` on `SensorsControl`, `PowerModeControl`, `PackageControl`, etc. to dispose `CancellationTokenSource`, `ThrottleLastDispatcher`, and `Process` fields.
- **Enforced Rule / 强制规则**: Every WPF control/paje that subscribes to a singleton event (`MessagingCenter`, `AbstractSettings.Changed`, registry listeners, `IRefreshable.Refreshed`) MUST unsubscribe in an `Unloaded` handler. Controls that own `CancellationTokenSource`, timers, or `Process` objects MUST implement `IDisposable` and clean up in `Unloaded`.
- **OS / .NET Version / OS及.NET版本**: Windows 11, .NET 10, WPF

---

## Adopted Engineering Principles / 已采纳的工程原则

1. **WPF Thread Safety**: Never use `.ConfigureAwait(true)` in Lib/SDK code. UI updates must go through `Dispatcher.InvokeAsync()`. (`CardHeaderControl` is already correct.)
2. **WMI Timeout Protection**: All WMI/process execution must have 2500-3000ms timeout with `CancellationToken`.
3. **Zero-Spam Polling**: High-frequency monitoring (500-2000ms) must NOT serialize JSON or write trace logs on every tick.
4. **Modular UI**: All XAML must use rounded cards (`CornerRadius="8"`), responsive layouts (`Grid` star-sizing, `WrapPanel`), and 100% host theme brush binding — never hardcoded hex colors.
5. **Chinese Localization**: Subtitle strings ≤40 chars. Use `\n` for line breaks. Validate in actual card layout.
6. **Startup Modal Dialogs**: Language selector and similar first-run dialogs must use `ShowDialog()`, not `Show()`.
7. **Memory Leak Prevention**: Unsubscribe singleton events in `Unloaded`. Dispose `IDisposable` resources. Null out event handlers in `OnDestroy`/`Unloaded`.
