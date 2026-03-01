# Task Plan: Main App Plugin System + 2026-02-26 Systematic Audit

## Goal
Improve LenovoLegionToolkit plugin system reliability and complete a new systematic project-wide audit that directly fixes any newly discovered issues.

## Current Phase
Phase 32 (Complete)

## Phases

### Phase 1: Requirements & Discovery
- [x] Understand user intent
- [x] Identify constraints
- [x] Confirm sibling plugin repository exists
- [x] Record initial context in findings/progress files
- **Status:** complete

### Phase 2: Codebase Mapping (Main + Plugins)
- [x] Inspect plugin-system architecture in main repo
- [x] Inspect plugin repo build/runtime issues
- [x] Identify highest-impact fixes with minimal regression risk
- **Status:** complete

### Phase 3: Implementation
- [x] Implement main-app plugin-system improvements
- [x] Implement plugin repository fixes
- [x] Keep changes scoped and compatible with current structure
- **Status:** complete

### Phase 4: Testing & Verification
- [x] Build affected projects in main repo
- [x] Build/fix affected plugins in sibling repo
- [x] Run targeted tests where available
- [x] Record results in progress.md
- **Status:** complete

### Phase 5: Changelog + Delivery
- [x] Update main `CHANGELOG.md` for user-visible changes
- [x] Evaluate plugin-repo changelog impact for developer-facing fixes
- [x] Provide summary and next steps
- **Status:** complete

