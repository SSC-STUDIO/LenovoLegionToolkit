# Potential Issues Audit / 潜在问题检查报告

Date / 日期: 2026-02-15

## Scope / 范围

- Static code inspection only (no local build), because `.NET SDK` is unavailable in this environment.
- Command evidence:
  - `dotnet build LenovoLegionToolkit.sln --configuration Debug` → `bash: command not found: dotnet`
  - `rg "Task.Delay\(" ...`
  - `rg "PluginStateChanged" -n LenovoLegionToolkit.WPF/Pages/PluginExtensionsPage.xaml.cs`

## Findings / 发现

### 1) Plugin update background interval is hardcoded to 24h

**Risk / 风险**
- The setting `PluginUpdateCheckFrequencyHours` can be configured, but background checks always wait 24 hours. This may cause user-selected shorter intervals to be ignored.
- `PluginUpdateCheckFrequencyHours` also has no bounds validation in settings.

**Evidence / 证据**
- `PluginUpdateManager.StartBackgroundCheck` uses `Task.Delay(TimeSpan.FromHours(24), ...)`.
- `ShouldCheckForUpdates()` compares elapsed time with `store.PluginUpdateCheckFrequencyHours`.
- `ApplicationSettingsStore.PluginUpdateCheckFrequencyHours` exists as a mutable int property.

**Suggestion / 建议**
- Replace fixed 24h delay with a delay derived from settings (with sane min/max bounds).
- Validate settings value on load (e.g., clamp to `1..168`).

### 2) Event subscription may leak page instances

**Risk / 风险**
- `PluginExtensionsPage` subscribes to `_pluginManager.PluginStateChanged` in constructor, but no matching unsubscribe was found.
- If page instances are recreated, stale handlers can accumulate and increase memory/event traffic.

**Evidence / 证据**
- Subscription found in constructor.
- No `-=` unsubscribe for this event found in the same file.

**Suggestion / 建议**
- Unsubscribe on `Unloaded` or implement disposable lifecycle handling for the page.

### 3) Large commented-out code blocks in plugin page

**Risk / 风险**
- Long commented code paths (import workflow) increase maintenance cost and make active behavior harder to understand.

**Evidence / 证据**
- Multiple large sections in `PluginExtensionsPage.xaml.cs` are commented out (`ImportPluginButton_Click`, library import flow, etc.).

**Suggestion / 建议**
- Move disabled functionality to feature branches/history or guarded feature flags.
- Keep production files focused on active code paths.

### 4) Delay abstraction (`IDelayProvider`) is not consistently adopted in library code

**Risk / 风险**
- The repo introduced `IDelayProvider` for testability, but many library paths still use direct `Task.Delay`, which can make tests slower/flakier.

**Evidence / 证据**
- `IDelayProvider`/`DelayProvider` exists.
- Multiple direct `Task.Delay` usages remain in `LenovoLegionToolkit.Lib` (integration/controllers/features/plugins/utils).

**Suggestion / 建议**
- Gradually inject `IDelayProvider` where timing-sensitive behavior is tested.
- Prioritize hot paths and retry/background loops first.

## Recommended Next Actions / 建议后续动作

1. Fix background update cadence to respect configured frequency.
2. Add event unsubscription in `PluginExtensionsPage` lifecycle.
3. Remove or refactor commented legacy blocks in plugin page.
4. Create a migration checklist for `Task.Delay` → `IDelayProvider` in test-critical lib components.
