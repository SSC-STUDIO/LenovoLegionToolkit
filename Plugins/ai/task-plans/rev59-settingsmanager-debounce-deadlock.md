# Task Plan
## Goal
Fix lock-order inversion deadlock in OnSaveDebounceTimerElapsed — debounce timer callback called Save() while holding _lock, causing AB-BA deadlock with concurrent SaveAsync() (which holds _semaphore then tries _lock). Same root cause as d7eac22 (Update deadlock) but in a missed call site.

## Baseline
HEAD aa79184 on origin/master. 4 prior commits: ViveTool fix (e1478b6), Update deadlock (d7eac22), temp file cleanup (aa79184), test assertion fix (e1478b6). Full gate: 548 pass, 2 skip, 0 fail. verify-hermes.ps1 exit 0.

## Scope
- Plugins/Shared/SettingsManager.cs — OnSaveDebounceTimerElapsed refactor
- Plugins/Shared.Tests/SettingsManagerEdgeCaseTests.cs — new regression test + using System.Threading

## Steps
1. Refactor OnSaveDebounceTimerElapsed: extract _pendingSettings under _lock, release _lock, call Save() outside lock.
2. Add SaveWithDebounce_ConcurrentWithSaveAsync_DoesNotDeadlock test — enableDebounce:true, debounceDelayMs:50, concurrent SaveAsync, 5s timeout assertion.
3. Add `using System.Threading;` to test file for CancellationTokenSource.
4. Build Shared.Tests.csproj — confirm 0 errors.
5. Run focused test — confirm 1/1 pass.
6. Run full solution gate — confirm 548+ pass, 2 skip, 0 fail.
7. Run verify-hermes.ps1 — confirm exit 0.

## Verification
- Focused: `dotnet test Plugins/Shared.Tests/Shared.Tests.csproj -c Release --filter "SaveWithDebounce_ConcurrentWithSaveAsync_DoesNotDeadlock"`
- Full gate: `dotnet test LenovoLegionToolkit-Plugins.sln -c Release --nologo`
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`

## Risks
- Dispose() reads _pendingSettings without _lock — benign race (best-effort flush after timer stopped), not fixed to keep change bounded.
- Timer may fire after Dispose() stops it — mitigated by _pendingSettings null check.

## Stop Conditions
Stop after one coherent increment if verification passes.

## Evidence
Defect: OnSaveDebounceTimerElapsed (line 499) called Save() while holding _lock (line 501). Save() Phase 2 acquires _semaphore (line 363). Concurrent SaveAsync() holds _semaphore (line 200) then tries lock(_lock) (line 234). Timer holds _lock waiting _semaphore, async holds _semaphore waiting _lock — AB-BA deadlock. Same root cause as d7eac22 but in debounce timer callback, which was missed.

Fix: Extract _pendingSettings under _lock, set to null, release _lock, then call Save() outside lock — identical pattern to d7eac22 Update() fix.

Files changed:
- Plugins/Shared/SettingsManager.cs: OnSaveDebounceTimerElapsed refactored to release _lock before Save()
- Plugins/Shared.Tests/SettingsManagerEdgeCaseTests.cs: Added SaveWithDebounce_ConcurrentWithSaveAsync_DoesNotDeadlock test + using System.Threading

Verification:
- Focused: dotnet test Plugins/Shared.Tests/Shared.Tests.csproj -c Release --filter "SaveWithDebounce_ConcurrentWithSaveAsync_DoesNotDeadlock" — 1/1 passed, 27ms, exit 0
- Full gate: dotnet test LenovoLegionToolkit-Plugins.sln -c Release --nologo — 548 pass, 2 skip, 0 fail, exit 0
- Canonical: powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1 — EXIT_CODE=0, build 0 errors 0 warnings
- Ad-hoc script: hermes-verify-rev56-debounce-deadlock.sh — FOCUSED_EXIT=0, GATE_EXIT=0, BUILD_EXIT=0, ALL PASS

Pre-existing skips: GetRecentSamples_PreservesFifoOrder, GetRecentSamples_AfterStart_ReturnsSamples (NetworkAcceleration.Tests, require live WMI/adapter).

Commit: fix(shared): break debounce timer Save deadlock by moving Save outside _lock
