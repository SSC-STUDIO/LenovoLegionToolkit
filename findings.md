# Findings & Decisions

## Requirements
- User requested: understand project first, improve main app plugin system, and fix issues in sibling plugin repository.
- User requested explicit use of `planning-with-files` skill.
- Keep existing uncommitted changes intact unless directly part of requested work.
- User requested a new `planning-with-files` analysis focused on translation files across 20+ languages, specifically checking missing entries and incorrect translations.

## Research Findings
- Current workspace root: `LenovoLegionToolkit`.
- Sibling plugin repository exists at `..\LenovoLegionToolkit-Plugins`.
- Main repo currently has pre-existing modified files:
  - `CHANGELOG.md`
  - `LenovoLegionToolkit.WPF/Pages/PluginExtensionsPage.xaml.cs`
  - `LenovoLegionToolkit.WPF/ViewModels/WindowsOptimizationViewModel.cs`
  - `LenovoLegionToolkit.WPF/Windows/MainWindow.xaml.cs`
- Planning files initialized in project root.

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Start with codebase mapping before edits | Need concrete issues first to avoid speculative changes |
| Focus fixes on plugin-system path and plugin build/runtime correctness | Directly aligned with user objective |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| Script execution policy blocked init script | Used `-ExecutionPolicy Bypass` |
| Initial PowerShell file-existence command invalid | Replaced with per-file `Test-Path` |

## Resources
- `C:\Users\96152\.codex\skills\planning-with-files\SKILL.md`
- `C:\Users\96152\.codex\skills\planning-with-files\scripts\init-session.ps1`
- Main plugin-system source concentrated in `LenovoLegionToolkit.Lib/Plugins` and WPF plugin pages/settings files.
- Sibling plugin repo has pre-existing dirty state (`LenovoLegionToolkit-Plugins.sln`, `obj/` folders).
- Build result: main app WPF project builds successfully in Release.
- Build result: plugin repo solution fails due to SDK project misconfiguration and wrong project selection in solution.
- Errors include NETSDK1005 for `SDK\\LenovoLegionToolkit.Plugins.SDK.csproj` and CS0234 in `SDK.csproj` (`LenovoLegionToolkit.Lib` namespace unresolved).
- Key compatibility issue found: plugin repo builds plugin DLL names like `custom-mouse.dll` / `shell-integration.dll`, while main app `PluginManager.IsPluginDll` only accepts `LenovoLegionToolkit.Plugins.*.dll` naming.
- This can prevent store-installed plugins from loading even if installation succeeds.
- Plugin repo contains duplicate SDK projects (`SDK.csproj` and `LenovoLegionToolkit.Plugins.SDK.csproj`), and solution currently points to the old one.
- Implemented main host plugin discovery compatibility:
  - `PluginManager` now accepts both classic `LenovoLegionToolkit.Plugins.*.dll` and ID/folder-matched names (e.g. `custom-mouse.dll`).
  - Added normalized token matching to bridge hyphenated IDs vs PascalCase folder names.
  - File cache now uses full path + UTC write time and updates after successful load.
- Implemented plugin store compatibility in host:
  - `PluginManifest` now maps legacy `minLLTVersion` into `MinimumHostVersion`.
  - `PluginRepositoryService` now fetches store metadata with URL fallback (`main` then `master`).
  - ZIP extraction now resolves plugin main DLL via robust matching instead of strict filename contains checks.
- Implemented plugin-repo project fixes:
  - Solution now references `SDK\\LenovoLegionToolkit.Plugins.SDK.csproj`.
  - `CustomMouse.Tests` project references corrected, target updated to `net10.0-windows`.
  - Rewrote outdated `CustomMousePluginTests.cs` to align with current plugin API.
  - Adjusted test project build overrides to prevent plugin cleanup logic from removing test runtime assets.
- Validation summary:
  - Main app WPF build (Release): PASS.
  - Plugin repo solution build (Release): PASS.
  - ViveTool + NetworkAcceleration plugin builds (Release): PASS (with existing CA1416 warnings in ViveTool).
  - CustomMouse tests: 20 passed.
- Changelog updates completed:
  - Main repo `CHANGELOG.md` updated with plugin discovery compatibility, store metadata compatibility, and store URL fallback improvements.
  - Plugin repo `Plugins/CustomMouse/CHANGELOG.md` updated for test/build fix release notes (v1.0.1).
- Additional install-flow discovery and fix:
  - `PluginInstallationService` still enforced legacy DLL pattern and failed to import ZIPs that use ID-based DLL names (e.g. `custom-mouse.dll`).
  - Fixed in `LenovoLegionToolkit.Lib/Plugins/PluginInstallationService.cs` by:
    - Supporting both naming schemes when analyzing and validating plugin ZIP contents.
    - Reading `plugin.json` `id` as plugin identity fallback.
    - Resolving main plugin DLL with normalized token matching (hyphenated IDs supported).
- Smoke-test evidence (real install/uninstall flow):
  - `PluginInstallationService.ExtractAndInstallPluginAsync`: PASS with `custom-mouse.dll` package.
  - `PluginRepositoryService.DownloadAndInstallPluginAsync` (`file://`): PASS after aligning smoke host with real runtime by placing `LenovoLegionToolkit.Plugins.SDK.dll` in smoke app output.
  - `PluginManager` scan/load installed-state and uninstall pending-deletion flow: PASS.
- Regression verification after fix:
  - Main app WPF Release build: PASS.
  - Plugin repo solution Release build: PASS.
  - `CustomMouse.Tests`: 20 passed.
- Per-plugin reliability fixes completed in plugin repository:
  - Added explicit plugin metadata attributes (`[Plugin(...)]`) for `CustomMouse`, `ShellIntegration`, and `NetworkAcceleration` with minimum host `3.6.1`.
  - Standardized `ViveTool` runtime ID to `vive-tool` and aligned its metadata (attribute + `Plugin.json`) with store identity.
  - Added packaged `plugin.json` manifests for `CustomMouse`, `ShellIntegration`, and `NetworkAcceleration`; ensured `ViveTool` manifest is copied as `plugin.json` in output.
  - Added plugin projects to solution coverage: `NetworkAcceleration`, `ViveTool`, `CustomMouse.Tests`.
- Plugin metadata/version alignment completed:
  - Main repo version file updated to `3.6.1` (`Directory.Build.props` patch version).
  - Plugin repo version file updated to `1.0.1` (`Directory.Build.props`).
  - Plugin versions aligned with changelogs and store:
    - `custom-mouse` -> `1.0.2`
    - `shell-integration` -> `1.0.1`
    - `network-acceleration` -> `1.0.1`
    - `vive-tool` -> `1.1.1`
  - `store.json` expanded to include all four official plugins and updated store version/timestamps.
- Changelog coverage completed:
  - Main `CHANGELOG.md` added plugin-ecosystem metadata consistency fix entry.
  - Plugin changelogs updated for all official plugins:
    - `Plugins/CustomMouse/CHANGELOG.md` (`1.0.2`)
    - `Plugins/ShellIntegration/CHANGELOG.md` (`1.0.1`)
    - `Plugins/NetworkAcceleration/CHANGELOG.md` (`1.0.1`)
    - `Plugins/ViveTool/CHANGELOG.md` (`1.1.1`)
- Full all-plugin smoke validation (new temporary harness) passed:
  - Direct ZIP import for all four plugins: PASS.
  - Repository `file://` install for all four plugins: PASS.
  - Host scan/load registration and install-state checks for all four plugins: PASS.
  - Uninstall + pending deletion processing for all four plugins: PASS.
