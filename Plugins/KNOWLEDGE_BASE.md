# Knowledge Base — Lessons Learned & Rules (经验知识与规则累积)

This file is the **Living Knowledge Ledger** for the Universal Device Toolkit Plugins project. Every time an AI agent or human developer solves a complex bug, discovers a Windows OS quirk, or optimizes an architecture, they MUST append a structured entry here.

## Current baseline (keep in sync)

| Item | Value |
|------|--------|
| Host app | Universal Device Toolkit **v5.0.0** (`Plugins/HostBaseline/host-release.json`) |
| Official store plugins | `custom-mouse` 1.0.17 · `vive-tool` 1.2.3 · `shell-integration` 1.0.13 |
| Tooling entry | `udt-plugin.cmd` (`llt-plugin.cmd` alias) |
| Min host field | `plugin.manifest.json` → `minHostVersion` = **5.0.0**; runtime `plugin.json` keeps ABI property name `MinLltVersion` with the same value |
| Version SoT | each `plugin.manifest.json` → `version` |

---

## 📚 Format Template

```markdown
### [YYYY-MM-DD] <Brief Description>
- **Symptom / Pitfall**: What failed?
- **Root Cause**: Why did it fail?
- **Enforced Rule**: What mandatory constraint prevents recursion?
- **.NET/OS Version**: Under what environment was this learned?
```

---

### [2026-07-18] Docs / minHostVersion alignment with host v5.0.0
- **Symptom / Pitfall**: README catalog versions lagged manifests; CONTRIBUTING had mojibake; docs still said host 4.2.1 while `host-release.json` and Phase 3 ABI are **5.0.0**; CLI still branded `llt-plugin.cmd` only.
- **Root Cause**: Sprint/promo docs and partial rebrand left multiple sources of truth; `[Plugin] MinimumHostVersion` never tracked host bumps after 3.6.x.
- **Enforced Rule**:
  - Product facts live in README + `plugin.manifest.json` + `host-release.json`
  - After host major ABI bumps, sync `minHostVersion` / `MinLltVersion` / `MinimumHostVersion` together
  - Prefer `udt-plugin.cmd` in new docs; keep `llt-plugin.cmd` as alias only
  - Product facts live in README + manifests; do not invent parallel version tables in ad-hoc notes
- **.NET/OS Version**: .NET 10, host UDT 5.0.0

### [2026-07-18] Removed migrated plugin sources (BatteryHealth / NetworkAcceleration)
- **Symptom / Pitfall**: Repo still carried full plugin projects after features shipped in the host, plus campaign handovers, bak files, and accidental `Plugins/*/*.dll` build outputs.
- **Root Cause**: “Keep source for migration” outlived the migration need; promotional and day-audit artifacts were never pruned.
- **Enforced Rule**:
  - Official plugin set is only `CustomMouse`, `ShellIntegration`, `ViveTool` (+ `Shared` / SDK / Tools)
  - Do not re-add `battery-health` / `network-acceleration` plugin projects; host owns those features
  - Do not commit `Plugins/<Name>/*.dll` next to sources (`Bundled/` is the only intentional binary drop-in)
  - Prefer GitHub Issues over growing root `BUGS.md` day dumps
- **.NET/OS Version**: .NET 10, host UDT 5.0.0

### [2026-07-06] Zero-Warnings Build Achievement
- **Symptom / Pitfall**: Multiple Roslyn analyzer warnings (CA1062, CA2024, CS1591) accumulating in build output
- **Root Cause**: 
  - CA1062: Missing null validation in public methods
  - CA2024: `EndOfStream` used in async context (should use `ReadLineAsync`)
  - CS1591: Missing XML documentation for public APIs
- **Enforced Rule**: 
  - Added `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to `Directory.Build.props`
  - Added `<WarningsAsErrors>CS1591;CA1062;CA2024</WarningsAsErrors>`
  - All public methods MUST have `ArgumentNullException.ThrowIfNull()` 
  - All async methods MUST NOT use `EndOfStream`
  - All public APIs MUST have XML documentation (`/// <summary>`)
- **.NET/OS Version**: .NET 10, Windows 11 24H2

---

