# Knowledge Base — Lessons Learned & Rules (经验知识与规则累积)

This file is the **Living Knowledge Ledger** for the Universal Device Toolkit Plugins project. Every time an AI agent or human developer solves a complex bug, discovers a Windows OS quirk, or optimizes an architecture, they MUST append a structured entry here.

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
  - `.csproj` `<Version>` MUST match `plugin.manifest.json` version
  - Use `.\llt-plugin.cmd promote` to auto-sync versions
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
- ALL plugins MUST pass `.\llt-plugin.cmd validate --profile contributor`
- Version consistency check: `plugin.json` = `plugin.manifest.json` = `.csproj <Version>`
- Build output MUST have 0 warnings, 0 errors (enforced via `TreatWarningsAsErrors`)

### Store Metadata Strict Mode
- `store-entry.json` MUST byte-match the `store` block of the sibling `plugin.manifest.json` (promote/generate-sync keeps them in lockstep).
- NEVER regenerate the root `store.json` wholesale via `generate-store` when some plugins lack a published release ZIP: it zeroes `fileSize`/`hash` for those plugins (silent regression). Merge the `store-entry.json` of a new plugin incrementally instead.
- Release assets are the single source for `fileSize`/`hash`; only present `release-assets/<id>-v<ver>.zip` entries may carry nonzero sizes.

### Release Automation
- Use `.\llt-plugin.cmd promote` to generate `store-entry.json`
- Release ZIP naming convention: `<plugin-id>-v<version>.zip`
- Tag format: `v<version>-<plugin-id>` (e.g., `v1.2.0-network-acceleration`)

---

## Cross-Repository Naming & ABI Compatibility (LenovoLegionToolkit -> UniversalDeviceToolkit)

- **Timestamp / Version**: 2026-07-06, .NET 10, host vendored as LenovoLegionToolkit.Lib v3.6.15
- **Symptom / Pitfall**: Mass-renaming internal `LenovoLegionToolkit.*` namespaces to `UniversalDeviceToolkit.*` in plugin projects breaks the build because the host SDK forwarder (PluginBase) inherits from `LenovoLegionToolkit.Lib.Plugins.PluginBase` and references `LenovoLegionToolkit.Lib.dll`.
- **Root Cause**: The plugin repo host dependencies and SDK are still published under the `LenovoLegionToolkit` assembly/namespace identity. The Main repo (UniversalDeviceToolkit) retains a LegacyPluginContracts.cs shim under the same `LenovoLegionToolkit.Lib.Plugins` namespace for ABI compatibility. Renaming plugins alone yields unresolved type references.
- **Enforced Rule**: Brand ONLY user-visible/store text as "Universal Device Toolkit" (manifest store.description, repository/issues URLs, UI strings). Keep internal `LenovoLegionToolkit.*` namespaces, x:Class names, manifest `class` resolution, and DLL assembly names intact until the Main repo ships renamed host DLLs. A full rename is a cross-repo TODO gated on the host DLL rename (tracked in BUGS.md M-010).
- **Evidence**: BatteryHealth brand-only pass verified - 16/16 tests green, 0 warnings/0 errors, store-entry.json merged into root store.json with Universal Device Toolkit branding.
- **Remaining user-visible `Lenovo Legion Toolkit` remnants (coordinated M-010 cross-repo pass, gated on host DLL rename)**:
  - `ShellIntegration` `PluginDescription` - localised `Resource.*.resx` `<value>` (all locales) + C# fallback `Plugins\ShellIntegration\ShellIntegrationText.cs:9` ("Integrate Lenovo Legion Toolkit with Windows shell context menu."). Cosmetic in-app subtitle; the store-facing `store.json` description is already rebranded to "Universal Device Toolkit". Rebrand the resx values + the C# fallback together.
  - `ShellIntegration` managed-config sentinel - `"Managed by Lenovo Legion Toolkit"` written into generated Nilesoft config; asserted by 4 tests in `Plugins\ShellIntegration.Tests\ShellIntegrationConfigServiceTests.cs` (L492/L521/L607/L978). Has config-detection/compatibility semantics; rebrand must be coordinated across the host repo and update those assertions in lockstep.
  - Internal `LenovoLegionToolkit.*` namespaces / `x:Class` / manifest `class` / DLL assembly names - intentionally intact (ABI; see BUGS.md M-010).
