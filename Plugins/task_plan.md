# Task Plan: Plugin Repo Translation Audit (2026-02-27)

## Goal
Establish planning-with-files logs in `LenovoLegionToolkit-Plugins` and audit current translation coverage for plugin resources.

## Current Phase
Phase 9 (Complete)

## Phases

### Phase 1: Planning File Bootstrap
- [x] Verify whether `progress.md` exists in plugin repository root
- [x] Create `task_plan.md`, `findings.md`, and `progress.md` for this repository
- **Status:** complete

### Phase 2: Translation Coverage Audit
- [x] Enumerate plugin `Resource*.resx` files
- [x] Run XML node-based missing/extra/placeholder audit
- [x] Summarize current locale coverage and missing-key totals
- **Status:** complete

### Phase 3: Session Handoff
- [x] Record outcomes in planning files
- [x] Keep plugin repo state unchanged (analysis-only)
- **Status:** complete

### Phase 4: CustomMouse Optimization Extension Alignment (2026-02-27)
- [x] Convert `custom-mouse` from standalone feature-page route to optimization-extension route
- [x] Add plugin-provided optimization category/actions for cursor auto-theme mode
- [x] Keep plugin settings page available for detailed configuration
- [x] Update plugin metadata/version/changelog/store manifest for `1.0.5`
- [x] Update and run `CustomMouse.Tests` for new plugin capability contract
- **Status:** complete

### Phase 5: CustomMouse Legacy Cursor Theme Restore + Validation (2026-02-28)
- [x] Restore historical cursor resources (`W11-CC-V2.2-HDPI`) and classic INF assets for both dark/light themes
- [x] Implement runtime light/dark cursor apply flow with INF-first and registry fallback strategy
- [x] Ensure settings UI exposes auto-theme toggle + manual apply action for cursor scheme
- [x] Bump CustomMouse plugin metadata/store/changelog from `1.0.5` to `1.0.6`
- [x] Rebuild plugin solution and rerun `CustomMouse.Tests` with updated behavior
- [x] Run remaining official plugin tests (`network-acceleration`, `shell-integration`, `vive-tool`) to close plugin-test scope
- [x] Record verification evidence in planning files
- **Status:** complete

### Phase 6: Plugin UI Title/Typeface Unification (2026-02-28)
- [x] Audit plugin controls/pages for duplicate top titles and bold headings
- [x] Remove duplicated in-page titles that conflict with host wrapper titles
- [x] Normalize text style to non-bold, consistent title sizing with host optimization-page convention
- [x] Rebuild plugin solution and rerun plugin tests as needed
- [x] Record findings/progress evidence
- **Status:** complete

### Phase 7: Plugin UI Visual Polish (ViveTool + Network) (2026-02-28)
- [x] Audit current ViveTool settings and Network plugin pages against host style consistency
- [x] Redesign ViveTool settings layout to be cleaner and aligned with System Optimization visual language
- [x] Redesign Network feature/settings pages to reduce "plain" appearance while keeping low complexity
- [x] Keep fallback UI paths in sync with XAML improvements
- [x] Rebuild plugin solution and rerun official plugin tests
- [x] Record findings/progress and finalize submit-ready state
- **Status:** complete

### Phase 8: Plugin Multi-Language Finalization + Local Action Verification (2026-02-28)
- [x] Ensure plugin metadata (`Name`/`Description`) for CustomMouse/Network/Shell follows current UI culture
- [x] Remove remaining ViveTool fallback hardcoded text and localize file-dialog filters
- [x] Update plugin tests to assert localized metadata sources instead of hardcoded English literals
- [x] Rebuild solution and rerun all official plugin tests
- [x] Run UI automation smoke (`PluginCompletionUiTool.Smoke`) for real click flow
- [x] Execute local action-level verification for CustomMouse cursor apply + ShellIntegration enable/disable flow
- [x] Record verification artifacts/results in planning files
- **Status:** complete

### Phase 9: ViveTool Bundled Runtime + Custom Path Override (2026-02-28)
- [x] Add bundled ViveTool runtime files (`ViVeTool.exe` + required dependencies) into plugin source
- [x] Update ViveTool runtime path resolution: custom path first, bundled runtime second, fallback discovery last
- [x] Keep settings page custom-path capability unchanged and clarify text for bundled-default behavior
- [x] Add tests covering bundled-path default and custom-path override behavior
- [x] Sync version/changelog/store metadata for ViveTool release
- [x] Rebuild and run full plugin tests + ViveTool completion check
- [x] Record verification artifacts/results in planning files
- **Status:** complete

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| One PowerShell command for verifier setup was blocked by policy | 1 | Switched to non-destructive directory creation flow (no cleanup action in same command) |
| Temporary verifier run failed (`runtimeconfig.json` missing) due repo-wide cleanup target behavior | 1 | Marked temporary verifier csproj with `<IsPluginToolProject>True</IsPluginToolProject>` |
| Temporary verifier failed to load `LenovoLegionToolkit.Lib` | 1 | Added explicit reference to `Dependencies/Host/LenovoLegionToolkit.Lib.dll` with `<Private>true</Private>` |