### [2026-07-06] Version Mismatch Between plugin.json and plugin.manifest.json
- **Symptom / Pitfall**: CI validation failure: `plugin.json version does not match plugin.manifest.json version`
- **Root Cause**: `plugin.json` had version `1.1.9` while `plugin.manifest.json` had `1.2.0`. Manual version updates cause drift.
- **Enforced Rule**: 
  - `plugin.manifest.json` is the SINGLE SOURCE OF TRUTH for version
  - `plugin.json` MUST be auto-generated from `plugin.manifest.json` (or strictly synced)
  - `.csproj` `<Version>` / `<FileVersion>` / `<AssemblyVersion>` MUST match `plugin.manifest.json` version
  - `[Plugin(... version: "...")]` in `*Plugin.cs` MUST match `plugin.manifest.json` version
  - Use `.\udt-plugin.cmd bump-version --plugin <id> --part patch|minor|major` to bump, or `sync-version` to propagate without bumping
  - `promote` only prepares store metadata (`store-entry.json`); it does **not** bump versions
- **.NET/OS Version**: .NET 10, Windows 11 24H2

---

### [2026-07-06] FeatureMerger.cs Missing using System
- **Symptom / Pitfall**: Compilation error: `CS0103: The name 'ArgumentNullException' does not exist in the current context`
- **Root Cause**: Added `ArgumentNullException.ThrowIfNull()` calls but forgot to add `using System;` to the file
- **Enforced Rule**: 
  - ALL `.cs` files MUST have explicit `using System;` if they use `ArgumentNullException`, `NullReferenceException`, etc.
  - When adding null validation to a file, ALWAYS check that `using System;` is present
  - Prefer full-qualified names (`System.ArgumentNullException.ThrowIfNull()`) if modifying legacy code without `using System;`
- **.NET/OS Version**: .NET 10, C# 13 (preview)

---

### [2026-07-05] WPF Fallback UI Hardcoded Brushes
- **Symptom / Pitfall**: Fallback UI (when WPF fails to load XAML) shows white background in Dark theme
- **Root Cause**: `WpfFallbackHelper.cs` used hardcoded `Brushes.White`, `Brushes.Black`, `Brushes.Gray`
- **Enforced Rule**: 
  - ALL fallback UI MUST use `DynamicResource` or theme-aware brush resolution
  - Created `ResolveFallbackBrush(string lightBrush, string darkBrush)` method
  - Never use `Brushes.*` directly in fallback UI code
  - Test fallback UI in BOTH Light and Dark themes
- **.NET/OS Version**: .NET 10, Windows 11 24H2, WPF UI 4.3.0

---

### [2026-07-05] ProcessRunner.PumpAsync CA2024 Warning
- **Symptom / Pitfall**: `CA2024: Use 'ReadLineAsync' instead of 'EndOfStream' in async method`
- **Root Cause**: `StreamReader.EndOfStream` is a synchronous property that blocks in async context
- **Enforced Rule**: 
  - In async methods, NEVER use `EndOfStream`
  - Use `ReadLineAsync()` in a loop until it returns `null`
  - Example fix:
    ```csharp
    string? line;
    while ((line = await streamReader.ReadLineAsync()) != null)
    {
        // Process line
    }
    ```
- **.NET/OS Version**: .NET 10, C# 13 (preview)

---

## 🔧 General Engineering Rules (工程规则)

### WPF Thread Safety
- NEVER use `ConfigureAwait(false)` in WPF projects — it strips the UI synchronization context
- Always use `Dispatcher.CheckAccess()` and `Dispatcher.InvokeAsync()` for background-triggered UI updates

### WMI & Remote Desktop Deadlock Protection
- ALL WMI queries MUST use async wrappers with 2,500ms–3,000ms timeouts
- ALL administrative process executions (`netsh`, `sc`) MUST use `CancellationToken`
- Test WMI queries under Remote Desktop sessions (they behave differently!)

### Zero-Spam Polling & I/O Efficiency
- High-frequency monitoring loops (500ms–2000ms) MUST NOT serialize JSON on every tick
- Use in-memory caching and only write to disk every 5–10 seconds
- NEVER use `Debug.WriteLine()` in production polling loops (it kills performance)

### Modular UI & Design Token Binding
- ALL UI elements MUST use `CornerRadius="8"` or `CornerRadius="10"` (rounded cards)
- ALL colors MUST use `DynamicResource` (e.g., `{DynamicResource ControlFillColorDefaultBrush}`)
- NEVER write monolithic 600-line StackPanels — use `Grid` star-sizing and `WrapPanel`
- ALL text MUST use `TextWrapping="Wrap"` and `TextTrimming="CharacterEllipsis"`