- **Dual-track verification of the broad 2026-07-06 working-tree changeset**: full `dotnet build LenovoLegionToolkit-Plugins.sln -c Release` => 0 warnings / 0 errors; `dotnet test` => 409 passed / 0 failed / 0 skipped (CustomMouse 54, ShellIntegration 114, BatteryHealth 16, NetworkAcceleration 39, ViveTool 186).

## Cross-Repo Naming Audit Re-confirmation (2026-07-06, M-010 Gate Still Valid)
- Re-audited the Main repo (`UniversalDeviceToolkit`): the project directory and solution file were renamed to `UniversalDeviceToolkit.*`, but `<AssemblyName>` / `<RootNamespace>` are STILL `LenovoLegionToolkit.Lib` / `LenovoLegionToolkit.Lib.Plugins`, and `LegacyPluginContracts.cs` retains the `LenovoLegionToolkit.Lib.Plugins` namespace for ABI compatibility. Therefore the Plugin repo compile-time identifiers (which match the Main `AssemblyName`) MUST stay `LenovoLegionToolkit.*` - the M-010 gate is still valid. Brand only the user-visible/store text as "Universal Device Toolkit"; do NOT mass-rename namespaces, `x:Class`, manifest `class`, or DLL names until the Main repo ships renamed host DLLs.

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
- **Evidence**: PLG-004 remediated in working tree; `dotnet build LenovoLegionToolkit-Plugins.sln -c Release` => 0 warnings / 0 errors; `dotnet test Plugins\BatteryHealth.Tests --no-build` => 16/16 pass. Per-query hard timeout `WmiQueryTimeoutMs = 2500` added (outer `WmiTimeoutMs = 3000` retained as the total budget ceiling).

---

## Brand Generalization Rule - User-Visible Text Must Not Pin to Lenovo Legion (M-010 Cosmetic Gate)
- **Timestamp / Version**: 2026-07-06, .NET 10, Plugins repo docs, Windows 11 24H2
- **Symptom / Pitfall**: Marketing/store docs still framed the toolkit as "联想拯救者工具包" / "Lenovo Legion laptops" / `r/LenovoLegion`, contradicting the renamed product scope (Universal Device Toolkit); a reader/contributor would infer the plugins only target Lenovo Legion laptops, which is now false.
- **Root Cause**: The repo evolved from a Lenovo-Legion-only toolkit to UniversalDeviceToolkit (any Windows 10/11 OEM device). Internal compile identifiers (`LenovoLegionToolkit.*` namespaces, `x:Class`, manifest `class`, solution/DLL names, `host-release.json`) CANNOT be renamed without breaking the host ABI (M-010 gate, gated on the Main repo shipping renamed host DLLs). User-visible strings ARE free to generalize, and the marketing/docs lagged behind the rebrand.
- **Enforced Rule**: (1) User-visible product framing (store copy, README, promotion docs, Reddit/V2EX/Bilibili posts, architecture doc prose) MUST use "Universal Device Toolkit" / "通用设备工具包" / universal OEM scope ("your Windows setup", "any-brand Windows laptop") — never Lenovo-Legion-exclusive positioning. (2) Community handles: `r/Lenovo` (genuinely cross-brand) is fine; `r/LenovoLegion` is NOT — redirect to a broader community (`r/pcmasterrace`) or remove. Competitor comparisons (e.g. "Lenovo Vantage") and canonical GitHub topics (`lenovo-legion`) may be retained as factual references. (3) DO NOT mass-rename compile identifiers — `LenovoLegionToolkit.*` namespaces, `x:Class`, `plugin.manifest.json` `class`, `*.csproj`/solution filenames, DLL assembly names, `host-release.json` stay `LenovoLegionToolkit` until the Main repo ships renamed host DLLs. Apply generalization surgery per-occurrence, fixing ONLY the user-visible token (e.g. ARCHITECTURE.md L5 generalized the Chinese product name "联想拯救者工具包"->"通用设备工具包" while keeping the M-010 `LenovoLegionToolkit-Plugins` solution name on the same line). ARCHITECTURE.md L15/L61/L218 (bare `LenovoLegionToolkit` directory-tree/sibling-repo/ASCII-diagram identifiers) are M-010 and stay as-is.
- **Evidence**: 15 docs rebranded (2026-07-06); `rg 'r/LenovoLegion'` across all .md => 0; Chinese brand residual ("联想拯救者"/"联想笔记本") => 0; English "Lenovo Legion" remaining only in this file's own M-010 gate documentation (ShellIntegration PluginDescription + managed-config sentinel). Dual-track verified: `dotnet build LenovoLegionToolkit-Plugins.sln -c Release` => 0 warnings / 0 errors; `dotnet test` => 409 passed / 0 failed / 0 skipped.

