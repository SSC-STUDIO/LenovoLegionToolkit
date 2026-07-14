# Task Plan
## Goal
Fix orphaned temp file leak in SettingsManager Save/SaveAsync — when File.Move fails (locked target, I/O error, cancellation), the .tmp file is left on disk. Clean up temp files in all 4 atomic save paths.
## Baseline
HEAD d7eac22 on origin/master. Prior commits e1478b6 (NetworkAcceleration test + ViveToolSettings atomic save) and d7eac22 (Update/SaveAsync deadlock fix) are published. 547 pass / 2 skip / 0 fail. verify-hermes.ps1 exit 0.
## Scope
Plugins/Shared/SettingsManager.cs — add CleanupTempFile helper + try/catch in all 4 save paths (JSON Save, JSON SaveAsync, MessagePack Save, MessagePack SaveAsync). Plugins/Shared.Tests/SettingsManagerEdgeCaseTests.cs — add 2 regression tests that lock target file to force File.Move failure and assert temp file cleanup.
## Steps
1. Inspect git status — confirm HEAD d7eac22, two modified product files unstaged.
2. In SettingsManager.cs SaveAsync JSON path: wrap FileStream write + File.Move in try/catch, call CleanupTempFile on failure, re-throw.
3. In SettingsManager.cs SaveAsync MessagePack path: same try/catch pattern.
4. In SettingsManager.cs Save() JSON path: same try/catch pattern.
5. In SettingsManager.cs Save() MessagePack path: same try/catch pattern.
6. Add private void CleanupTempFile(string tempPath) helper that calls DeleteIfExists with logging.
7. Add Save_WhenFileMoveFails_CleansUpTempFile test — locks target file, calls Save with changed settings, asserts Save returns false and no .tmp file remains.
8. Add Save_MessagePack_WhenFileMoveFails_CleansUpTempFile test — same pattern for MessagePack path.
9. Build Shared.Tests project — confirm 0 errors 0 warnings.
10. Run focused tests — confirm 2/2 pass.
11. Run full gate: dotnet test UniversalDeviceToolkit-Plugins.sln -c Release --nologo.
12. Run verify-hermes.ps1 — confirm exit 0.
13. Stage, commit, push as rev58.
## Verification
- Focused: dotnet test Plugins/Shared.Tests/Shared.Tests.csproj -c Release --filter "Save_WhenFileMoveFails|Save_MessagePack_WhenFileMoveFails" — 2/2 passed, exit 0.
- Full gate: dotnet test UniversalDeviceToolkit-Plugins.sln -c Release --nologo — 547 pass, 2 skip, 0 fail, exit 0.
- Canonical: powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1 — EXIT_CODE=0.
## Risks
- CleanupTempFile is best-effort: if temp file is locked by another process, it logs a warning but does not throw. This is acceptable — the orphaned temp file will be cleaned up on next successful save or manual deletion.
- The catch blocks re-throw the original exception, preserving the error-handling contract of Save (catches Exception, returns false) and SaveAsync (catches Exception, returns false). The outer catch still handles the re-thrown exception.
## Stop Conditions
Stop after commit and push if verification passes. No further work needed — this is a bounded, self-contained fix.
## Evidence
Defect: SettingsManager.Save() and SaveAsync() write settings to a .tmp file then File.Move it over the target. If File.Move fails (target locked, I/O error, cancellation), the .tmp file is orphaned on disk permanently. All 4 paths affected: JSON Save, JSON SaveAsync, MessagePack Save, MessagePack SaveAsync.

Fix: Wrapped each temp-file write+move sequence in try/catch. On any exception, CleanupTempFile(tempPath) is called before re-throwing. Added private void CleanupTempFile(string) helper that calls existing DeleteIfExists with best-effort logging.

Tests: 2 new regression tests added to SettingsManagerEdgeCaseTests.cs:
- Save_WhenFileMoveFails_CleansUpTempFile: locks JSON target file via FileStream(FileShare.Read), calls Save with changed settings, asserts Save returns false and no .tmp file remains.
- Save_MessagePack_WhenFileMoveFails_CleansUpTempFile: same pattern for MessagePack path.

Build: dotnet build Plugins/Shared.Tests/Shared.Tests.csproj -c Release — 0 errors, 0 warnings.
Focused test: 2/2 passed (1s), exit 0.
Full gate: 547 pass, 2 skip, 0 fail, exit 0.
verify-hermes.ps1: EXIT_CODE=0.

Commit: fix(shared): clean up orphaned temp files on save failure in SettingsManager