---

## 🌍 Localization Rules (本地化规则)

### Resource File Conventions
- ALL user-facing strings MUST be in `Resource.resx` (never hardcode English text)
- Use numbered placeholders (`{0}`, `{1}`) — NEVER string concatenation
- Generate satellite assemblies for all 34 languages in `<PluginSatelliteResourceLanguages>`

### OCR Verification (5-Dimension Check)
When evaluating UI via FlaUI + WinRT OCR:
1. **Untranslated Detection**: Flag English text in non-English locales
2. **Mojibake & Encoding Corruption**: Flag corrupted UTF-8/16 characters
3. **Broken Placeholders**: Flag unreplaced `{0}` or raw binding tags
4. **Layout Truncation & Box Overflow**: Compare OCR text width vs. container width
5. **Technical Domain Semantics**: Verify accurate hardware terminology

---

## 📊 CI/CD Rules (持续集成规则)

### Multi-Plugin Validation
- ALL plugins MUST pass `.\udt-plugin.cmd validate --profile contributor`
- Version consistency check: `plugin.json` = `plugin.manifest.json` = `.csproj <Version>`
- Build output MUST have 0 warnings, 0 errors (enforced via `TreatWarningsAsErrors`)

### Store Metadata Strict Mode
- `store-entry.json` MUST byte-match the `store` block of the sibling `plugin.manifest.json` (promote/generate-sync keeps them in lockstep).
- NEVER regenerate the generated `Plugins/.build/catalog/plugin-catalog.json` wholesale via `generate-store` when some plugins lack a published release ZIP: it zeroes `fileSize`/`hash` for those plugins (silent regression). Merge the `store-entry.json` of a new plugin incrementally instead.
- Release assets are the single source for `fileSize`/`hash`; only present `release-assets/<id>-v<ver>.zip` entries may carry nonzero sizes.

### Release Automation
- Use `.\udt-plugin.cmd promote` to generate `store-entry.json`
- Release ZIP naming convention: `<plugin-id>-v<version>.zip`
- Tag format: `v<version>-<plugin-id>` (e.g., `v1.2.0-network-acceleration`)

---

## Cross-Repository Naming & ABI Compatibility (LenovoLegionToolkit -> UniversalDeviceToolkit)

- **Timestamp / Version**: 2026-07-14 Phase 3 hard cutover, .NET 10, host vendored as UniversalDeviceToolkit.Lib v5.0.0
- **History**: Prior to Phase 3, internal compile identifiers stayed `LenovoLegionToolkit.*` for host ABI (BUGS.md M-010). Host and plugins now share `UniversalDeviceToolkit.*` assembly/namespace identity.
- **Enforced Rule (Phase 3)**: Primary code, assemblies, namespaces, `x:Class`, manifest `class`, solution/csproj names, and SDK/Shared outputs use `UniversalDeviceToolkit.*`. Brand user-visible text as "Universal Device Toolkit".
- **Legacy path retention (do NOT "fix")**: `SettingsManager<T>`, ShellIntegration profile/lang migration still **read** pre-rebrand data under `%LocalAppData%\LenovoLegionToolkit\...` and migrate into `%LocalAppData%\UniversalDeviceToolkit\...`. Those path segment strings are intentional migration sources.
- **Evidence (Phase 3)**: Shared + SDK + BatteryHealth Release build 0/0; Shared.Tests 210/210 pass; outputs `UniversalDeviceToolkit.Plugins.SDK.dll` / `UniversalDeviceToolkit.Plugins.Shared.dll`.

## Cross-Repo Naming Audit Re-confirmation (2026-07-14, Phase 3 Hard Cutover)
- Main repo `UniversalDeviceToolkit.Lib` / `UniversalDeviceToolkit.Lib.Plugins` ship AssemblyName/RootNamespace `UniversalDeviceToolkit.Lib` / `UniversalDeviceToolkit.Lib.Plugins`. Plugin repo compile identifiers match. M-010 ABI rename gate is closed for new builds (old LLT-named plugin ZIPs require host dual-load or recompile).