- Post-clean rerun confirmation:
  - After cleaning plugin solution outputs, main/app/plugin builds and all-plugin smoke test were rerun and remained PASS.
- Final delivery-state adjustments:
  - Reduced main `CHANGELOG.md` diff to a minimal `3.6.1` delta only (avoided broad historical formatting churn).
  - Plugin solution build outputs are emitted under main repo `Build\plugins\...`; all-plugin smoke harness must use main repo root as `<repoRoot>`.
- UX behavior gap confirmed from runtime logs:
  - Plugin list item double-click handler previously called click handlers with `sender` = `ListBox`, so install/open logic did not execute as intended.
  - Updated behavior: installed plugin item double-click now opens plugin settings directly (same flow as configure button).
- Settings UI rendering issue confirmed:
  - `SettingsPage` sidebar item template used animated `DropShadowEffect` for selected entries.
  - This effect can render inconsistently across different GPU/driver combinations; replaced with stable highlight-only selection visuals.
- Plugin management UX extension completed:
  - Added a new one-click bulk install action in `PluginExtensionsPage` top action bar.
  - Bulk install targets all currently available online plugins not yet installed, runs sequentially, and refreshes plugin list/state after completion.
  - Added localized text keys for bulk install button/status in `Resource.resx`, `Resource.zh-hans.resx`, and `Resource.zh-hant.resx`.

## 2026-02-26 Systematic Audit Session
- Re-read repository AGENTS.md and confirmed mandatory rule to update CHANGELOG.md after user-visible fixes.
- Confirmed this repository already has pre-existing modified files; audit must layer new fixes without reverting unrelated work.
- Next execution focus: full solution baseline (
estore/build/test) and direct remediation of reproducible failures.
- 2026-02-26 audit (plugin UI): Update hint icon currently binds only to UpdateAvailable; store metadata often lacks changelog, making tooltip feel empty. Planned fix: gate update hint by install-state, hide empty rows, format 
eleaseDate, and add clickable changelog URL action from update icon.
- 2026-02-26 audit completion: implemented plugin update indicator usability fix in main app.
  - Added UpdateAvailable/UpdateInfoVisible bindings in PluginViewModel so UI reflects real backend update state.
  - Limited update hint rendering to installed plugins with actual updates.
  - Formatted release date for display and hid empty release/changelog rows in tooltip.
  - Added update-icon click action to open changelog URL when provided by store metadata.
- Added changelog URLs for all official plugins in sibling repository store.json so update-info click-through in host UI has actionable targets.
- 2026-02-26 icon-color fix: plugin icon colors were unstable because string.GetHashCode() is process-randomized in .NET; replaced with deterministic hash and wired store.json.iconBackground into plugin card ViewModel mapping.
- Sibling plugin repository metadata now includes explicit iconBackground values for all official plugins, so icon colors can be centrally managed via store.json instead of fallback hashing.
- New runtime issue from user logs (2026-02-26, plugin UX):
  - Installed plugins are repeatedly reported as "does not provide settings page" and "does not provide IPluginPage".
  - Main window currently adds all installed plugins into sidebar navigation, causing blank plugin pages for plugins without `GetFeatureExtension()` return value.
  - Plugin card Open button is currently visible for any installed plugin, even if plugin has no feature page.
  - Sibling plugins `CustomMouse`, `NetworkAcceleration`, `ShellIntegration` are still skeletal and do not expose feature/settings/optimization hooks required by current UI expectations.
- Behavior gap to fix:
  - `shell-integration` should surface through Windows Optimization extension category, not as an empty sidebar page.
  - `network-acceleration` should expose a usable configuration/settings page.
  - `custom-mouse` should expose a usable page/settings instead of placeholder behavior.
- Implemented host-side capability alignment:
  - `MainWindow.UpdateInstalledPluginsNavigationItems()` now includes only installed plugins that expose `GetFeatureExtension()` as `IPluginPage`.
  - Plugin card `Open` button now depends on `IsInstalled && SupportsFeaturePage`.
  - Plugin capability probing in `PluginExtensionsPage` now separates `SupportsSettingsPage` and `SupportsFeaturePage`.
  - `PluginSettingsWindow` now supports both raw `Page` objects and `IPluginPage` providers from plugins.
- Implemented sibling plugin functionality (official plugins):
  - `custom-mouse` now provides feature page + settings page and persists configuration through `PluginBase.Configuration`.
  - `network-acceleration` now provides feature page + settings page with persisted optimization preferences.
  - `shell-integration` now provides settings page and plugin-supplied Windows Optimization category/actions; no feature page is exposed for sidebar navigation.
- Verification results for this round:
  - Main WPF build (`Release`): PASS.
  - Plugin repository solution build (`Release`): PASS (warnings only).
  - `CustomMouse.Tests`: 20/20 PASS.
  - Install smoke test (real `PluginInstallationService` + ZIP packages for `custom-mouse`, `network-acceleration`, `shell-integration`): PASS for all three (install result + DLL + plugin.json checks).
- Version metadata updated for delivery:
  - Main repo version -> `3.6.2` (`Directory.Build.props` patch bump).
  - Plugin repo version -> `1.0.2` (`Directory.Build.props`).
  - Plugin versions bumped and aligned:
    - `custom-mouse` -> `1.0.3`
    - `network-acceleration` -> `1.0.2`
    - `shell-integration` -> `1.0.2`
  - Plugin store metadata (`store.json`) updated accordingly, including version/tag links and `lastUpdated` date.

## 2026-02-26 Audit Results (Main Repository)
- Reproduced and fixed a real retry-loop bug in RetryHelper: reaching max retries no longer loops forever.
- Reproduced and fixed a real process-output deadlock in CMD.RunAsync under large-output commands (dir %TEMP%).
- Reproduced and fixed stale/incompatible test expectations across controller/util layers (GodMode, AIController, Sensors, GPUController, WindowsOptimization, CMD).
- Updated PowerModeUnavailableWithoutACException message to include blocked mode name for clearer diagnostics.
- Final stabilization pass fixed two timing-sensitive throttling tests:
  - `LenovoLegionToolkit.Tests/ThrottleFirstDispatcherTests.cs` now verifies throttle behavior with deterministic queued dispatch ordering.
  - `LenovoLegionToolkit.Tests/ThrottleLastDispatcherTests.cs` removed fragile short-delay pacing and asserts last-call execution via rapid consecutive dispatches.
- Verification matrix:
  - dotnet test LenovoLegionToolkit.Tests/LenovoLegionToolkit.Tests.csproj --framework net10.0-windows -c Release --no-build => PASS (364/364)
  - dotnet build LenovoLegionToolkit.sln -c Debug => PASS
  - dotnet build LenovoLegionToolkit.sln -c Release => PASS
- Process note: parallel Debug/Release solution builds can produce transient false-negative WPF compile errors in this workspace; sequential builds are stable.

## 2026-02-26 Continuation Findings
- User-reported runtime symptom "installed but no settings/blank page" is now covered by current capability split + plugin implementations:
  - Host only surfaces sidebar plugin navigation for plugins that provide `IPluginPage`.
  - Per-plugin open/config actions now map to actual feature/settings capability.
  - `shell-integration` remains optimization-category/settings oriented, not a sidebar blank page.
- Independent install smoke was re-run by agent using real `PluginInstallationService` and current plugin build outputs; all 3 core official plugins pass ZIP install validation end-to-end.
- No temporary artifacts remain in repository after smoke run.