## Cross-Repo Data-Root Alignment - Dual-Name Migration Contract (M-010 ABI Gate)
- **Timestamp / Version**: 2026-07-07, .NET 10, plugin<->host data-root verification, Windows 11 24H2
- **Symptom / Pitfall**: After the LenovoLegionToolkit->UniversalDeviceToolkit rebrand, a contributor might "fix" hard-coded `LenovoLegionToolkit` path segments in plugin or Main repo settings/discovery code, believing them to be stale brand drift. Doing so would silently break legacy-setting migration for pre-rebrand users AND split the active data root between the plugin (writer) and the host (reader).
- **Root Cause**: Both repos preserve the OLD name `LenovoLegionToolkit` DELIBERATELY as a forward-migration *source*, while writing NEW data under `UniversalDeviceToolkit`. The active data root must stay the SAME new name on BOTH sides; the legacy name is read-only migration scaffolding, NOT active state.
- **Enforced Rule**: (1) Active write roots MUST use `UniversalDeviceToolkit` (`%LocalAppData%\UniversalDeviceToolkit`; plugins sub-root `%LocalAppData%\UniversalDeviceToolkit\plugins\<id>`). (2) `LenovoLegionToolkit` path segments are read-ONLY legacy-migration sources - DO NOT delete or rename them; pre-rebrand users have settings migrated from `%LocalAppData%\LenovoLegionToolkit`. (3) In Main, prefer `AppIdentity.CompactName`/`LegacyCompactName` over hard-coded string literals so the contract follows a single source of truth. (4) Test harnesses that seed a legacy `%LocalAppData%\LenovoLegionToolkit\settings.json` (e.g. `Tools\MainAppPluginUi.Smoke\Program.cs:L1342-1344`) are SIMULATING the legacy-upgrade path - keep them on the legacy name to exercise the host migration logic; do NOT point them at the new name. (5) Before any cross-repo change, verify: plugin `SettingsManager` primary root == Main `Folders.AppData` == `UniversalDeviceToolkit`; `PluginDiscovery` search roots include `UniversalDeviceToolkit\plugins`.
- **Evidence**: Plugin repo `SettingsManager<T>.cs` L21-24 writes `%LocalAppData%\UniversalDeviceToolkit\plugins\`; L29/L72-76 retain `LenovoLegionToolkit\plugins` & `AppDomain\plugins` as legacy MIGRATION read sources. Main repo `Folders.cs` `AppData` (L29-30) writes `%LocalAppData%\UniversalDeviceToolkit` with legacy-then-new read search (L64-66); `PluginDiscovery.cs` L180 adds `%AppData%\UniversalDeviceToolkit\plugins` to search roots; `Tools\MainAppPluginUi.Smoke\Program.cs` L1342-1344 seeds a legacy `settings.json` to exercise migration. NO production data-root mismatch exists; NO cross-repo code change required. Dual-track verified this session: `dotnet build LenovoLegionToolkit-Plugins.sln -c Release` => 0 warnings/0 errors; `dotnet test` => 409/409 green (BatteryHealth 16, CustomMouse 54, ShellIntegration 114, NetworkAcceleration 39, ViveTool 186).

---

**This file is alive. Every bug fix or architecture optimization MUST be recorded here.** 📝

---

### [2026-07-07] Host v4.2.1 Serilog Transitive Dependency
- **Symptom / Pitfall**: After upgrading vendored host DLLs from v3.6.14 to v4.2.1, plugin tests fail with `System.IO.FileNotFoundException: Could not load file or assembly 'Serilog, Version=4.3.0.0'`. 15/409 tests fail (14 in ViveTool, 1 in NetworkAcceleration). PluginLoader-based workbench fails to load any plugin with `InvalidOperationException` at `PluginWorkbenchSession.cs:155`.
- **Root Cause**: v4.2.1 `LenovoLegionToolkit.Lib.dll` uses Serilog 4.3.0 in `Log..ctor()` (logging initialization), which is a new transitive dependency not present in v3.6.14. The plugin repo's `Dependencies\Host\` only vendored the 3 host DLLs (Lib, Lib.Plugins, WPF) but not their transitive dependencies.
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
- **Symptom / Pitfall**: After adding `<Reference Include="LenovoLegionToolkit.Lib.Plugins">` to `Tools\PluginWorkbench\PluginWorkbench.csproj`, build fails with 2 CS0104 errors: `PluginHostMode` is ambiguous between `LenovoLegionToolkit.Lib.Plugins.PluginHostMode` and `LenovoLegionToolkit.Plugins.SDK.PluginHostMode`. After adding the alias, 3 more CS0104 errors for `PluginHostContext` (static class).
- **Root Cause**: The host Lib.Plugins DLL and the SDK both define the same types (intentional type forwarding / dual-surface design). The `using LenovoLegionToolkit.Lib.Plugins;` import was already present but harmless (the assembly was not referenced, so the types were invisible). Once Lib.Plugins.dll v4.2.1 was referenced, both namespaces became visible and C# disambiguation failed.
- **Enforced Rule**:
  - When adding the `LenovoLegionToolkit.Lib.Plugins` reference to any project that also imports `LenovoLegionToolkit.Plugins.SDK`, add using aliases for ALL shared types: `using PluginHostMode = LenovoLegionToolkit.Plugins.SDK.PluginHostMode;` and `using PluginHostContext = LenovoLegionToolkit.Plugins.SDK.PluginHostContext;`
  - The SDK types are the correct consumer-facing types: SDK `PluginHostContext.Current` has a public setter (for `PluginHostContext.Current = _hostContext` assignment), Lib `PluginHostContext.Current` is read-only (must use `SetCurrent()` method instead) — code that does property assignment only compiles against SDK
  - The intersection of types in both namespaces is: `IAppStartupPlugin`, `IOptimizationCategoryProvider`, `IPluginHostContext`, `IPluginPage`, `PluginHostContext`, `PluginHostMode` — audit any new `using` block that imports both namespaces and alias all 6 if used unqualified
- **.NET/OS Version**: .NET 10, UniversalDeviceToolkit host v4.2.1

---

### [2026-07-07] Cross-Assembly Test File Contention on PluginConfiguration
- **Symptom / Pitfall**: `ApplySettingsAsync_ReplacesCurrentSettings` test fails with `IOException` (file locked by another process) when all 5 plugin test assemblies run in parallel via `dotnet test` full solution run. Passes 100% when run solo.
- **Root Cause**: The host library `PluginConfiguration.SaveAsync()` writes to a shared temp config path resolved by `LenovoLegionToolkit.Lib.Plugins.PluginBase`. Cross-assembly parallel xUnit execution causes file contention between test host processes on this shared path.
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

### [2026-07-07] WPF Assembly Renamed Without Namespace Rename (M-010)
- **Symptom / Pitfall**: Need to reflect UniversalDeviceToolkit rebrand in plugin references, but `host-release.json` `minLLTVersion` JSON property, `plugin.manifest.json` `class` field, and `LenovoLegionToolkit.*` namespaces must stay frozen for host ABI compatibility.
- **Root Cause**: M-010 ABI gate — the host (UniversalDeviceToolkit v4.2.1) loads plugins via reflection on names like `LenovoLegionToolkit.Plugins.BatteryHealth.BatteryHealthPlugin`. Renaming the namespace would break plugin loading for ALL existing users.
- **Enforced Rule (M-010 whitelist)**:
  - **DO rename**: User-visible strings (UI text, docs, marketing), WPF `AssemblyName` (the `.dll` filename of the WPF host — `Lenovo Legion Toolkit.dll` → `Universal Device Toolkit.dll`), cross-csproj `<Reference Include="...">` names, `host-release.json` `downloadUrl` / `artifacts.wpf` / `artifacts.package` fields
  - **DO NOT rename**: `LenovoLegionToolkit.Plugins.*` namespaces, `LenovoLegionToolkit-Plugins.sln` filename, `*.csproj` filenames, plugin assembly names, `plugin.manifest.json` `class` field, DLL names, `store.json` `minLLTVersion` JSON property name, `SettingsManager<T>` legacy read paths under `%LocalAppData%\LenovoLegionToolkit\`
- **.NET/OS Version**: .NET 10, Windows 11 24H2, UniversalDeviceToolkit v4.2.1