## WPF UI Thread-Safety - Await Scheduler Hints in Code-Behind (PLG-001)
- **Timestamp / Version**: 2026-07-06, .NET 10, WPF plugin UI
- **Symptom / Pitfall**: `ViveToolPage.xaml.cs` awaited service calls with `.ConfigureAwait(false)` (15 sites), stripping the captured `DispatcherSynchronizationContext`. No live race existed (the page marshals UI touches via `await Dispatcher.InvokeAsync(...)`), but drifting from the repo thread-safety contract risked regressions.
- **Root Cause**: UI code-behind copied a Lib/SDK-style await hint. `.ConfigureAwait(false)` is only safe in non-UI/library code where no `SynchronizationContext` capture is wanted.
- **Enforced Rule**: Zero `.ConfigureAwait(false)` in `**/*.xaml.cs`. UI code-behind relies on the captured `DispatcherSynchronizationContext` (omit the hint, or use `.ConfigureAwait(true)` for an explicit guarantee). Only Lib/SDK/non-UI code may use `.ConfigureAwait(false)`. Enforce via a CI grep gate (`rg -g '*.xaml.cs' 'ConfigureAwait\(false\)'` => 0).
- **Evidence**: PLG-001 remediated in working tree (concurrent editor removed the 15 offending sites; sole remaining await hint is `ConfigureAwait(true)` at L829, UI-safe); 0 `ConfigureAwait(false)` across all plugin `*.xaml.cs`; full solution build 0 warnings/0 errors; 409/409 tests green.

## xUnit Parallelism Race on Static Resource Culture (PLG-002)
- **Timestamp / Version**: 2026-07-06, .NET 10, xUnit, Windows 11 24H2
- **Symptom / Pitfall**: `BatteryHealthPluginTests.Plugin_HasExpectedMetadata` failed intermittently with `Expected: 电池健康` vs `Actual: Battery Health` only under the full-solution parallel test run; the plugin tests passed in isolation (16/16).
- **Root Cause**: `LocalizedTextTestsBase.TextClass_HasNoHardcodedChinese` mutates the process-wide static `Resources.Resource.Culture` to `en` via reflection (restored in `finally`). xUnit runs each plugin's `*TextTests` concurrently with its `*PluginTests`. The metadata assertion reads the culture-dependent `plugin.Name` and `XText.PluginName` in two separate operations; a parallel text test flips the static from `null` to `en` between the reads, so the two reads disagree. Latent in CustomMouse / NetworkAcceleration / ShellIntegration (same tautological two-read `Assert.Equal` pattern); ViveTool immune (uses `IsNullOrWhiteSpace` + already serialises its test collection).
- **Enforced Rule**: Never assert culture-dependent properties with a tautological two-read pattern while a sibling test mutates the shared static culture. Pin test classes that share a mutable static into one `[CollectionDefinition("<Name>", DisableParallelization = true)]` and tag both the writer (TextTests) and reader (PluginTests) classes with `[Collection("<Name>")]`. Mirror per plugin whenever a `*TextTests` base mutates a process-wide static.

## WPF UI Governance - Inline Status, No Modal Dialogs (PLG-003)
- **Timestamp / Version**: 2026-07-06, .NET 10, WPF plugin UI, Windows 11 24H2
- **Symptom / Pitfall**: Plugin settings pages (`ViveToolSettingsPage`, `ShellIntegrationStyleSettingsWindow`) surfaced errors/confirmations via synchronous modal `MessageBox.Show` (6 sites in ViveTool, 1 in ShellIntegration). Modal dialogs block the host WPF message pump, steal focus from the host window, and break the embedded-plugin non-blocking UX contract.
- **Root Cause**: Plugin code-behind copied a WinForms-style modal-feedback pattern. The repo UI-governance rule demands inline, non-blocking status feedback inside a hosted plugin surface (no modal focus steal).
- **Enforced Rule**: Never use `MessageBox.Show` in plugin UI code-behind. Surface status inline via a status `TextBlock` + a `SetStatus(string, bool)`/`ShowInlineStatus` helper, themed via `DynamicResource` foreground by error/success state (no modal focus steal). `WpfHostNotifications.cs` is the sole allowed modal site (host-level fallback notifications). Enforce via a CI grep gate: `rg -g '*.cs' 'MessageBox\.Show' Plugins` => only `WpfHostNotifications.cs`.
- **Evidence**: 6 `MessageBox.Show` sites in `ViveToolSettingsPage.xaml.cs` replaced with `_statusTextBlock` + `SetStatus(text, isError)` (themed foreground, `SymbolIcon` glyph, `AutomationProperties.AutomationId = "ViveToolSettingsStatusText"`); 1 site in `ShellIntegrationStyleSettingsWindow.cs:135` replaced with inline `_statusTextBlock` + status method (`AutomationId = "ShellIntegrationStyleSettingsStatusText"`). `rg -n 'MessageBox\.Show' Plugins -g '*.cs'` => 0 in plugin code-behind (2 in `WpfHostNotifications.cs` host shim, allowed); full solution build 0 warnings/0 errors; 409/409 tests green (CustomMouse 54, ShellIntegration 114, BatteryHealth 16, NetworkAcceleration 39, ViveTool 186).