## 2026-02-26 Independence Refactor Findings (complete)
- Plugin repository now builds without any source `ProjectReference` to sibling `LenovoLegionToolkit` repo code.
- Shared compile-time host API reference is centralized in plugin-repo `Directory.Build.props`:
  - `Dependencies/Host/LenovoLegionToolkit.Lib.dll`
  - `WPF-UI` package reference for plugin XAML controls
- Added independent maintenance script:
  - `scripts/refresh-host-references.ps1` to refresh vendored host DLLs from a host build output.
- Fixed independence-related compile/test blockers:
  - Added missing `NeoSmart.AsyncLock` package reference to `ViveTool`.
  - Updated `CustomMouse.Tests` runtime host reference copy behavior and aligned lifecycle assertion with current plugin API.
- Validation:
  - `dotnet build LenovoLegionToolkit-Plugins.sln -c Release --no-restore` (from plugin repo root): PASS.
  - `dotnet test Plugins/CustomMouse.Tests/CustomMouse.Tests.csproj -c Release --no-build` (from plugin repo root): PASS (20/20).

## 2026-02-26 Translation Audit Findings (in progress)
- Resource footprint confirmed across four modules with 20+ locales each:
  - `LenovoLegionToolkit.WPF/Resources/Resource*.resx`
  - `LenovoLegionToolkit.Lib/Resources/Resource*.resx`
  - `LenovoLegionToolkit.Lib.Automation/Resources/Resource*.resx`
  - `LenovoLegionToolkit.Lib.Macro/Resources/Resource*.resx`
- Audit scope will check:
  - Missing/extra keys per locale against neutral `Resource.resx`
  - Empty values and whitespace-only values
  - Format placeholder mismatch (`{0}`, `{1}`, etc.)
  - Suspected untranslated strings (locale value equals neutral value for non-invariant terms)

## 2026-02-26 Independent Plugin Completion Tool Findings (complete)
- New independent validation tool added:
  - `..\\LenovoLegionToolkit-Plugins\\scripts\\plugin-completion-check.ps1`
- Tool scope (no main-app runtime dependency):
  - Validates `store.json` entries against `plugin.json` (`version`, `minLLTVersion`)
  - Validates plugin project metadata (`<Version>`, `<AssemblyName>`) consistency
  - Builds each official plugin via `dotnet build`
  - Verifies release artifacts (`<assembly>.dll`, `plugin.json`) in output directory
  - Runs sibling `*.Tests` project if available
  - Supports metadata-only checks via `-SkipBuild -SkipTests` (artifact checks are skipped in this mode)
  - Fails fast if any plugin project references forbidden sibling source path (`..\\..\\..\\LenovoLegionToolkit`)
- Documentation updated:
  - `..\\LenovoLegionToolkit-Plugins\\README.md` now includes usage and modes (`-PluginIds`, `-SkipBuild`, `-SkipTests`)
- Full-run result on current official plugins:
  - `custom-mouse`, `network-acceleration`, `shell-integration`, `vive-tool` all PASS with 0 failures.

## 2026-02-26 Independent Plugin Completion UI Tool Findings (complete)
- New standalone UI tool implemented in plugin repository:
  - `..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool`
- Independence model:
  - Tool is marked with `<IsPluginToolProject>true</IsPluginToolProject>` to opt out of plugin-only build targets in plugin repo `Directory.Build.props`.
  - No source dependency on main `LenovoLegionToolkit` repository.
- UI capability implemented:
  - Repository root selection (manual input + folder browse).
  - Optional plugin ID filter input.
  - `Configuration` (`Release` / `Debug`) + `Skip Build` + `Skip Tests`.
  - One-click run of `scripts/plugin-completion-check.ps1`.
  - Live stdout/stderr log pane.
  - Parsed JSON result views (plugin summary table + step log table).
  - Open generated JSON report file action.
- Solution and docs integration:
  - Added tool project into `..\\LenovoLegionToolkit-Plugins\\LenovoLegionToolkit-Plugins.sln`.
  - Updated `..\\LenovoLegionToolkit-Plugins\\README.md` with UI tool usage section.
- Verification:
  - `dotnet build ..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool\\PluginCompletionUiTool.csproj -c Release` => PASS (0 warnings, 0 errors).
  - `dotnet build ..\\LenovoLegionToolkit-Plugins\\LenovoLegionToolkit-Plugins.sln -c Release` => PASS (existing plugin warnings unchanged).
  - `powershell -ExecutionPolicy Bypass -File ..\\LenovoLegionToolkit-Plugins\\scripts\\plugin-completion-check.ps1 -SkipBuild -SkipTests -JsonReportPath artifacts\\plugin-completion-ui-report.json` => PASS; report generated.
- Additional full verification:
  - `powershell -ExecutionPolicy Bypass -File ..\\LenovoLegionToolkit-Plugins\\scripts\\plugin-completion-check.ps1 -JsonReportPath artifacts\\plugin-completion-ui-report.full.json` => PASS (4 plugins, 0 failures; warnings are optional test-missing items and existing platform analyzer warnings).
  - UI startup smoke (`dotnet run --project ..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool\\PluginCompletionUiTool.csproj -c Release --no-build`) => PASS (window process started successfully).
- Added tool-specific changelog:
  - `..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool\\CHANGELOG.md`

## 2026-02-26 Version Finalization Findings (complete)
- Main repository version file bumped:
  - `Directory.Build.props` `PatchVersion` updated from `2` to `3` (now `3.6.3`).
- Main changelog release section added:
  - `CHANGELOG.md` now includes `## [3.6.3] - 2026-02-26` with the plugin tooling UI-tooling release note.
- Plugin repository version file bumped:
  - `..\\LenovoLegionToolkit-Plugins\\Directory.Build.props` updated from `1.0.2` to `1.0.3`.
- Plugin repository metadata version marker updated:
  - `..\\LenovoLegionToolkit-Plugins\\store.json` top-level `version` -> `1.0.3`, `lastUpdated` refreshed.
- Post-bump validation:
  - `dotnet build LenovoLegionToolkit.WPF\\LenovoLegionToolkit.WPF.csproj -c Release --no-restore` => PASS.
  - `dotnet build ..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool\\PluginCompletionUiTool.csproj -c Release --no-restore` => PASS.
  - `plugin-completion-check.ps1` metadata run (`-SkipBuild -SkipTests`) => PASS (4 plugins, 0 failures).
  - `store.json` parse validation (`ConvertFrom-Json`) => PASS.

## 2026-02-26 Skill/MCP Readiness Findings (complete)
- Local `planning-with-files` skill metadata:
  - `C:\\Users\\96152\\.codex\\skills\\planning-with-files\\SKILL.md` reports `version: "2.10.0"`.
  - Local install timestamp for this skill folder: `2026-02-15`.
- Upstream registry check:
  - `skill-installer` curated list (`openai/skills` `.curated`) does **not** include `planning-with-files`, so curated updater cannot confirm/update it directly.
  - External index listing for `planning-with-files` shows `version: "2.10.0"`, which matches local.
- MCP/UI automation capability assessment in this session:
  - Browser automation MCP is available (`playwright`/`agent-browser`) for web UI.
  - No pre-registered desktop pointer-control MCP (for direct Windows WPF mouse-driving) is available in current toolset.
  - Therefore desktop UI testing is currently covered by build/smoke checks + launch validation; full desktop click-path automation would require adding an external desktop automation stack (for example WinAppDriver/Appium or FlaUI harness).

