# MainAppPluginUi.Smoke Review and Fix Log

## Background
This document is the persistent engineering journal for hardening `Tools/MainAppPluginUi.Smoke/Program.cs`. It exists because the user explicitly requested a detailed bug-fix record that is maintained during the work, not reconstructed afterward.

## Scope
- Reviewed file: `Tools/MainAppPluginUi.Smoke/Program.cs`
- Supporting tracking files:
  - `task_plan.md`
  - `findings.md`
  - `progress.md`
  - `CHANGELOG.md` (final concise user-facing summary only)

## Bug Register
| ID | Status | Severity | Source location | Issue | Risk / impact | Planned round |
|----|--------|----------|-----------------|-------|---------------|---------------|
| BR-01 | fixed | high | `Tools/MainAppPluginUi.Smoke/Program.cs:56-60`, `Tools/MainAppPluginUi.Smoke/Program.cs:230-243` | Runtime plugin directory is constructed as `<runtime>/Build/plugins`, which likely mismatches actual runtime layout when the app already runs from a publish/output directory. | Smoke runs can silently miss runtime plugins, skip install-state preseeding, or prepare fixtures in the wrong location. | Round 1 |
| BR-02 | fixed | high | `Tools/MainAppPluginUi.Smoke/Program.cs:436-531` | Runtime fixture preparation deletes target plugin directories before replacement succeeds. | A failed copy can leave the runtime plugin tree partially destroyed and non-recoverable for the current run. | Round 1 |
| BR-03 | fixed | high | `Tools/MainAppPluginUi.Smoke/Program.cs:276-386` | `settings.json` restore writes the whole original file back. | Legitimate runtime changes made while the smoke run is active can be lost. | Round 1 |
| BR-04 | fixed | medium | `Tools/MainAppPluginUi.Smoke/Program.cs:1916-2006` | Screenshot helper discovery is hard-coded to `.codex`, and screenshot execution does not validate success/output presence. | Verification artifacts may silently be missing even though the smoke log claims screenshots were captured. | Round 1 |
| BR-05 | fixed | medium | `Tools/MainAppPluginUi.Smoke/Program.cs:1751-1790`, `Tools/MainAppPluginUi.Smoke/Program.cs:1842-1859`, `Tools/MainAppPluginUi.Smoke/Program.cs:2089-2111` | Several waits only require an element to exist, not to be visible/live/interactable. | The smoke flow can proceed with stale or offscreen elements, causing flaky or misleading success/failure states. | Round 1 |
| BR-06 | fixed | high | `Tools/MainAppPluginUi.Smoke/Program.cs:1103-1127` | Missing settings windows are conditionally skipped for known plugins. | Real regressions can be hidden and reported as acceptable skips. | Round 1 |
| BR-07 | fixed | medium | `Tools/MainAppPluginUi.Smoke/Program.cs:1134-1177` | `TestDoubleClickOpensSettings(...)` still falls back from double-click failure to Configure for every `TimeoutException` / `InvalidOperationException`, which can hide regressions in the intended double-click interaction path. | The smoke tool may report success for a broken double-click gesture as long as Configure still works. | Round 2 |
| BR-08 | fixed | medium | `Tools/MainAppPluginUi.Smoke/Program.cs:541-597` | Runtime fixture rollback excludes the copied runtime-level `LenovoLegionToolkit.Plugins.SDK.dll`, which is still overwritten in place without backup/restore. | A failed or temporary smoke run can leave the runtime SDK assembly mutated after completion. | Round 2 |
| BR-09 | fixed | low | `Tools/MainAppPluginUi.Smoke/Program.cs:1157-1160`, `Tools/MainAppPluginUi.Smoke/Program.cs:1207` | Dead `CanSkipMissingOptimizationSettingsWindow(...)` / `CanSkipMissingMarketplaceSettingsWindow(...)` helpers remain after skip removal. | Dead code makes the review log and source misleading and suggests behavior that no longer exists. | Round 2 |
| BR-10 | fixed | low | `Tools/MainAppPluginUi.Smoke/Program.cs:910-924`, `Tools/MainAppPluginUi.Smoke/Program.cs:1134-1155` | `TestDoubleClickOpensSettingsOrSkip(...)` is now just a pass-through wrapper around `TestDoubleClickOpensSettings(...)`. | Unnecessary wrapper code obscures the actual behavior and preserves outdated naming from removed skip logic. | Round 3 |
| BR-11 | fixed | low | `Tools/MainAppPluginUi.Smoke/Program.cs:444-497`, `Tools/MainAppPluginUi.Smoke/Program.cs:541-570` | `PrepareRuntimePluginFixtures(...)` still computes and uses `sdkDllCandidates` even though runtime-root SDK handling has been split into `PrepareRuntimeSdkFixture(...)`. | Duplicated source selection logic increases drift risk and makes fixture behavior harder to reason about. | Round 3 |
| BR-12 | fixed | medium | `Tools/MainAppPluginUi.Smoke/Program.cs:218-280` | Runtime directory discovery can pick an arbitrary nested directory that merely contains `Lenovo Legion Toolkit.dll`, and startup prefers `dotnet <dll>` even when the directory is not a runnable app output. | Smoke runs can launch from the wrong folder, fail to start the app, or validate a stale/non-runnable artifact tree. | Round 4 |