## WMI Timeout Decoration - CancelAfter Cannot Bound Native COM (PLG-004)
- **Timestamp / Version**: 2026-07-06, .NET 10, WPF plugin runtime, Windows 11 24H2
- **Symptom / Pitfall**: `BatteryHealthService.QueryFirst` enumerated `ManagementObjectSearcher.Get()` synchronously. The outer `GetBatteryHealthReportAsync` wrapped the body in `Task.Run` + `cts.CancelAfter(WmiTimeoutMs=3000)` and `QueryFirst` checked `cancellationToken.ThrowIfCancellationRequested()` per row, but `ManagementObjectSearcher.Get()` is a blocking native COM enumeration that does NOT poll the CancellationToken. The per-row guard only runs AFTER a row is returned, so a hung ACPI/WMI provider pinned the thread-pool task (and the await of the caller) indefinitely past the 3,000ms contract.
- **Root Cause**: `cts.CancelAfter` is purely decorative for a hung native COM call once the delegate is already running; it cannot interrupt the blocking enumeration. The existing rule "ALL WMI queries MUST use async wrappers with 2,500ms-3,000ms timeouts" was satisfied nominally but NOT enforced as a HARD deadline - cancellation is cooperative, native COM does not cooperate.
- **Enforced Rule**: For WMI queries behind a cancellation budget, do NOT rely on `CancelAfter` alone. Race the blocking `ManagementObjectSearcher.Get()` enumeration off-thread (`Task.Run`) against a hard deadline and ABANDON the task if it does not complete: `Task.Wait(TimeSpan.FromMilliseconds(timeoutMs), cts.Token)` returning false -> `cts.Cancel()` + throw `TimeoutException`. This matches the host-side `ManagementObjectSearcherExtensions.GetAsync` (`Task.WhenAny(task, Task.Delay(timeoutMs))` + abandon). When the enumeration faults inside the bounded task, `Task.Wait` throws `AggregateException` -> unwrap (`ae.InnerExceptions.Count == 1 ? ae.InnerExceptions[0] : ae`) to rethrow the original `ManagementException` / `COMException` / `OperationCanceledException` so the typed catch blocks of the caller still match. Mirror this abandon pattern in any plugin WMI site that currently relies on `CancelAfter` for a hung-provider deadline.
- **Evidence**: PLG-004 remediated in working tree; `dotnet build UniversalDeviceToolkit.Plugins.sln -c Release` => 0 warnings / 0 errors; `dotnet test Plugins\BatteryHealth.Tests --no-build` => 16/16 pass. Per-query hard timeout `WmiQueryTimeoutMs = 2500` added (outer `WmiTimeoutMs = 3000` retained as the total budget ceiling).

---

## Brand Generalization Rule - User-Visible Text Must Not Pin to Lenovo Legion
- **Timestamp / Version**: 2026-07-14 (Phase 3), .NET 10
- **Enforced Rule**: (1) User-visible product framing MUST use "Universal Device Toolkit" / "通用设备工具包" / universal OEM scope — never Lenovo-Legion-exclusive positioning. (2) Community: `r/Lenovo` is fine; `r/LenovoLegion` is not. Competitor comparisons (e.g. "Lenovo Vantage") and historical GitHub topics may remain as factual references. (3) Compile identifiers (namespaces, assemblies, solution/csproj, manifest `class`) are now `UniversalDeviceToolkit.*` after Phase 3 hard cutover.