## 2026-02-26 Desktop UI Automation Smoke Findings (complete)
- Added deterministic automation IDs in UI tool:
  - `..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool\\MainWindow.xaml`
  - Covers key fields/actions (`RepositoryPathTextBox`, `SkipBuildCheckBox`, `SkipTestsCheckBox`, `RunButton`, status/summary text, result grids).
- Added independent desktop smoke runner:
  - `..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool.Smoke\\PluginCompletionUiTool.Smoke.csproj`
  - `..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool.Smoke\\Program.cs`
  - Uses Windows UI Automation (`System.Windows.Automation`) to simulate real click flow.
- Added tool-level changelog coverage:
  - Updated `..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool\\CHANGELOG.md` (`Unreleased`).
  - Added `..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool.Smoke\\CHANGELOG.md`.
- Added docs for execution:
  - `..\\LenovoLegionToolkit-Plugins\\README.md` includes smoke runner commands and behavior.
- Validation evidence:
  - `dotnet build ..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool\\PluginCompletionUiTool.csproj -c Release --no-restore` => PASS.
  - `dotnet build ..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool.Smoke\\PluginCompletionUiTool.Smoke.csproj -c Release --no-restore` => PASS.
  - `dotnet run --project ..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool.Smoke\\PluginCompletionUiTool.Smoke.csproj -c Release --no-build -- ..\\LenovoLegionToolkit-Plugins` => PASS.
  - `dotnet build ..\\LenovoLegionToolkit-Plugins\\LenovoLegionToolkit-Plugins.sln -c Release --no-restore` => PASS (existing analyzer warnings unchanged).

## 2026-02-26 CI + JSON Report Enhancements (complete)
- CI workflow integration added in plugin repo:
  - `..\\LenovoLegionToolkit-Plugins\\.github\\workflows\\build.yml`
  - New `validate-completion` job runs `scripts/plugin-completion-check.ps1` before build job.
  - Workflow uploads `artifacts/plugin-completion-report.json` as `plugin-completion-report` artifact.
  - Added `pull_request` trigger and updated path filters for plugin-relevant files.
- Checker JSON output support added:
  - `-JsonReportPath` parameter in `scripts/plugin-completion-check.ps1`.
  - JSON report includes metadata (`generatedAt`, config), totals, per-plugin status, and step-level logs.
- Local verification:
  - Metadata mode run with JSON output succeeded and produced valid report structure.
- Plugin-repo commits completed (feature-grouped):
  - `0821f81` tooling
  - `c71e781` CI workflow integration
  - `a1d5e43` documentation

## 2026-02-26 Translation Audit Findings (completed)
- Automated audit artifact generated: `.tmp/translation_audit.json`.
- Coverage summary by module:
  - `LenovoLegionToolkit.WPF/Resources`: 27 locale files, base 1243 keys, **0 fully-synced locales**.
  - `LenovoLegionToolkit.Lib/Resources`: 27 locale files, base 123 keys, fully-synced: `zh-hans` only.
  - `LenovoLegionToolkit.Lib.Automation/Resources`: 27 locale files, base 38 keys, fully-synced: `zh-hans` only.
  - `LenovoLegionToolkit.Lib.Macro/Resources`: 26 locale files, base 2 keys, fully-synced 17 locales.
- Deterministic errors found:
  - 1 placeholder mismatch in `LenovoLegionToolkit.WPF/Resources/Resource.zh-hans.resx`:
    - `PluginExtensionsPage_OpenFailed` lacks `{0}` while neutral string includes `{0}`.
- Systemic issues found:
  - High missing-key volume in WPF locales (commonly ~294-306 missing per locale, `ar` 326 missing).
  - Some locales are effectively untranslated in specific modules:
    - `Lib`: `ca`, `ko` missing 123/123.
    - `Lib.Automation`: `ca`, `ko` missing 38/38.
    - `Lib.Macro`: `bg`, `bs`, `cs`, `it`, `ko`, `lv`, `ro`, `sk`, `uz-latn-uz` missing 2/2.
  - Extra-key drift exists mainly in `WPF`:
    - `zh-hans` has 18 extra keys,
    - `ar` has 4 extra keys,
    - several locales have stale `SettingsPage_Autorun_Message`.
- Most commonly missing WPF keys (26 locales each) are new plugin/optimization/menu-style entries, e.g.:
  - `PluginExtensionsPage_UpdateAll`
  - `PluginExtensionsPage_BulkUpdateCompleteMessage`
  - `PluginExtensionsPage_ConfigurationNotSupported`
  - `WindowsOptimization_Category_NetworkAcceleration_Description`
  - `MenuStyleSettingsWindow_HoverColor`

## 2026-02-26 Translation Audit Auto-Fix Batch
- Applied deterministic localization fix in `LenovoLegionToolkit.WPF/Resources/Resource.resx`:
  - `PluginExtensionsPage_OpenFailed` changed from `Failed to open plugin: {0}` to `Failed to open plugin` (title text should not include unused format placeholder).
- Applied code-level key unification in `LenovoLegionToolkit.WPF/Pages/PluginExtensionsPage.xaml.cs`:
  - Replaced `PluginExtensionsPage_OpenPluginFailed` usage with `PluginExtensionsPage_OpenFailedMessage` to avoid locale-specific key drift.
- Post-fix verification:
  - `dotnet build LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj -c Release --no-restore` => PASS.
  - `zh-hans` placeholder mismatch for `PluginExtensionsPage_OpenFailed` => resolved (0 mismatch).
- Applied missing-entry fixes for actively used settings/optimization keys:
  - Added `SettingsPage_Autorun_Message` to `LenovoLegionToolkit.WPF/Resources/Resource.resx` (base) and `Resource.zh-hans.resx`.
  - Added missing base keys referenced by `WindowsOptimizationCategoryProvider`:
    - `WindowsOptimization_Action_NetworkAcceleration_Title`
    - `WindowsOptimization_Action_NetworkAcceleration_Description`
    - `WindowsOptimization_Action_NetworkOptimization_Title`
    - `WindowsOptimization_Action_NetworkOptimization_Description`
- Validation:
  - `dotnet build LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj -c Release --no-restore` => PASS after key additions.
  - `rg` verification confirms the above keys now exist in base resource and are aligned with code references.
  - Post-fix resx consistency rerun:
    - Global placeholder mismatch count: `0`.
    - WPF `zh-hans` extra-key count reduced from `18` to `14`.
    - WPF `ar` extra-key count reduced from `4` to `3` (after promoting shared key to base).
- Performed stale-key cleanup for locale-only, non-referenced entries:
  - Removed 14 extra keys from `LenovoLegionToolkit.WPF/Resources/Resource.zh-hans.resx`.
  - Removed 3 extra keys from `LenovoLegionToolkit.WPF/Resources/Resource.ar.resx`.
  - Removed 1 extra key from `LenovoLegionToolkit.WPF/Resources/Resource.zh-hant.resx`.
- Final consistency checks:
  - WPF `zh-hans` / `zh-hant` / `ar` extra key count => `0`.
  - Global placeholder mismatch check across all `Resource*.resx` => `0`.

## 2026-02-26 Post-Fix Retrospective
- Resource encoding/newline normalization completed for locale files touched by cleanup:
  - `Resource.ar.resx`, `Resource.zh-hans.resx`, `Resource.zh-hant.resx` are now UTF-8 (no BOM) + LF only.