## Repair Rounds

### Round 0 - Baseline capture
- Status: completed
- Goals:
  - Re-base persistent planning files to this remediation effort.
  - Create this dedicated engineering journal.
  - Confirm the currently known bug register and expected verification checkpoints.
- Outcome:
  - `task_plan.md`, `findings.md`, and `progress.md` were re-based to the smoke-tool remediation scope.
  - This dedicated remediation log was created and initialized with the approved starting bug register.
- Known verification baseline:
  - No code build was required for the documentation-only baseline step.

### Round 1 - Runtime path + fixture recovery + targeted settings restore + screenshot validation + stricter UIA/skip behavior
- Status: completed
- Bugs addressed:
  - BR-01
  - BR-02
  - BR-03
  - BR-04
  - BR-05
  - BR-06
- Changes made:
  - Replaced the hard-coded `<runtime>/Build/plugins` assumption with `ResolveRuntimePluginsDirectory(...)`, which now prefers `<runtime>/plugins` and only falls back to `<runtime>/Build/plugins` when needed.
  - Converted runtime fixture preparation to return tracked fixture state and added `RestoreRuntimePluginFixtures(...)` so existing plugin directories are backed up before replacement and restored in `finally`.
  - Changed settings mutation tracking from whole-file snapshot/restore to targeted property capture/restoration for `InstalledExtensions` and `PendingDeletionExtensions`.
  - Switched screenshot helper discovery to support `LLT_SMOKE_SCREENSHOT_SCRIPT`, `%USERPROFILE%\\.claude\\...`, and the legacy `%USERPROFILE%\\.codex\\...` path.
  - Made screenshot execution fail when the helper process cannot start, times out, exits non-zero, or does not produce the expected output file.
  - Tightened `WaitForAutomationId(...)`, `WaitForAutomationIdOrNames(...)`, `Click(...)`, and `MouseClick(...)` so they require visible/enabled/non-empty-bounds elements before interaction.
  - Removed the prior skip-on-missing-settings-window fallbacks so marketplace/optimization settings window regressions now surface instead of being silently skipped.
- Newly discovered implementation bugs in this round:
  - Initial edits accidentally dropped method signatures/body lines around `CreateMainAppStartInfo(...)` and `ParseSettingsRoot(...)`, causing compile errors. These were repaired immediately before verification.
- Verification evidence:
  - Build: `dotnet build Tools/MainAppPluginUi.Smoke/MainAppPluginUi.Smoke.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false`
  - Result: Passed with `0` warnings / `0` errors.
  - Notes: No focused smoke execution has been run yet in this round; build verification confirms the remediation compiles cleanly.


### Round 2 - Strict double-click validation + runtime SDK rollback + dead-code cleanup
- Status: completed
- Bugs addressed:
  - BR-07
  - BR-08
  - BR-09
