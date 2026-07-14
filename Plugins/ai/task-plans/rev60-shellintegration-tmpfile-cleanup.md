# Task Plan
## Goal
Fix orphaned temp file leak in ShellIntegrationConfigService — two atomic-write sites (SaveProfile line 192, WriteFileIfChangedUnlocked line 604) create `.tmp` files then call `File.Move(overwrite: true)`. If File.Move throws (target locked, I/O error), the `.tmp` file is orphaned permanently with no cleanup. Same defect class as aa79184 (SettingsManager) but in ShellIntegration plugin.

## Baseline
HEAD b1de808 on origin/master. 4 commits already pushed this revision cycle (e1478b6, d7eac22, aa79184, b1de808). Full gate: 548 pass, 2 skip, 0 fail. verify-hermes.ps1 EXIT_CODE=0.

## Scope
- `Plugins/ShellIntegration/ShellIntegrationConfigService.cs` — wrap both atomic-write sites in try/catch with DeleteIfExists cleanup; add private static DeleteIfExists helper
- `Plugins/ShellIntegration.Tests/ShellIntegrationConfigServiceTests.cs` — new test SaveProfile_WhenFileMoveFails_CleansUpTempFile

## Steps
1. Wrap SaveProfile (line 192-194) write+move in try/catch with DeleteIfExists(tempPath) cleanup before re-throwing
2. Wrap WriteFileIfChangedUnlocked (line 604-606) write+move in try/catch with DeleteIfExists(tempPath) cleanup before re-throwing
3. Add private static DeleteIfExists helper (best-effort, swallows IOException)
4. Add SaveProfile_WhenFileMoveFails_CleansUpTempFile regression test — saves profile, locks target with FileStream(FileShare.None), calls SaveProfile again (catches UnauthorizedAccessException), asserts .tmp file does not exist
5. Build, run focused test, run full gate, run verify-hermes.ps1
6. Commit and push as rev60

## Verification
- Focused: `dotnet test Plugins/ShellIntegration.Tests/ShellIntegration.Tests.csproj -c Release --filter "SaveProfile_WhenFileMoveFails_CleansUpTempFile"` — 1/1 passed, 42ms, exit 0
- Full gate: `dotnet test UniversalDeviceToolkit-Plugins.sln -c Release --nologo` — 549 pass (71+37+146+62+230+3), 2 skip, 0 fail, exit 0
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1` — EXIT_CODE=0, 0 errors, 0 warnings
- Ad-hoc: hermes-verify-rev56-shellintegration-templeak.sh — FOCUSED_EXIT=0, GATE_EXIT=0

## Risks
- WriteFileIfChangedUnlocked is called under _staticFileLock — DeleteIfExists swallows IOException only, any other exception still re-throws, lock released via throw. No deadlock risk.
- DeleteIfExists is best-effort, swallow IOException — if File.Delete also fails, temp file remains, but original exception is still re-thrown so caller sees the real failure.

## Stop Conditions
Stop after commit and push if verification passes. No further defects in scope.

## Evidence
Defect: ShellIntegrationConfigService.SaveProfile (line 192) and WriteFileIfChangedUnlocked (line 604) both use atomic-write pattern (write .tmp + File.Move). If File.Move throws (target locked by another process, antivirus, I/O error), .tmp file is orphaned permanently — same defect class as aa79184 (SettingsManager) but in ShellIntegration plugin. No try/catch cleanup.

Fix: wrapped both write+move sites in try/catch with DeleteIfExists(tempPath) cleanup before re-throwing. Added private static DeleteIfExists helper (best-effort, swallows IOException). Catch is catch-all (not just IOException) because File.Move on a locked target throws UnauthorizedAccessException, not IOException.

Test: SaveProfile_WhenFileMoveFails_CleansUpTempFile — saves profile successfully, locks target file with FileStream(FileShare.None), calls SaveProfile again (catches UnauthorizedAccessException), asserts .tmp file does not exist after failure.

Verification results:
- Build: dotnet build Plugins/ShellIntegration.Tests/ShellIntegration.Tests.csproj -c Release — 0 errors, 0 warnings, exit 0
- Focused: dotnet test Plugins/ShellIntegration.Tests/ShellIntegration.Tests.csproj -c Release --filter "SaveProfile_WhenFileMoveFails_CleansUpTempFile" — 1/1 passed, 42ms, exit 0
- Full gate: dotnet test UniversalDeviceToolkit-Plugins.sln -c Release — 549 pass (71+37+146+62+230+3), 2 skip, 0 fail, exit 0
- Canonical: powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1 — EXIT_CODE=0, build 0 errors 0 warnings
- Ad-hoc: hermes-verify-rev56-shellintegration-templeak.sh — FOCUSED_EXIT=0, GATE_EXIT=0

Commit: fix(shell): clean up orphaned temp files on save failure in ShellIntegrationConfigService