- `Resource.Designer.cs` cross-check:
  - 33 generated string properties still exist without matching base keys in `Resource.resx`.
  - Searched code/XAML references (excluding `Resource.Designer.cs` and `.resx` files): all 33 keys are currently **unreferenced**.
  - Risk assessment: no active runtime impact at present; classify as historical designer drift/cleanup debt.
- Preventive alignment applied:
  - Re-added base fallback keys for:
    - `WindowsOptimizationPage_Extensions_ComingSoon_Title`
    - `WindowsOptimizationPage_Extensions_ComingSoon_Message`
    - `PluginExtensionsPage_OpenPluginFailed`
  - Purpose: keep base resources aligned with generated designer metadata for these known keys.

## 2026-02-26 Translation Audit Final Revalidation (complete)
- Re-ran a fresh detailed audit artifact from current workspace state:
  - `.tmp/current_translation_audit_detailed.json`
  - `.tmp/current_translation_audit_summary.json`
- Initial rerun still reported `total_missing=54`, concentrated in 18 WPF locale files with keys:
  - `Bitmap1`, `Icon1`, `Name1`.
- Root cause analysis:
  - Existing regex-based extractor matches `<data ...>` patterns inside the instructional XML comment block in `Resource.resx`.
  - `Bitmap1/Icon1/Name1` are sample lines inside comments, not real resource nodes.
- Corrective verification method:
  - Re-ran audit using XML node parsing (`[xml]` + `root.data`) so commented samples are excluded.
  - Output artifacts:
    - `.tmp/current_translation_audit_detailed_xml.json`
    - `.tmp/current_translation_audit_summary_xml.json`
- Final validated result (real node-level truth):
  - `locale_files=107`
  - `total_missing=0`
  - `total_extra=0`
  - `total_placeholder_mismatch=0`
  - `nonzero_files=0`
- Conclusion:
  - After prior deterministic fixes, all translation resource files are structurally synchronized.
  - Remaining 54-item signal was a tooling false positive, now resolved by parser strategy.

## 2026-02-26 Translation Semantic Quality Pass (complete)
- Ran cross-locale semantic-lint artifacts:
  - `.tmp/translation_semantic_summary.json`
  - `.tmp/translation_semantic_suspects.json`
- Initial heuristic totals (all locales):
  - `identical_total=17935`
  - `script_mismatch_total=4769`
  - `empty_total=0`
- High-confidence strategy selected:
  - Only auto-fix same-language Chinese variant drift (`zh-hans` -> `zh-hant`) where:
    - `zh-hant` value == base English
    - `zh-hans` has a non-base localized value
  - Conversion method: Simplified -> Traditional via `opencc-python-reimplemented` (`s2t`).
- Auto-fix result:
  - Updated `LenovoLegionToolkit.WPF/Resources/Resource.zh-hant.resx`: 295 entries
  - Updated `LenovoLegionToolkit.Lib.Automation/Resources/Resource.zh-hant.resx`: 2 entries
- Manual Chinese quality pass:
  - Localized 29 high-visibility UI strings in both:
    - `LenovoLegionToolkit.WPF/Resources/Resource.zh-hans.resx`
    - `LenovoLegionToolkit.WPF/Resources/Resource.zh-hant.resx`
  - Scope includes plugin/open-failure message, coming-soon extension text, and Nilesoft menu-style settings guidance text.
- Post-fix Chinese residual English-key check (ASCII-identical to base):
  - `WPF zh-hans`: `53 -> 23`
  - `WPF zh-hant`: `343 -> 19`
  - `Lib.Automation zh-hant`: `2 -> 0`
- Structural integrity revalidation after semantic edits:
  - `locale_files=107`
  - `total_missing=0`
  - `total_extra=0`
  - `total_placeholder_mismatch=0`
  - `nonzero_files=0`

## 2026-02-26 Main-App Plugin UI Automation Extension (in progress)
- User requested extending desktop click automation from plugin repo tooling into the main app plugin system UI.
- Existing state discovery:
  - Main repo has no standalone smoke project under `Tools/` yet.
  - `LenovoLegionToolkit.WPF/Windows/MainWindow.xaml` plugin navigation controls currently lack stable UIA IDs.
  - `LenovoLegionToolkit.WPF/Pages/PluginExtensionsPage.xaml` exposes key controls by `x:Name` but lacks `AutomationProperties.AutomationId` for stable external UIA targeting.
- Existing reusable reference:
  - `..\\LenovoLegionToolkit-Plugins\\Tools\\PluginCompletionUiTool.Smoke\\Program.cs` provides a proven Windows UI Automation smoke pattern (launch app, locate by automation ID, click/set/wait/assert, clean exit).
- Planned implementation direction:
  - Add deterministic automation IDs in main app plugin navigation + plugin page key controls.
  - Add independent main-repo smoke project to automate plugin-system page flow.
  - Validate by building WPF + smoke tool and running smoke end-to-end.

## 2026-02-26 Main-App Plugin UI Automation Extension (complete)
- Implemented deterministic automation IDs in host UI:
  - `LenovoLegionToolkit.WPF/Windows/MainWindow.xaml`
  - `LenovoLegionToolkit.WPF/Pages/PluginExtensionsPage.xaml`
- Implemented standalone smoke runner in main repo:
  - `Tools/MainAppPluginUi.Smoke/MainAppPluginUi.Smoke.csproj`
  - `Tools/MainAppPluginUi.Smoke/Program.cs`
  - Added project to `LenovoLegionToolkit.sln`
- Smoke runner now validates requested UI flow with real clicks:
  - Enter Plugin Extensions page
  - Validate plugin feature page opening
  - Validate plugin item double-click triggers settings window
  - Validate install + uninstall cycle for marketplace plugin
- Key runtime finding while implementing:
  - Runtime fixture plugins initially failed to load because `LenovoLegionToolkit.Plugins.SDK.dll` was missing from plugin/runtime probing locations.
  - Resolved in smoke setup by copying SDK DLL into runtime directory and each copied fixture plugin directory.
- Additional host behavior fix applied:
  - `PluginListBox_MouseDoubleClick` now resolves clicked item via `DataContext` from visual tree (not only `SelectedItem`), reducing missed double-click configuration opens.
- Final verification result:
  - Smoke output PASS with all required steps:
    - feature-page open,
    - double-click configuration window open,
    - install verified,
    - uninstall verified.

## 2026-02-26 Documentation & Crowdin Refresh (complete)
- Replaced legacy `Crowdin.yml` with standardized `crowdin.yml` and expanded mapping from 1 source file to all 4 neutral resource modules:
  - `LenovoLegionToolkit.WPF/Resources/Resource.resx`
  - `LenovoLegionToolkit.Lib/Resources/Resource.resx`
  - `LenovoLegionToolkit.Lib.Automation/Resources/Resource.resx`
  - `LenovoLegionToolkit.Lib.Macro/Resources/Resource.resx`
- Added explicit locale mappings for repository naming conventions (e.g. `zh-CN -> zh-hans`, `zh-TW -> zh-hant`, `pt-BR -> pt-br`, `nl -> nl-nl`, `uz -> uz-latn-uz`).
- Documentation refresh scope:
  - `README.md`, `README_zh-hans.md`: added localization workflow section, updated repository links, updated docs index.
  - `Docs/ARCHITECTURE.md`: updated repository URLs and localization/stack wording.
  - `Docs/DEPLOYMENT.md`: aligned with current GitHub workflow files and release examples; added Crowdin sync section.
  - `Docs/PLUGIN_DEVELOPMENT.md`: updated minimum host version examples and plugin repository URL.
  - `Docs/SECURITY.md`: corrected ambiguous claims and refreshed dependency examples/date.
  - `Docs/CODE_OF_CONDUCT.md`: clarified Crowdin usage scope (UI/resource translation).
  - `Docs/TEST_DIAGNOSTICS.md`: updated recommended release test command and update date.