- Changes made:
  - Removed the catch-and-fallback path from `TestDoubleClickOpensSettings(...)` so a broken double-click interaction now fails instead of being silently converted into a Configure-button success.
  - Added `TestConfigureOpensSettings(...)` as a separate explicit validation path, preserving Configure coverage without weakening the double-click assertion.
  - Added `RuntimeFileFixtureState`, `PrepareRuntimeSdkFixture(...)`, and `RestoreRuntimeFileFixture(...)` so the runtime-root `LenovoLegionToolkit.Plugins.SDK.dll` overwrite is now covered by backup/restore just like plugin directories.
  - Removed the obsolete skip helper methods left behind after the earlier skip-path removal.
- Newly discovered implementation bugs in this round:
  - Reintroducing runtime SDK backup/restore initially failed to compile because `CleanupFixtureDirectory(...)` had been removed earlier and needed to be restored.
- Verification evidence:
  - Build: `dotnet build Tools/MainAppPluginUi.Smoke/MainAppPluginUi.Smoke.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false`
  - Result: Passed with `0` warnings / `0` errors.
  - Notes: This round focused on code-level hardening and build verification.

### Round 3 - Low-risk cleanup after review 3
- Status: completed
- Bugs addressed:
  - BR-10
  - BR-11
- Changes made:
  - Removed the obsolete `TestDoubleClickOpensSettingsOrSkip(...)` wrapper and routed the remaining caller directly to `TestDoubleClickOpensSettings(...)`.
  - Removed the duplicated SDK source-selection and copy logic from `PrepareRuntimePluginFixtures(...)`, leaving runtime-root SDK handling solely in `PrepareRuntimeSdkFixture(...)`.
- Newly discovered implementation bugs in this round:
  - The first cleanup build failed because one leftover `sdkDllCandidates` reference remained in `PrepareRuntimePluginFixtures(...)`; it was removed immediately and the follow-up build passed.
- Verification evidence:
  - Build: `dotnet build Tools/MainAppPluginUi.Smoke/MainAppPluginUi.Smoke.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false`
  - Result: Passed with `0` warnings / `0` errors.
  - Notes: This round was a source-cleanup follow-up after the third review.

## Newly Discovered Bugs During Implementation
- Round 1: While refactoring helper methods, transient edit mistakes removed the `CreateMainAppStartInfo(...)` method body prelude (`dllPath`) and the `ParseSettingsRoot(...)` signature, which caused a temporary compile break. The mistakes were fixed before the verification build and are recorded here as implementation-time defects caught during the round.
- New review round: `TestDoubleClickOpensSettings(...)` still downgrades a failed double-click into a Configure-button success path, so the code does not yet strictly verify the gesture-specific regression it names.
- New review round: runtime fixture restore still leaves the runtime-root `LenovoLegionToolkit.Plugins.SDK.dll` overwrite outside the backup/rollback path.
- New review round: the old skip helper methods remain as dead code after the behavioral skip removal.
- Round 2: the first compile attempt after restoring runtime SDK rollback failed because `CleanupFixtureDirectory(...)` had been removed and needed to be restored alongside the new file-fixture helpers.
- Round 3 cleanup: the first compile attempt after removing duplicated SDK source-selection logic failed because one leftover `sdkDllCandidates` reference remained in `PrepareRuntimePluginFixtures(...)`; it was caught immediately by the build and removed.

## Verification Evidence
### Round 0
- Planning/documentation baseline completed by direct source inspection of `Tools/MainAppPluginUi.Smoke/Program.cs` and creation of the persistent remediation documents.

### Round 1
- Build command: `dotnet build Tools\\MainAppPluginUi.Smoke\\MainAppPluginUi.Smoke.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false`
- Build result: success, `0` warnings, `0` errors.
- Evidence source: local CLI build output in this session.

### Round 2
- Build command: `dotnet build Tools\\MainAppPluginUi.Smoke\\MainAppPluginUi.Smoke.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false`
- Build result: success, `0` warnings, `0` errors.
- Evidence source: local CLI build output in this session.

### Round 3
- Build command: `dotnet build Tools\\MainAppPluginUi.Smoke\\MainAppPluginUi.Smoke.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false`
- Build result: success, `0` warnings, `0` errors.
- Evidence source: local CLI build output in this session.