## Cross-Repo Data-Root Alignment - Dual-Name Migration Contract
- **Timestamp / Version**: 2026-07-14, .NET 10
- **Symptom / Pitfall**: After LenovoLegionToolkit → UniversalDeviceToolkit rebrand, a contributor might "fix" hard-coded legacy path segments, breaking migration for pre-rebrand users.
- **Root Cause**: Both repos preserve the OLD name `LenovoLegionToolkit` DELIBERATELY as a forward-migration *source*, while writing NEW data under `UniversalDeviceToolkit`.
- **Enforced Rule**: (1) Active write roots MUST use `UniversalDeviceToolkit` (`%LocalAppData%\UniversalDeviceToolkit\plugins\<id>`). (2) `LenovoLegionToolkit` path segments are read-ONLY legacy-migration sources — DO NOT delete or rename them. (3) Prefer host `AppIdentity.CompactName`/`LegacyCompactName` over hard-coded literals where available.
- **Evidence**: Plugin `SettingsManager<T>` writes under `UniversalDeviceToolkit\plugins\`; retains `LenovoLegionToolkit\plugins` as legacy migration read source.

---

**This file is alive. Every bug fix or architecture optimization MUST be recorded here.** 📝

---

### [2026-07-07] Host v4.2.1 Serilog Transitive Dependency
- **Symptom / Pitfall**: After upgrading vendored host DLLs from v3.6.14 to v4.2.1, plugin tests fail with `System.IO.FileNotFoundException: Could not load file or assembly 'Serilog, Version=4.3.0.0'`. 15/409 tests fail (14 in ViveTool, 1 in NetworkAcceleration). PluginLoader-based workbench fails to load any plugin with `InvalidOperationException` at `PluginWorkbenchSession.cs:155`.
- **Root Cause**: v4.2.1 `UniversalDeviceToolkit.Lib.dll` uses Serilog 4.3.0 in `Log..ctor()` (logging initialization), which is a new transitive dependency not present in v3.6.14. The plugin repo's `Dependencies\Host\` only vendored the 3 host DLLs (Lib, Lib.Plugins, WPF) but not their transitive dependencies.
- **Enforced Rule**:
  - `Dependencies\Host\` MUST contain all transitive dependencies of the vendored host DLLs, not just the top-level assemblies
  - When syncing host DLLs from the main repo's `Build\` directory, copy ALL `*.dll` files (or at minimum, the transitive deps reachable via `Assembly.GetReferencedAssemblies()`)
  - `ensure-host-dependencies.ps1` and `refresh-host-references.ps1` `$requiredFiles` lists MUST be kept in sync with actual host transitive deps
  - `host-release.json` MUST document `transitiveDependencies` so the next sync doesn't drop them again
  - `Directory.Build.targets` MUST copy host deps (including transitive) to test/tool output via a dedicated `CopyHostDependenciesToOutput` target (plugin projects use `Private=false` references and should NOT include them in plugin ZIPs)
- **Detection Method**: After any host DLL version bump, run `dotnet test` and `PluginWorkbench.Smoke` for at least 1 plugin — Serilog/init failures surface immediately as `FileNotFoundException` in the test host process.
- **.NET/OS Version**: .NET 10, Windows 11 24H2, Serilog 4.3.0 + Sinks.Async 2.1.0 + Sinks.File 7.0.0

---

### [2026-07-07] CS0104 Ambiguity When Adding Lib.Plugins Reference
- **Symptom / Pitfall**: After adding `<Reference Include="UniversalDeviceToolkit.Lib.Plugins">` to `Tools\PluginWorkbench\PluginWorkbench.csproj`, build fails with 2 CS0104 errors: `PluginHostMode` is ambiguous between `UniversalDeviceToolkit.Lib.Plugins.PluginHostMode` and `UniversalDeviceToolkit.Plugins.SDK.PluginHostMode`. After adding the alias, 3 more CS0104 errors for `PluginHostContext` (static class).
- **Root Cause**: The host Lib.Plugins DLL and the SDK both define the same types (intentional type forwarding / dual-surface design). The `using UniversalDeviceToolkit.Lib.Plugins;` import was already present but harmless (the assembly was not referenced, so the types were invisible). Once Lib.Plugins.dll v4.2.1 was referenced, both namespaces became visible and C# disambiguation failed.
- **Enforced Rule**:
  - When adding the `UniversalDeviceToolkit.Lib.Plugins` reference to any project that also imports `UniversalDeviceToolkit.Plugins.SDK`, add using aliases for ALL shared types: `using PluginHostMode = UniversalDeviceToolkit.Plugins.SDK.PluginHostMode;` and `using PluginHostContext = UniversalDeviceToolkit.Plugins.SDK.PluginHostContext;`
  - The SDK types are the correct consumer-facing types: SDK `PluginHostContext.Current` has a public setter (for `PluginHostContext.Current = _hostContext` assignment), Lib `PluginHostContext.Current` is read-only (must use `SetCurrent()` method instead) — code that does property assignment only compiles against SDK
  - The intersection of types in both namespaces is: `IAppStartupPlugin`, `IOptimizationCategoryProvider`, `IPluginHostContext`, `IPluginPage`, `PluginHostContext`, `PluginHostMode` — audit any new `using` block that imports both namespaces and alias all 6 if used unqualified
- **.NET/OS Version**: .NET 10, UniversalDeviceToolkit host v4.2.1

---

### [2026-07-07] Cross-Assembly Test File Contention on PluginConfiguration
- **Symptom / Pitfall**: `ApplySettingsAsync_ReplacesCurrentSettings` test fails with `IOException` (file locked by another process) when all 5 plugin test assemblies run in parallel via `dotnet test` full solution run. Passes 100% when run solo.
- **Root Cause**: The host library `PluginConfiguration.SaveAsync()` writes to a shared temp config path resolved by `UniversalDeviceToolkit.Lib.Plugins.PluginBase`. Cross-assembly parallel xUnit execution causes file contention between test host processes on this shared path.
- **Enforced Rule**:
  - All plugin `SaveSettingsAsync()` methods that call `Configuration.SaveAsync()` MUST wrap the call in a retry-with-backoff pattern (3 attempts, exponential 50ms delays)
  - Catch only `IOException` (file contention) — do not retry on other exceptions
  - The root fix should be in the host library `PluginConfiguration` using per-process temp paths, but the retry pattern is a safe defensive fallback
- **.NET/OS Version**: .NET 10, Windows 11 24H2

---

### [2026-07-07] Plugin Author and Version Metadata Harmonization
- **Symptom / Pitfall**: BatteryHealth plugin used author 'EliuaK_Csy' while all other 4 plugins used 'SSC-STUDIO'; NetworkAcceleration C# Plugin attribute said version '1.1.9' while manifest said '1.2.0'.
- **Root Cause**: BatteryHealth was contributed by a different author before repository standardization; NetworkAcceleration version was bumped in manifest without syncing the C# attribute.
- **Enforced Rule**:
  - ALL plugins MUST use the same author value in plugin.json, plugin.manifest.json, and [Plugin(...)] attribute
  - The plugin.manifest.json version is the SINGLE SOURCE OF TRUTH
  - C# [Plugin(...)] attribute version MUST match plugin.manifest.json version
  - After any manifest version bump, update C# attribute, plugin.json, and Build directory in the same commit
- **.NET/OS Version**: .NET 10, Windows 11 24H2

---

### [2026-07-07] WPF Assembly Renamed Without Namespace Rename (M-010) — SUPERSEDED 2026-07-14
- **History**: Prior gate froze `LenovoLegionToolkit.*` compile identifiers until host DLL rename.
- **Phase 3 (2026-07-14)**: Hard cutover complete — namespaces/assemblies/csproj/solution/manifest class fields are `UniversalDeviceToolkit.*`. Still **do not** rename SettingsManager/ShellIntegration legacy `%LocalAppData%\LenovoLegionToolkit\` migration path strings.
- **.NET/OS Version**: .NET 10, Windows 11 24H2

---

### [2026-07-09] Unguarded async-void Event Handlers Crash the Host Process (PLG-012)
- **Symptom / Pitfall**: WPF async-void event handlers (e.g. sync void XxxButton_Click) perform async I/O (plugin loading, file dialogs, process bootstrap) without try-catch. Any unhandled exception propagates to the SynchronizationContext and tears down the whole host WPF process, since async-void cannot be observed by the caller.
- **Root Cause**: async-void methods do not surface exceptions to the caller; unhandled exceptions reach AppDomain.UnhandledException / TaskScheduler.UnobservedTaskException and crash. PluginWorkbench had 8 unguarded async-void handlers (MainWindow_Loaded, PluginListBox_MouseDoubleClick, LoadSelectedButton_Click, BootstrapHostButton_Click, OpenZipButton_Click, ReloadCurrentButton_Click, ModeToggleButton_Click, ModeComboBox_SelectionChanged).
- **Enforced Rule (PLG-012)**:
  - Every sync void WPF event handler MUST wrap its body in 	ry { ... } catch (Exception ex) { <log + inline status> }.
  - Log via a single AppendLog/PluginLog.Trace sink and surface the failure inline (e.g. StatusTextBlock.Text = "..."); never let exceptions escape async-void.
  - Preserve state-cleanup side effects outside or in inally (e.g. _suppressModeSelectionChanged = false, button IsEnabled restore in BootstrapHostButton_Click).
  - Control-flow early-returns (suppress-flag checks) should sit above the try or be returned from inside; the guard must not swallow expected cancellations.
  - Reference pattern: RunOptimizationActionButton_Click (MainWindow.xaml.cs).
- **.NET/OS Version**: .NET 10, Windows 11 24H2

---

### [2026-07-09] Missing `using` on HttpResponseMessage Causes Socket Leak (VSR-014)
- **Symptom / Pitfall**: `ViveToolDownloadService.DownloadViveToolAsync()` calls `httpClient.GetAsync()` with `HttpCompletionOption.ResponseHeadersRead` but stores the result in a plain local (`var response`). When `EnsureSuccessStatusCode()` throws (non-2xx status), the `HttpResponseMessage` is never disposed, and the underlying TCP connection is not returned to the pool — a socket leak that eventually exhausts the connection pool under repeated failures.
- **Root Cause**: The `using var response` pattern was used consistently in the sibling `ImportFeaturesFromUrlAsync` (line 253) but omitted in `DownloadViveToolAsync` (line 80). The `ResponseHeadersRead` option makes this worse because the content stream has not been fully consumed, so the response cannot be implicitly cleaned up by the GC finalizer in a timely manner.
- **Enforced Rule**:
  - EVERY `HttpClient.GetAsync` / `PostAsync` / `SendAsync` call MUST use `using var response = await ...` regardless of whether the content is consumed synchronously or asynchronously.
  - This rule applies even when the response stream is read and disposed separately via a nested `using` — the `response` itself must still be covered by `using` to guarantee cleanup on the exception path.
  - Reference: `ImportFeaturesFromUrlAsync` (ViveToolDownloadService.cs) — the correct pattern. The buggy pattern was `var response = await ...; response.EnsureSuccessStatusCode();` without `using`.
- **.NET/OS Version**: .NET 10, Windows 11 24H2

---

### [2026-07-09] Catch-All Exceptions Silently Suppress SDK Diagnostics (H-007)
- **Symptom / Pitfall**: Plugin SDK `PluginHostContext.ResolveType()` and `CreateHostWindow()` both contained bare `catch { return null; }` blocks that swallowed every exception — including `FileLoadException` (assembly version mismatch), `BadImageFormatException` (corrupt DLL), `SecurityException`, and `TargetInvocationException` (constructor body threw). When the host couldn't resolve a plugin's host bridge or settings window, the entire system silently degraded to "Preview mode" with zero diagnostic output.
- **Root Cause**: Code assumed any assembly/host-window resolution failure was benign and returned null silently. In practice `FileLoadException` (version policy failure), `SecurityException`, and `BadImageFormatException` are genuine diagnostic issues that developers need visibility into.
- **Enforced Rule**:
  - Catch and return null ONLY for *expected* failures (`FileNotFoundException`, `BadImageFormatException` for assemblies; `MissingMethodException` for missing constructors).
  - For *unexpected* exceptions, `Debug.WriteLine(...)` surfaces the failure in Debug builds (zero-cost in Release). This is the minimum diagnostic requirement for SDK/library code.
  - Never use bare `catch { }` / `catch { return null; }` in plugin SDK code — always catch a specific type or at minimum `catch (Exception ex)` with diagnostic output.
  - Reference: `PluginHostContext.ResolveType` and `CreateHostWindow`.
- **.NET/OS Version**: .NET 10, Windows 11 24H2