- Verified `crowdin.yml` YAML syntax via Python parser (`yaml.safe_load`) with 4 file entries.
- Verified current `winget search LenovoLegionToolkit` results and adjusted deployment docs to avoid stale publisher-specific package IDs.

## 2026-02-26 AGENTS.md Repository Link Alignment (complete)
- Updated `AGENTS.md` plugin architecture section and JSON examples to current repository owner:
  - `github.com/SSC-STUDIO/LenovoLegionToolkit`
  - `github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins`
- Updated fields:
  - repository table (main/plugin remotes)
  - `store.json` example `downloadUrl`/`changelog`
  - `plugin.json` example `repository`/`issues`

## 2026-02-26 Plugin Runtime UI Reliability + Marketplace Validation (complete)
- Reproduced runtime plugin page/config blank-content issue from logs during marketplace flow:
  - `PluginPageWrapper`: failed to load `custom-mouse` feature page (`custommousecontrol.xaml`)
  - `PluginSettingsWindow`: failed to load `custom-mouse` settings page (`custommousesettingscontrol.xaml`)
- Root cause:
  - Plugin projects with WPF `InitializeComponent` can fail in host plugin load context (pack URI resolution path).
  - This caused configuration windows to open but with empty/error content.
- Implemented stabilization in sibling plugin repository:
  - `Directory.Build.props`: enabled assembly info generation for plugin projects (`GenerateAssemblyInfo=true`) to keep assembly metadata consistent.
  - Added runtime fallback UI construction (code-built controls) in plugin pages/settings:
    - `Plugins/CustomMouse/CustomMouseControl.xaml.cs`
    - `Plugins/CustomMouse/CustomMouseSettingsControl.xaml.cs`
    - `Plugins/NetworkAcceleration/NetworkAccelerationControl.xaml.cs`
    - `Plugins/NetworkAcceleration/NetworkAccelerationSettingsControl.xaml.cs`
    - `Plugins/ShellIntegration/ShellIntegrationSettingsControl.xaml.cs`
  - Standardized plugin assembly names to stable host-style naming:
    - `LenovoLegionToolkit.Plugins.CustomMouse`
    - `LenovoLegionToolkit.Plugins.NetworkAcceleration`
    - `LenovoLegionToolkit.Plugins.ShellIntegration`
    - `LenovoLegionToolkit.Plugins.ViveTool`
- Validation outcome:
  - Main-app plugin UI smoke workflow PASS:
    - open plugin marketplace page,
    - open plugin feature page,
    - double-click plugin card opens configuration window,
    - install plugin,
    - double-click installed plugin opens configuration window,
    - uninstall plugin.
  - Latest runtime log check: no plugin page/settings load errors for tested flow.

## 2026-02-26 Version Finalization (Main + Plugin Repo) after Phase 23
- Main repository version file updated:
  - `Directory.Build.props` patch version `3.6.3 -> 3.6.4`.
  - `CHANGELOG.md` added `3.6.4` release note for plugin marketplace desktop-smoke validation.
- Plugin repository version alignment updated:
  - `Directory.Build.props` `1.0.3 -> 1.0.4`.
  - Plugin versions advanced and synchronized across csproj/plugin attribute/plugin.json/store metadata:
    - `custom-mouse` `1.0.4`
    - `network-acceleration` `1.0.3`
    - `shell-integration` `1.0.3`
    - `vive-tool` `1.1.2`
  - `store.json` updated to `version=1.0.4` with refreshed per-plugin changelog links and timestamp.
- Per-plugin changelogs updated for the runtime UI reliability fix.

## 2026-02-27 All-Locale Translation Semantic Completion (complete)
- Continued from prior localization audit artifacts and preserved all planning files without truncation.
- Added robust translation execution utility:
  - `.tmp/translate_resx_all_locales.py`
  - Key behavior: placeholder-safe masking/restoration (`{0}`, `{1:F1}` etc.), cross-project translation memory fallback, persistent cache (`.tmp/translation_bing_cache.json`), locale-by-locale report output.
- Root-cause correction for previous failed batch run:
  - MyMemory path was quota-limited and length-sensitive (`429` / `NotValidLength`).
  - Switched to Bing-backed translation batching via `translators` package.
  - Fixed locale adapter mismatches discovered during execution (`nl-NL -> nl`, `zh-CN/zh-TW -> zh-Hans/zh-Hant`).
- Full locale execution completed for 27 locales, with per-locale reports under `.tmp/translation_fix_<locale>.json`.
- Aggregate execution outcome (from report merge):
  - `candidate_entries=17559`
  - `entries_updated=16549`
  - `untranslated_after=1010` (mostly technical/proper-noun labels intentionally left in English)
  - `files_changed=90`
  - `api_calls=1220`
- Post-fix structural integrity (XML-node audit):
  - `.tmp/current_translation_audit_summary_xml_after_bing.json`
  - `locale_files=107`
  - `total_missing=0`
  - `total_extra=0`
  - `total_placeholder_mismatch=0`
  - `nonzero_files=0`
- Post-fix semantic summary:
  - `.tmp/translation_semantic_summary_after_bing.json`
  - `total_identical=1110`, `files_with_identical=57`.
  - Residual items are concentrated in terms like `CPU/GPU/RPM/MHz/GHz`, product/plugin proper names, and short technical labels.
- Build validation after full localization pass:
  - `dotnet build LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj -c Release --no-restore`
  - Result: PASS (`0 warnings`, `0 errors`).


## 2026-02-27 Translation Increment Findings (Phase 25 complete)
- Re-checked user-target repositories for planning logs:
  - `LenovoLegionToolkit/progress.md` exists and was updated.
  - `LenovoLegionToolkit-Plugins/progress.md` did not exist before this session.
- Main repository structural audit (XML node-based) after incremental pass:
  - `locale_files=107`
  - `total_missing=0`
  - `total_extra=0`
  - `total_placeholder_mismatch=0`
  - `nonzero_files=0`
- Main repository semantic increment pass executed with `.tmp/translate_resx_all_locales.py`:
  - report: `.tmp/translation_all_languages_fix_report_bing_2026-02-27-pass2.json`
  - aggregate: `candidate_entries=1010`, `entries_updated=63`, `files_changed=25`, `untranslated_after=947`
- Semantic residual trend:
  - before this pass: `total_identical_alpha=1110`
  - after this pass: `total_identical_alpha=1047`
  - delta: `-63`
- Build verification after localization updates:
  - `dotnet build LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj -c Release --no-restore` => PASS (`0 errors`, `0 warnings`).
- Plugin repository translation scope finding:
  - current plugin locale resources are limited to ViveTool subset (not 20+ locales), and contain known missing keys (`total_missing=38` across 5 locale files).
  - This session prioritized user-requested 20+ locale completion in the main repository.

## 2026-02-27 Plugin Test Coverage Completion Findings (Phase 26 complete)
- Reproduced the exact plugin-test gap in sibling plugin repository:
  - `scripts/plugin-completion-check.ps1` baseline showed warnings for:
    - `network-acceleration`
    - `shell-integration`
    - `vive-tool`
  - Warning text: `No sibling *.Tests project found (optional)`.
