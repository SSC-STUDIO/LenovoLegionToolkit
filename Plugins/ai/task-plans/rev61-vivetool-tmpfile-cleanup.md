# Task Plan
## Goal
Fix orphaned temp file leak in ViveToolSettings.SaveAsync — the third and final instance of the cross-plugin atomic-write .tmp leak defect class (SettingsManager → ShellIntegrationConfigService → ViveToolSettings).

## Baseline
HEAD 5bd9018 on origin/master. All prior fixes committed and pushed:
- aa79184: SettingsManager temp file cleanup
- 5bd9018: ShellIntegrationConfigService temp file cleanup
ViveToolSettings.SaveAsync (line 176-183) has the same atomic-write pattern without cleanup in its catch block.

## Scope
- Plugins/ViveTool/Services/Settings/ViveToolSettings.cs — add .tmp cleanup in existing catch block
- Plugins/ViveTool.Tests/ViveToolPathServiceTests.cs — add SaveAsync_WhenFileMoveFails_CleansUpTempFile regression test
- ai/task-plans/rev61-vivetool-tmpfile-cleanup.md — this plan

## Steps
1. Add try/catch File.Delete cleanup in ViveToolSettings.SaveAsync catch block (before logging)
2. Add regression test that locks target file with FileStream(FileShare.None), calls SaveAsync, asserts no .tmp remains
3. Build ViveTool.Tests.csproj -c Release
4. Run focused test: dotnet test --filter "SaveAsync_WhenFileMoveFails_CleansUpTempFile"
5. Run full gate: dotnet test UniversalDeviceToolkit-Plugins.sln -c Release
6. Run canonical: powershell -File scripts/verify-hermes.ps1
7. Update Evidence with exact commands and exit codes

## Verification
- Focused: dotnet test Plugins/ViveTool.Tests/ViveTool.Tests.csproj -c Release --filter "SaveAsync_WhenFileMoveFails_CleansUpTempFile"
- Full gate: dotnet test UniversalDeviceToolkit-Plugins.sln -c Release --nologo
- Canonical: powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1

## Risks
- File.Delete in catch could throw if .tmp is also locked — handled with nested try/catch + logging
- Test could leave .tmp if SaveAsync doesn't actually fail — test locks target file with FileShare.None to guarantee File.Move throws
- ViveTool test count increases from 230 to 231 (new regression test)

## Stop Conditions
Stop after commit is pushed and old plan deleted, or if verification fails after two focused attempts.

## Evidence
Defect: ViveToolSettings.SaveAsync() line 176-178 uses atomic-write pattern (WriteAllText .tmp + File.Move). Catch block at line 180 logs exception but does NOT delete .tmp. If File.Move throws (target locked), .tmp is orphaned permanently.

Fix: Added 4 lines in catch block — try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch (Exception cleanupEx) { PluginLog.Trace(...) }

Regression test: SaveAsync_WhenFileMoveFails_CleansUpTempFile — locks target with FileStream(FileShare.None), calls SaveAsync, asserts no .tmp remains.

Verification:
- Focused: 1/1 passed, 290ms, exit 0
- Full gate: 550 pass (BatteryHealth 71, CustomMouse 37, ShellIntegration 146, NetworkAcceleration 62+2 skip, ViveTool 231, Shared.Tests 3), 2 skip, 0 fail, exit 0
- Canonical: verify-hermes.ps1 exit 0, build 0 errors 0 warnings
- Ad-hoc: hermes-verify-rev56-vivetool.sh — FOCUSED_EXIT=0, CANONICAL_EXIT=0 (temp script created, run, cleaned up)

Pre-existing skips: GetRecentSamples_PreservesFifoOrder, GetRecentSamples_AfterStart_ReturnsSamples (NetworkAcceleration.Tests, require live WMI/network adapter).