## Remaining Risks / Deferred Items
- BR-01 through BR-12 are implemented in `Tools/MainAppPluginUi.Smoke/Program.cs`; no additional remediation round is currently queued in this log.
- Verification breadth is no longer limited to build-only evidence: a 2026-03-24 single-plugin smoke run passed for `shell-integration`, while `custom-mouse` and the default plugin set exposed stable end-to-end failures that still need follow-up.
- Final `CHANGELOG.md` update is now backed by real smoke evidence from the 2026-03-24 validation pass rather than build-only verification.

## End-to-End Smoke Evidence (2026-03-24)
### Environment
- Host machine: current local Windows workstation used for this repository session.
- Repository root: `C:\Users\96152\My-Project\Active\Software\LenovoLegionToolkit`
- Runtime root used by smoke: `LenovoLegionToolkit.WPF\bin\Release\net10.0-windows\win-x64`
- Fixture source detected by smoke: `C:\Users\96152\My-Project\Active\Software\LenovoLegionToolkit-Plugins\Build\plugins`
- Build precheck: `dotnet build Tools\MainAppPluginUi.Smoke\MainAppPluginUi.Smoke.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false` → passed with `0` warnings / `0` errors.

### Commands executed
1. `dotnet build Tools\MainAppPluginUi.Smoke\MainAppPluginUi.Smoke.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false`
2. `LLT_SMOKE_PLUGIN_IDS=custom-mouse dotnet run --project Tools\MainAppPluginUi.Smoke\MainAppPluginUi.Smoke.csproj -c Release --no-build -- <repo-root>`
3. `LLT_SMOKE_PLUGIN_IDS=shell-integration dotnet run --project Tools\MainAppPluginUi.Smoke\MainAppPluginUi.Smoke.csproj -c Release --no-build -- <repo-root>`
4. `LLT_SMOKE_PLUGIN_IDS=network-acceleration dotnet run --project Tools\MainAppPluginUi.Smoke\MainAppPluginUi.Smoke.csproj -c Release --no-build -- <repo-root>`
5. `dotnet run --project Tools\MainAppPluginUi.Smoke\MainAppPluginUi.Smoke.csproj -c Release --no-build -- <repo-root>`

### Coverage matrix
| Plugin set | Entry path reached | Result | Evidence |
|---|---|---|---|
| `custom-mouse` | Marketplace → Windows Optimization route | FAIL | `Timed out waiting for automation element 'WindowsOptimizationCategory_custom.mouse'` after `Navigated to Windows Optimization page`; stack shows `EnsureOptimizationCategoryVisible(...)` / `TestOpenOptimizationExtension(...)`. |
| `shell-integration` | Marketplace → Windows Optimization route | PASS | Optimization category found, settings button visible, `shell.integration.enable` and `shell.integration.disable` actions verified, screenshot captured at `C:\Users\96152\AppData\Local\Temp\llt-plugin-settings-host-20260324-203734\shell-integration-optimization-page.png`, final line `[main-smoke] PASS`. |
| `network-acceleration` | Did not reach UI flow | FAIL | `UnauthorizedAccessException` during `PrepareRuntimePluginFixtures(...)` while deleting `LenovoLegionToolkit.Plugins.ViveTool.resources.dll`. |
| default preferred set (`custom-mouse`, `shell-integration`, `vive-tool`, `network-acceleration`) | Did not reach UI flow | FAIL | Same startup-time `UnauthorizedAccessException` in `PrepareRuntimePluginFixtures(...)`; no plugin UI validation ran. |

### Failure classification
- `custom-mouse`: stable code/UI-path failure in optimization category discovery, not a marketplace-unavailable case.
- `network-acceleration` single-plugin run: runtime fixture cleanup / file-lock failure before app launch.
- default preferred set: same runtime fixture cleanup / file-lock failure before app launch.
- `shell-integration`: verified PASS path through optimization route with executable actions and screenshot evidence.

### Known limitations after this validation pass
- Current verification evidence proves at least one full end-to-end PASS path (`shell-integration`) and two reproducible FAIL classes, but it does not yet provide a clean full-pass run for the entire default preferred plugin set.
- The fixture-cleanup failure blocks broader coverage until the locked runtime plugin directory issue is addressed.
- `custom-mouse` still lacks a successful optimization-route verification in this environment.