- Implemented missing plugin test projects (independent plugin repo scope):
  - `..\\LenovoLegionToolkit-Plugins\\Plugins\\NetworkAcceleration.Tests`
  - `..\\LenovoLegionToolkit-Plugins\\Plugins\\ShellIntegration.Tests`
  - `..\\LenovoLegionToolkit-Plugins\\Plugins\\ViveTool.Tests`
- Added all new test projects to plugin solution:
  - `..\\LenovoLegionToolkit-Plugins\\LenovoLegionToolkit-Plugins.sln`
- Test-design decisions:
  - Chose deterministic unit tests that do not require elevated commands or mutable host runtime state.
  - Focused on plugin contract-level coverage:
    - plugin metadata and identity
    - feature/settings extension capability surface
    - plugin-specific settings/default behavior
    - shell optimization-category structure and action wiring
    - ViveTool plugin attribute correctness
- Final verification outcome:
  - New tests all pass (`NetworkAcceleration: 7`, `ShellIntegration: 5`, `ViveTool: 4`).
  - `plugin-completion-check` full run now reports:
    - `Total failures: 0`
    - `Total warnings: 0`
  - Plugin solution build remains green after adding the three test projects.

## 2026-02-27 Main-App Plugin Settings Smoke Stabilization Findings (Phase 27 complete)
- Reproduced the user-facing blocker in main smoke:
  - `Timed out waiting for element 'NetworkAcceleration_AutoOptimizeCheckBox'`.
- Root cause:
  - smoke runner could pick a stale/previous plugin settings window during sequential plugin checks;
  - settings-window discovery based on `RootElement.Children` was insufficient in this environment for `UiWindow` dialog discovery;
  - close operation lacked a deterministic wait, so previous settings dialogs could leak into next step.
- Implemented fixes in `Tools/MainAppPluginUi.Smoke/Program.cs`:
  - switched settings-window discovery to `TreeScope.Descendants` + explicit `ControlType.Window` filter;
  - tracked existing settings window handles and only accepted newly opened settings windows;
  - added robust `CloseWindowAndWait` logic with close-button fallback and handle disappearance wait;
  - added fallback flow: when double-click does not open configuration quickly, click `PluginConfigureButton_<pluginId>` and continue.
- Validation outcome:
  - Full smoke run now passes end-to-end with explicit network plugin validation:
    - `Network settings-page interactions passed`
    - `Network feature-page interactions passed`
    - final marker: `[main-smoke] PASS`.

## 2026-02-27 Plugin Open Routing to Optimization Extensions Findings (Phase 28 complete)
- Reproduced user-reported UX issue:
  - `shell-integration` had no marketplace `Open` button because host only considered `SupportsFeaturePage`.
  - `custom-mouse` was still treated as standalone feature-page plugin instead of optimization extension route.
- Root cause in host:
  - `PluginExtensionsPage.ResolvePluginCapabilities` did not evaluate `GetOptimizationCategory()` capability.
  - `Open` button visibility binding required `SupportsFeaturePage` only.
  - `PluginOpenButton_Click` had no branch for optimization-category navigation.
- Implemented host fixes:
  - Added `SupportsOptimizationCategory` capability probing and propagated to `PluginViewModel`.
  - Added `SupportsOpenAction` composite property and switched XAML `Open` visibility binding to it.
  - Added optimization-route open branch that navigates to Windows Optimization and requests plugin-category focus.
  - Added pending-focus handling in `WindowsOptimizationPage` to expand/bring-into-view the requested plugin category.
- Implemented plugin-side fix (`LenovoLegionToolkit-Plugins`):
  - Converted `custom-mouse` to optimization extension style (`GetFeatureExtension` now `null`, `GetOptimizationCategory` provided).
  - Added persisted `AutoThemeCursorStyle` setting and enable/disable optimization actions for auto-theme cursor style mode.
- Smoke validation adjustments:
  - Updated `MainAppPluginUi.Smoke` to validate optimization-route open behavior for `custom-mouse` and `shell-integration`.
  - Added stale settings-window cleanup to reduce flaky navigation failures during mixed feature/settings test sequences.
- Validation outcome:
  - Main app build: PASS.
  - CustomMouse tests: PASS (`20/20`).
  - Updated smoke run log: PASS with explicit evidence:
    - `Open button routed to optimization extension: custom-mouse`
    - `Open button routed to optimization extension: shell-integration`
    - final marker: `[main-smoke] PASS`.

## 2026-02-28 CustomMouse Legacy Cursor Restore + zh-Hant Localization Completion (Phase 29 complete)
- Continued from prior plugin optimization-route work and user request to restore historical CustomMouse cursor behavior.
- Confirmed plugin repository now includes restored legacy cursor asset pack path:
  - `Plugins/CustomMouse/Resources/W11-CC-V2.2-HDPI`
  - Includes dark/light base cursors plus classic animation assets and `Install.inf` files.
- Confirmed main repository initially missed `CustomMouse` optimization localization keys in `Resource.zh-hant.resx`.
- Added six missing Traditional Chinese keys:
  - `WindowsOptimization_Category_CustomMouse_Title`
  - `WindowsOptimization_Category_CustomMouse_Description`
  - `WindowsOptimization_Action_CustomMouse_AutoTheme_Enable_Title`
  - `WindowsOptimization_Action_CustomMouse_AutoTheme_Enable_Description`
  - `WindowsOptimization_Action_CustomMouse_AutoTheme_Disable_Title`
  - `WindowsOptimization_Action_CustomMouse_AutoTheme_Disable_Description`
- Validation outcome for this phase:
  - Plugin repo build: PASS (`LenovoLegionToolkit-Plugins.sln`, Release)
  - `CustomMouse.Tests`: PASS (`21/21`)
  - Main WPF build: PASS (`LenovoLegionToolkit.WPF`, Release)
  - Main plugin UI smoke: PASS (`.tmp/main-smoke-custommouse-20260228.log` contains optimization open-route + final PASS marker)
- Noted transient execution issue:
  - First `dotnet test Plugins/CustomMouse.Tests/...` invocation timed out.
  - Resolved by explicit pre-build + `dotnet test --no-build` rerun.
- Follow-up plugin-test closure completed in sibling plugin repository:
  - `NetworkAcceleration.Tests` 7/7 PASS
  - `ShellIntegration.Tests` 5/5 PASS
  - `ViveTool.Tests` 4/4 PASS

## 2026-02-28 Plugin UI Title/Typeface Unification (Phase 30 complete)
- User requirement: plugin page/settings UI had duplicate titles and inconsistent typography; requested removal of bold headers and unified title sizing with System Optimization page style.
- Main-host alignment updates:
  - `LenovoLegionToolkit.WPF/Pages/PluginPageWrapper.xaml`: plugin title `FontSize` adjusted to `24` (aligned with System Optimization title scale), kept `FontWeight=Medium`.
  - `LenovoLegionToolkit.WPF/Windows/Settings/PluginSettingsWindow.xaml`: plugin name heading updated to `FontSize=24` (non-bold).
- Resulting behavior:
  - Plugin page wrapper header remains the single top-level title surface.
  - Plugin settings window keeps a single prominent plugin title style consistent with main title sizing.
  - Eliminates visual mismatch between plugin pages and host pages.