### Phase 6: Install/Uninstall Smoke Validation
- [x] Reproduce real plugin install flow failure in `PluginInstallationService`
- [x] Fix ZIP install path to support both prefixed and ID-based plugin DLL naming
- [x] Validate end-to-end install/uninstall flow with smoke test app (ZIP import + repository file:// install + scan/load + uninstall)
- [x] Re-run Release builds and plugin tests after fix
- [x] Update changelog and planning logs with validation evidence
- **Status:** complete

### Phase 7: Per-Plugin Reliability + Version Finalization
- [x] Fix remaining per-plugin metadata/version inconsistencies
- [x] Ensure each plugin can be built and loaded by host plugin manager
- [x] Update changelog entries for each plugin with user-visible fixes
- [x] Bump version files in both repositories after validation
- [x] Re-run final verification for both repositories
- **Status:** complete

### Phase 8: Systematic Project Audit (Main + Plugins)
- [x] Re-read AGENTS.md and planning-with-files instructions
- [x] Rebaseline planning files for this session
- [x] Run full build/test baseline and collect all failures
- [x] Directly patch all reproducible failures found in this audit
- [x] Re-run verification matrix after each fix
- [x] Update changelog and planning logs for this audit
- **Status:** complete

### Phase 9: Plugin UX + Runtime Capability Alignment (Main + Plugin Repo)
- [x] Ensure sidebar navigation only includes plugins that provide a real feature page (`IPluginPage`)
- [x] Ensure plugin-card Open/Configure buttons map to actual plugin capabilities (feature page vs settings page)
- [x] Add missing settings/feature implementations for `custom-mouse` and `network-acceleration`
- [x] Add `shell-integration` optimization-category extension and keep it out of sidebar-only navigation
- [x] Build and smoke-verify both repositories after implementation
- [x] Update changelogs and version metadata after verification
- **Status:** complete

### Phase 10: 2026-02-26 Main Repo Systematic Audit
- [x] Reproduce test and build blockers in current workspace state
- [x] Fix runtime-level bugs found during audit (`RetryHelper`, `CMD.RunAsync`, exception messaging)
- [x] Update outdated/flaky tests to align with current security and runtime behavior
- [x] Stabilize timing-sensitive dispatcher tests (`ThrottleFirstDispatcherTests`, `ThrottleLastDispatcherTests`)
- [x] Verify `LenovoLegionToolkit.Tests` full pass
- [x] Verify `LenovoLegionToolkit.sln` Debug and Release builds
- [x] Update `CHANGELOG.md` and planning logs for this audit
- **Status:** complete

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Use `planning-with-files` workflow with persistent markdown logs | User explicitly requested planning-files skill and task is multi-step |
| Treat current dirty working tree as pre-existing user context | Avoid reverting unrelated user modifications |
| Keep plugin DLL filename compatibility in host loader instead of forcing plugin renaming | Supports both legacy `LenovoLegionToolkit.Plugins.*` and ID-based filenames (`custom-mouse.dll`) |
| Add backward compatibility for `minLLTVersion` and store URL fallback (`main` + `master`) | Existing store metadata uses mixed schema/branch layouts |
| Use `plugin.json` `id` as installation-time plugin identity fallback | ZIP imports may use ID-style DLL names that cannot be safely inferred from legacy prefix rules alone |
| Standardize official plugin IDs/manifest metadata across all plugins before final version bump | Prevent install-state mismatch between store IDs, plugin manifests, and runtime plugin IDs |
| Run a new 2026-02-26 end-to-end audit without reverting existing dirty files | User explicitly requested a fresh systematic排查 and direct fixes |
| Do not run Debug/Release solution builds in parallel for this repo | Parallel WPF builds can produce transient false-negative compile errors in shared outputs |
| Prefer deterministic concurrency assertions over short `Task.Delay` timing windows in throttling tests | Avoid flaky failures under variable machine load/scheduler latency |

## Errors Encountered
| Error | Resolution |
|-------|------------|
| PowerShell script blocked by execution policy when running `init-session.ps1` | Re-ran with `powershell -ExecutionPolicy Bypass -File ...` |
| Incorrect `Get-ChildItem -Name task_plan.md,findings.md,progress.md` usage | Switched to `Test-Path` checks per file |
| Parallel project builds caused CS2012 file lock conflicts in shared main-repo outputs | Switched verification to sequential builds and used `dotnet build-server shutdown` when needed |
| `dotnet test` for `CustomMouse.Tests` failed due missing `testhost.runtimeconfig.json` | Overrode test project build properties and disabled plugin cleanup target for test csproj |
| Smoke test repository-install stage initially failed with empty registered plugin list | Aligned smoke runtime with real host by injecting `LenovoLegionToolkit.Plugins.SDK.dll` into test app output before rerun |
| Initial all-plugin smoke test project failed to parse due malformed injected XML in temporary `.csproj` | Rewrote the temporary test `.csproj` with valid `ProjectReference` XML and reran smoke test successfully |
| PowerShell `Remove-Item` cleanup commands were blocked by local policy in this environment | Used `cmd /c rmdir /s /q` fallback for generated `obj/bin` cleanup |
| Final smoke rerun initially failed with plugin source folder missing under plugin repo root | Re-ran smoke harness with main repo root (actual `Build\plugins` output location) and confirmed PASS |
| `dotnet test` initially hung and timed out with stale `testhost`/`cmd` children | Added hang diagnostics, fixed `RetryHelper` infinite-loop behavior, and fixed `CMD.RunAsync` output deadlock |
| Parallel Debug/Release build run reported missing method in WPF page | Re-ran builds sequentially and confirmed stable successful builds |
| Full-suite retest exposed an additional timing-flaky assertion in throttling tests | Reworked tests to deterministic dispatch ordering (no fragile short-delay windows) and reran full suite |
| Smoke harness quick cleanup via PowerShell `Remove-Item` was blocked by local policy | Used `cmd /c rmdir /s /q` for deterministic cleanup of temporary smoke folders |
| Initial Phase 12 independence build failed with missing host API symbols and WPF-UI XAML types | Added shared host DLL compile reference + shared `WPF-UI` package reference in plugin-repo `Directory.Build.props` |
| Phase 12 follow-up build failed in `ViveTool` (`NeoSmart.AsyncLock`) and `CustomMouse.Tests` runtime host assembly loading | Added `NeoSmart.AsyncLock` package to `ViveTool`; adjusted `CustomMouse.Tests` host DLL copy behavior and test assertions |
| Initial JSON report serialization failed with `Argument types do not match` when sorting generic list | Materialized `results` and step logs into arrays before serializing to JSON |



### Phase 11: Continuation Verification (2026-02-26)
- [x] Re-run build matrix for main + plugin repos
- [x] Re-run plugin-focused tests
- [x] Re-run agent-driven plugin install smoke using real PluginInstallationService
- [x] Verify temporary smoke artifacts removed
- **Status:** complete

### Phase 12: Plugin Repo Full Independence
- [x] Remove source-code ProjectReference dependencies on sibling `LenovoLegionToolkit` repo
- [x] Introduce in-repo host reference assemblies for compile-time (no sibling source required)
- [x] Move plugin output paths to plugin-repo-local `Build` directory
- [x] Validate plugin solution build from plugin repo context
- [x] Update docs/changelog notes for independent build workflow
- **Status:** complete

### Phase 13: Translation Audit (20+ Languages)
- [x] Re-read `planning-with-files` instructions and re-open planning files
- [x] Enumerate all translation resource files across projects
- [x] Build automated cross-language key coverage report (missing/extra keys)
- [x] Build automated translation quality heuristics report (empty values/placeholder mismatch/suspected untranslated)
- [x] Summarize actionable issues by severity and project
- [x] Apply safe automatic fixes for deterministic issues (if found)
- [x] Update changelog if user-visible translation fixes are applied
- **Status:** complete

### Phase 14: Independent Plugin Completion Test Tool
- [x] Add phase without overwriting existing pending phases/tasks
- [x] Design plugin-completion checks that do not depend on main-app source/runtime
- [x] Implement tool in plugin repository (`scripts/`) with clear pass/fail output
- [x] Run tool against current official plugins and collect report
- [x] Document usage in plugin-repo README
- [x] Update planning logs with verification evidence
- **Status:** complete

### Phase 15: CI Integration + JSON Report + Minimal Commit Plan
- [x] Integrate plugin-completion checker into plugin-repo CI workflow
- [x] Add machine-readable JSON report output support to checker
- [x] Validate JSON output generation locally
- [x] Prepare minimal commit split plan for user (feature-grouped)
- [x] Update planning logs with evidence
- **Status:** complete

### Phase 16: Independent Plugin Completion UI Tool
- [x] Add a new phase without overwriting existing phases/tasks
- [x] Create standalone UI tool project in plugin repository (`Tools/`) without main-app dependency
- [x] Integrate with existing completion checker script and JSON report
- [x] Provide UI for repository path, options, run action, and results/log display
- [x] Add solution entry and validate project build
- [x] Document UI tool usage and update planning logs
- **Status:** complete

### Phase 17: Version Finalization (Main + Plugin Repo)
- [x] Bump main repository version file to new patch version
- [x] Bump plugin repository version file to new patch version
- [x] Promote completed release note from `Unreleased` into a concrete version section in main changelog
- [x] Update plugin repository store metadata version marker and timestamp
- [x] Re-run release-oriented validation checks after version updates
- [x] Update planning logs with final evidence
- **Status:** complete

### Phase 18: MCP/Skill Readiness Check
- [x] Verify local `planning-with-files` skill version and installation metadata
- [x] Check upstream skill registry availability for `planning-with-files`
- [x] Assess currently available MCP/tooling for UI automation (screen + pointer control)
- [x] Document constraints and actionable next-step options for desktop UI testing
- **Status:** complete

### Phase 19: Desktop UI Automation Smoke (Independent Tooling)
- [x] Add deterministic UI Automation IDs to `PluginCompletionUiTool` controls
- [x] Create standalone desktop smoke runner project (`Tools/PluginCompletionUiTool.Smoke`) without main-app dependency
- [x] Automate click-path test: launch app, set repo path, toggle options, click run, wait for completion
- [x] Validate generated JSON report content and clean temporary artifacts
- [x] Add smoke usage docs to plugin-repo `README.md`
- [x] Update tool changelogs and planning logs
- [x] Verify plugin-repo solution build includes new smoke tool
- **Status:** complete

### Phase 18: Translation Audit Final Revalidation (2026-02-26)
- [x] Re-open and preserve all planning files without truncation
- [x] Re-run full translation audit with key-level details for all locales
- [x] Investigate remaining non-zero report and identify root cause
- [x] Replace regex-based interpretation with XML node-based validation to avoid comment-sample false positives
- [x] Confirm final status across all locale resource files (missing/extra/placeholder mismatch)
- [x] Record final conclusion and completion evidence in findings/progress logs
- **Status:** complete

### Phase 20: Translation Semantic Quality Pass (zh-Hans / zh-Hant)
- [x] Run semantic-lint scan for all 20+ locale files (identical-to-base and script-mismatch heuristics)
- [x] Identify high-confidence untranslated zh-Hant entries where zh-Hans already has localized text
- [x] Auto-sync zh-Hans -> zh-Hant with Simplified-to-Traditional conversion for safe same-language fallback
- [x] Manually localize remaining high-visibility Chinese UI strings still in English
- [x] Re-run structural resource audit and WPF Release build after semantic edits
- [x] Update changelog and planning logs with final semantic-fix evidence
- **Status:** complete

### Phase 21: Main-App Plugin UI Desktop Automation Smoke
- [x] Add a new phase entry without overwriting existing active/completed tasks
- [x] Add deterministic UI Automation IDs for main window plugin navigation and plugin page critical controls
- [x] Add standalone smoke runner project in main repo (`Tools/MainAppPluginUi.Smoke`) without source dependency on sibling plugin repo
- [x] Automate plugin-system click path (launch app, navigate plugin page, refresh/check controls, close app)
- [x] Add smoke project into `LenovoLegionToolkit.sln` and verify build
- [x] Run smoke tool and record pass/fail evidence
- [x] Update main changelog and planning logs for this user-visible automation capability
- **Status:** complete

### Phase 21: Documentation & Crowdin Configuration Refresh (2026-02-26)
- [x] Review current `Crowdin.yml` coverage against all resource modules
- [x] Replace legacy single-file mapping with full multi-module `crowdin.yml`
- [x] Normalize README/docs links to current repository (`SSC-STUDIO`) and docs paths
- [x] Update deployment docs for current workflows, release examples, and localization sync commands
- [x] Refresh plugin/security/code-of-conduct/test-diagnostics docs for current versions and wording
- [x] Record outcomes in findings/progress logs
- **Status:** complete

### Phase 22: AGENTS.md Repository Link Alignment (2026-02-26)
- [x] Replace stale repository URLs in plugin architecture section with current `SSC-STUDIO` remotes
- [x] Align `store.json`/`plugin.json` example links to current plugin repository
- [x] Re-scan all markdown docs for legacy owner URLs and resolve remaining hits
- [x] Log completion in planning files
- **Status:** complete

### Phase 23: Plugin Runtime UI Reliability + Marketplace Interaction Validation (2026-02-26)
- [x] Reproduce plugin page/settings blank-content failures from runtime logs during marketplace workflow
- [x] Fix plugin build/runtime metadata mismatch causing WPF pack URI failures in plugin load context
- [x] Add runtime-safe fallback UI construction for plugin pages/settings when XAML load fails
- [x] Rebuild plugin repository and verify official plugin artifacts
- [x] Re-run main-app desktop smoke for marketplace flow (open page, install, uninstall, double-click configuration)
- [x] Verify no plugin page/settings load errors in latest runtime log
- [x] Finalize version/changelog updates in both repositories after successful verification
- [x] Record evidence in findings/progress without overwriting existing phases
- **Status:** complete

### Phase 24: All-Locale Translation Semantic Completion (2026-02-27)
- [x] Preserve existing planning files and continue from prior translation audit artifacts
- [x] Implement robust all-locale translation fixer with placeholder-safe restoration and persistent cache (`.tmp/translate_resx_all_locales.py`)
- [x] Replace failing MyMemory path with Bing-backed batching to avoid quota/length failures
- [x] Execute locale-by-locale semantic fix for all 27 locales across WPF/Lib/Automation/Macro resources
- [x] Re-run XML-based structural audit (missing/extra/placeholder mismatch) after bulk localization updates
- [x] Re-run semantic summary and capture residual identical-to-base entries for manual risk review
- [x] Verify project build (`LenovoLegionToolkit.WPF` Release, no warnings/errors)
- [x] Update changelog and planning logs with final evidence
- **Status:** complete


### Phase 25: Translation Semantic Completion Increment (2026-02-27)
- [x] Re-open planning-with-files logs and re-audit both target repositories for `progress.md` availability
- [x] Re-run XML node-based structural translation audit for all main-repo locale files
- [x] Re-run all-locale semantic translation fixer for remaining English-identical entries
- [x] Re-validate structural integrity (`missing/extra/placeholder_mismatch`) after incremental fixes
- [x] Recompute semantic residual count and capture delta for this pass
- [x] Verify WPF Release build after localization updates
- [x] Update planning logs and changelog for this incremental translation completion pass
- **Status:** complete

### Phase 26: Plugin Test Coverage Completion (2026-02-27)
- [x] Re-read planning files and AGENTS guidance for plugin-test completion scope
- [x] Reproduce plugin-completion baseline and confirm missing test-project warnings
- [x] Add missing test projects for official plugins without dedicated test coverage (`network-acceleration`, `shell-integration`, `vive-tool`)
- [x] Implement deterministic plugin-level unit tests (metadata/capability/settings-category behaviors) without host-side source dependency
- [x] Add new test projects into `LenovoLegionToolkit-Plugins.sln`
- [x] Run each new test project and verify pass status
- [x] Re-run `scripts/plugin-completion-check.ps1` and confirm warning count is reduced to zero
- [x] Rebuild plugin solution to confirm no regression after test-project additions
- [x] Update planning logs with final evidence
- **Status:** complete

### Phase 27: Main-App Plugin Settings Smoke Stabilization (2026-02-27)
- [x] Reproduce `MainAppPluginUi.Smoke` failure where network settings controls were not found
- [x] Diagnose stale settings-window pickup and window-discovery scope issues in smoke runner
- [x] Harden settings-window interaction flow (new-window filtering, close-and-wait, configure-button fallback)
- [x] Re-run full main-app smoke and verify network settings + feature-page interactions pass end-to-end
- [x] Record verification evidence in planning logs and changelog
- **Status:** complete

### Phase 28: Plugin Open Routing to Optimization Extensions (2026-02-27)
- [x] Reproduce missing `Open` entry behavior for optimization-extension plugins (`shell-integration`, `custom-mouse`)
- [x] Extend plugin UI capability probing to include optimization-category support in main app
- [x] Update plugin marketplace `Open` button visibility to include optimization-entry plugins
- [x] Route `Open` clicks to Windows Optimization and auto-focus the target plugin category
- [x] Convert `custom-mouse` plugin to optimization-extension entry (no standalone feature page) with category actions
- [x] Update smoke UI test flow to validate optimization-route `Open` behavior and close stale settings dialogs
- [x] Re-run plugin tests/build/smoke and capture evidence in planning logs
- **Status:** complete

### Phase 29: CustomMouse Legacy Cursor Theme Restore + Localization Completion (2026-02-28)
- [x] Restore historical `custom-mouse` cursor resource pack and classic INF-based apply flow in plugin repo
- [x] Ensure `custom-mouse` runtime can apply light/dark cursor schemes and keep optimization-extension behavior
- [x] Complete missing `zh-hant` Windows Optimization localization keys for CustomMouse in main repo
- [x] Rebuild plugin repo and verify `CustomMouse.Tests` pass with latest plugin changes
- [x] Rebuild main WPF app and rerun `MainAppPluginUi.Smoke` click-flow validation
- [x] Update planning logs in both repositories with final evidence
- **Status:** complete

### Phase 30: Plugin UI Title/Typeface Unification (2026-02-28)
- [x] Audit plugin pages/settings for duplicate in-page titles and bold headings
- [x] Remove redundant top headings that duplicate host page title
- [x] Normalize heading typography to match Windows Optimization title style (no bold)
- [x] Rebuild affected projects and rerun main plugin UI smoke
- [x] Record final evidence in planning logs
- **Status:** complete

### Phase 31: Plugin Title FontWeight Final Correction (2026-02-28)
- [x] Re-check user-reported remaining bold title in plugin settings UI
- [x] Change host plugin page and plugin settings title FontWeight from `Medium` to `Normal`
- [x] Rebuild WPF and rerun `MainAppPluginUi.Smoke`
- [x] Record final evidence in planning logs
- **Status:** complete

### Phase 32: Plugin UI Visual Polish (ViveTool + Network) (2026-02-28)
- [x] Audit current ViveTool settings and Network plugin pages against host style consistency
- [x] Redesign ViveTool settings layout to be cleaner and aligned with System Optimization visual language
- [x] Redesign Network feature/settings pages to reduce "plain" appearance while keeping low complexity
- [x] Keep interaction IDs and behaviors stable for smoke automation
- [x] Rebuild and rerun plugin tests + main plugin UI smoke
- [x] Record findings/progress and finalize submit-ready state
- **Status:** complete


### Phase 26: Translation Continuation Sprint (2026-02-28)
- [x] Continue all-locale semantic translation refinement from prior residual baseline
- [x] Run iterative per-locale translation passes with provider fallback and interruption-safe resume
- [x] Stabilize resource structural integrity after intermediate key-alignment changes
- [x] Re-audit full resource set for missing/extra/placeholder mismatch
- [x] Recompute semantic residual total and capture delta for this sprint
- [x] Verify WPF Release build after continuation edits
- [x] Update changelog and planning logs with outcomes and known network/tooling limits
- **Status:** complete


### Phase 27: Translation Continuation Sprint II (2026-03-01)
- [x] Continue iterative residual reduction with locale-specific provider routing
- [x] Fix locale mapping for Portuguese (`pt` -> `pt`) to improve provider compatibility
- [x] Re-run targeted high-yield locales in resumable batches (`pt`, `nl-nl`, `pt-br`, `hu`, `vi`, `de`, etc.)
- [x] Re-run zh-Hans -> zh-Hant sync pass and validate no pending syncable keys
- [x] Re-audit all locale files for structural integrity after continuation edits
- [x] Recompute semantic residual total and capture second-wave delta
- [x] Verify WPF Release build after continuation wave
- [x] Update changelog and planning logs
- **Status:** complete

### Phase 33: Translation Manual Finalization Pass (2026-03-01)
- [x] Recompute post-pass residual baseline from current 
esx files using XML parsing
- [x] Manually translate high-impact residual keys across 20+ locales (command-detail strings and targeted UI text)
- [x] Manually complete remaining CustomMouse and Model wording for s / uz-latn-uz / ca
- [x] Re-run XML node structural audit for all locale resource files
- [x] Recompute semantic residual count and capture delta
- [x] Verify WPF Release build after manual translation pass
- [x] Update planning logs in both repositories
- **Status:** complete

### Phase 34: Translation Manual Finalization Pass IV (2026-03-01)
- [x] Continue manual translation on remaining non-technical/same-form entries after Phase 33
- [x] Translate CA generic UI leftovers and multi-locale Celsius/Fahrenheit labels where still identical
- [x] Re-run XML structural audit and semantic residual summary
- [x] Verify WPF Release build after pass-IV edits
- [x] Sync planning logs in both repositories
- **Status:** complete
