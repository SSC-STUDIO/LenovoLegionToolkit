# Task Plan

## Goal
Fix SaveSettingsAsync retry loop in CustomMousePlugin to catch UnauthorizedAccessException (from File.Move when destination is locked) in addition to IOException. Add focused behavioral regression test that verifies retry succeeds after a temporary file lock is released — no timing assertions.

## Baseline
HEAD: 9b153d8 (master, pushed to origin). Working tree has 2 modified files: CustomMousePlugin.cs (retry catch widened), CustomMousePluginTests.cs (new behavioral test). Full gate: 551 pass, 2 skip, 0 fail. verify-hermes.ps1 exit 0.

## Scope
- Plugins/CustomMouse/CustomMousePlugin.cs — widen retry catch to include UnauthorizedAccessException
- Plugins/CustomMouse.Tests/CustomMousePluginTests.cs — behavioral regression test (no timing)
- ai/task-plans/rev62-custommouse-unauthorized-access-retry.md — this plan

## Steps
1. CustomMousePlugin.SaveSettingsAsync retry loop (line ~362-377): add `catch (UnauthorizedAccessException) when (attempt < maxRetries - 1)` with same retry delay pattern as IOException catch.
2. Replace flaky timing-based test with behavioral test: lock settings file for 20ms (before first 50ms retry delay), release lock via Task.Run, verify SaveSettingsAsync succeeds after retry. Assert file content is non-empty after save.
3. Build CustomMouse.Tests.csproj -c Release — confirm 0 errors, 0 warnings.
4. Run focused test: dotnet test --filter "SaveSettingsAsync_RetriesOnException_WhenSettingsFileTemporarilyLocked" — confirm 1/1 pass.
5. Run full CustomMouse suite — confirm 38 pass (37 original + 1 new).
6. Run canonical verify-hermes.ps1 — confirm exit 0, all projects pass.

## Verification
- Focused: dotnet test Plugins/CustomMouse.Tests/CustomMouse.Tests.csproj -c Release --filter "SaveSettingsAsync_RetriesOnException_WhenSettingsFileTemporarilyLocked" — 1/1 pass, exit 0
- CustomMouse suite: 38 pass, 0 fail, 0 skip, exit 0
- Canonical: powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1 — BatteryHealth 71, CustomMouse 38, ShellIntegration 146, NetworkAcceleration 62+2 skip, ViveTool 231, total 551 pass, 2 skip, 0 fail, build 0 errors, exit 0
- Ad-hoc verification: hermes-verify-rev56-custommouse-retry.sh — ALL PASS, CANONICAL_EXIT=0

## Risks
- 20ms lock-release delay has 30ms margin before 50ms retry delay; could be increased if CI is very slow.
- UnauthorizedAccessException is not the primary exception from PluginConfiguration.SaveAsync (IOException is), but is a valid defensive catch for File.Move scenarios.

## Stop Conditions
Stop after commit and push of rev62. All verification passes.

## Evidence
Attempt 20: Fixed flaky timing-based test by replacing with behavioral reliability check. Focused test 1/1 pass (89ms). CustomMouse 38 pass. Full gate 551 pass, 2 skip, 0 fail. Canonical verify-hermes.ps1 exit 0. Ad-hoc verification CANONICAL_EXIT=0.

Attempt 21: Master approved publication. Committing as rev62.
- Build: 0 errors, 0 warnings
- Focused: 1/1 pass, 366ms
- CustomMouse suite: 38 pass
- Canonical: 551 pass, 2 skip, 0 fail, exit 0