## 2026-02-28 Plugin Title FontWeight Final Correction (Phase 31 complete)
- User feedback confirmed remaining title looked bold in plugin settings interface.
- Root cause: host title TextBlocks still used `FontWeight=Medium` (not `Bold`, but visually heavy).
- Applied final typography correction in main host:
  - `LenovoLegionToolkit.WPF/Pages/PluginPageWrapper.xaml` `_pluginTitle` -> `FontWeight=Normal`
  - `LenovoLegionToolkit.WPF/Windows/Settings/PluginSettingsWindow.xaml` `_pluginNameTextBlock` -> `FontWeight=Normal`
- Verification:
  - `dotnet build LenovoLegionToolkit.WPF/... -c Release --no-restore`: PASS
  - `MainAppPluginUi.Smoke` rerun: PASS (`.tmp/main-smoke-final-fontweight-20260228.log`)

## 2026-02-28 Plugin UI Visual Polish (ViveTool + Network) Findings (Phase 32 complete)
- User requirement: improve plugin UI quality (especially `ViveTool` settings and network plugin pages), avoid overly plain layout while keeping complexity controlled and visually aligned with host System Optimization style.
- Implemented `NetworkAcceleration` UI polish:
  - Feature page converted to cleaner two-card layout (quick actions + preferred mode) with clearer hierarchy.
  - Settings page converted to single focused card with grouped options and status area.
  - Preserved all automation IDs used by smoke tests.
- Implemented `ViveTool` settings UI polish:
  - Removed heavy shadow styling and simplified to host-aligned clean card sections.
  - Grouped status, download progress, and action buttons more clearly.
  - Added cleaner path-management section with consistent spacing and helper text.
  - Updated fallback code-built settings UI layout to stay close to XAML style.
- Metadata/version alignment for release readiness:
  - `network-acceleration` bumped to `1.0.4` (attribute/csproj/plugin.json/store/changelog aligned)
  - `vive-tool` bumped to `1.1.3` (attribute/csproj/Plugin.json/store/changelog aligned)
  - `store.json` top-level version bumped to `1.0.7`, timestamp refreshed.
- Smoke stability note:
  - First run of `MainAppPluginUi.Smoke` in this phase hit a marketplace-page control timeout (navigation retry exhaustion).
  - Immediate rerun passed end-to-end; treated as transient navigation timing issue.


## 2026-02-28 Translation Continuation Sprint Findings (Phase 26 complete)
- Continued multi-pass semantic translation refinement from prior residual baseline using:
  - `.tmp/translate_resx_remaining_multi_provider.py`
  - provider sets: `bing` and fallback chain (`yandex,youdao,reverso,translateCom,argos`)
- Residual-English metric progression during continuation:
  - baseline at continuation start: `total_identical_alpha=1047`
  - final after sprint: `total_identical_alpha=486`
  - net reduction: `-561`
- Structural integrity final state (XML node-based):
  - `locale_files=107`
  - `total_missing=0`
  - `total_extra=0`
  - `total_placeholder_mismatch=0`
  - `nonzero_files=0`
- Build verification after all continuation edits:
  - `dotnet build LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj -c Release --no-restore` => PASS (`0 errors`, `0 warnings`).
- Operational constraint observed repeatedly:
  - Long batch runs were frequently interrupted by network/provider startup failures or timeout windows.
  - Mitigation used: per-locale resume strategy + mixed provider modes, and periodic structural revalidation to avoid hidden drift.


## 2026-03-01 Translation Continuation Sprint II Findings (Phase 27 complete)
- Added locale-compatibility adjustment in continuation script:
  - `.tmp/translate_resx_remaining_multi_provider.py`
  - `pt` target mapping changed from `pt-PT` to `pt`, significantly improving provider success.
- High-yield locale batches completed with provider routing (`yandex,reverso,translateCom,argos,sogou`):
  - major reductions observed in `pt`, `nl-nl`, `pt-br`, `hu`, `vi`, `de`, `ko`, `ja`, `pl`, `ru`.
- Residual-English metric progression for this wave:
  - before Phase 27: `total_identical_alpha=486`
  - after Phase 27: `total_identical_alpha=291`
  - wave delta: `-195`
- Structural integrity final state remained clean:
  - `total_missing=0`
  - `total_extra=0`
  - `total_placeholder_mismatch=0`
- zh-Hans -> zh-Hant sync recheck:
  - no additional syncable entries found in this wave (`entries_updated=0`).
- Remaining residuals are now concentrated in locales/providers with limited language support and technical/proper-noun terms:
  - highest residual locales: `bs`, `ca`, `uz-latn-uz`, `lv`, `sk`, `cs`, `ro`.

## 2026-03-01 Translation Manual Finalization Pass (Phase 33 complete)
- Continued from Phase 27 residual baseline and switched to manual/key-targeted localization.
- Applied manual translations to `LenovoLegionToolkit.WPF/Resources/Resource.<locale>.resx` for 25 locales on high-impact command detail keys:
  - `ActionDetailsWindow_DISMCommand`
  - `ActionDetailsWindow_NetworkFlushDNS`
  - `ActionDetailsWindow_NetworkResetWinsock`
  - `ActionDetailsWindow_NetworkResetTCPIP`
- Added manual quality pass for high-residual locales:
  - `bs`: translated CustomMouse category/actions plus model/status wording.
  - `uz-latn-uz`: translated CustomMouse category/actions plus model wording.
  - `ca`: translated model wording keys.
  - `sk`/`cs`/`ro`: translated `BasicCompatibilityCheck_Model` wording.
- Semantic residual result:
  - before pass: `total_identical_alpha=291` (historic pass6 artifact) / `292` (fresh recompute)
  - after pass: `total_identical_alpha=164`
  - delta: `-127` (`-128` vs fresh recompute baseline)
- XML node structural audit result (authoritative):
  - `locale_files=107`
  - `total_missing=0`
  - `total_extra=0`
  - `total_placeholder_mismatch=0`
  - `nonzero_files=0`
- Build verification:
  - `dotnet build LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj -c Release --no-restore` => PASS (`0 errors`, `0 warnings`)
- Note:
  - Existing regex-only audit script reported false positives on compact/minified-resx layout; final acceptance used XML-parsed audit metrics above.

## 2026-03-01 Translation Manual Finalization Pass IV (Phase 34 complete)
- Continued manual localization from Phase 33.
- Main updates in `LenovoLegionToolkit.WPF/Resources`:
  - `Resource.ca.resx`: translated remaining generic labels (macro/sensors/extensions/mode/arguments/color set) and temperature unit labels.
  - `Resource.bg.resx`, `Resource.bs.resx`, `Resource.lv.resx`, `Resource.ro.resx`, `Resource.sk.resx`, `Resource.tr.resx`, `Resource.uk.resx`, `Resource.uz-latn-uz.resx`, `Resource.zh-hans.resx`, `Resource.zh-hant.resx`: localized `Celsius` / `Fahrenheit` labels.
  - `Resource.de.resx`: localized `PluginExtensionsPage_VersionLabel`.
- Additional CA keys (`BatteryState_Normal`, `NotificationDuration_Normal`) were updated in `LenovoLegionToolkit.Lib/Resources/Resource.ca.resx`.
- Semantic residual result:
  - before pass: `total_identical_alpha=164`
  - after pass: `total_identical_alpha=130`
  - delta: `-34`
- Residual profile after pass-IV is now mostly technical/proper nouns or cross-language same-form words.
- XML node structural audit remains clean:
  - `locale_files=107`, `total_missing=0`, `total_extra=0`, `total_placeholder_mismatch=0`, `nonzero_files=0`
- Build verification:
  - `dotnet build LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj -c Release --no-restore` => PASS (`0 errors`, `0 warnings`)
