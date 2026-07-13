# Task Plan

## Goal
Fix SettingsManager.Dispose() race condition with debounce timer callback that causes ObjectDisposedException when _semaphore is disposed while timer-driven Save() is in flight.

## Baseline
HEAD: 9f9bbad on origin/master. 7 prior commits in revision 56 cycle. Full gate: 548 pass, 2 skip, 0 fail.

## Scope
- `Plugins/Shared/SettingsManager.cs` — Dispose() and OnSaveDebounceTimerElapsed()
- `Plugins/Shared.Tests/SettingsManagerEdgeCaseTests.cs` — regression test

## Steps
1. Add `_disposed` check inside `_lock` in `OnSaveDebounceTimerElapsed` — return immediately if disposed.
2. Move `_pendingSettings` read+null under `_lock` in `Dispose()` — eliminate data race with timer callback. Flush pending save outside `_lock` to preserve lock-order inversion safety.
3. Add regression test `Dispose_ConcurrentWithDebounceTimer_DoesNotThrowObjectDisposed` — queues 50 debounced saves with 1ms delay, Disposes while timer callbacks may be in flight, verifies no ObjectDisposedException and idempotent double-Dispose.
4. Build, run focused test, run full gate, run canonical verify-hermes.ps1.

## Verification
- Focused: `dotnet test Plugins/Shared.Tests/Shared.Tests.csproj -c Release --nologo --filter "Dispose_ConcurrentWithDebounceTimer_DoesNotThrowObjectDisposed"`
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`

## Risks
- Timer callback could still execute Save() after _disposed check but before Dispose() disposes _semaphore — mitigated by _disposed check happening under _lock; Dispose sets _disposed before disposing timer/semaphore.
- Double-Dispose safety — already handled by _disposed guard at top of Dispose().

## Stop Conditions
Stop after fix is committed and pushed if verification passes.

## Evidence
Attempt 25 — Dispose vs debounce timer race condition fix.

Defect: SettingsManager.Dispose() disposes _semaphore while the debounce timer
callback OnSaveDebounceTimerElapsed may be executing Save() which calls
_semaphore.Wait(). The timer callback did not check _disposed, causing
ObjectDisposedException. Additionally, Dispose() read _pendingSettings outside
_lock — a data race with the timer callback which nulls _pendingSettings under
_lock.

Fix (SettingsManager.cs, 2 changes):
1. OnSaveDebounceTimerElapsed: added _disposed check inside _lock (line 508).
   Returns immediately if disposed, preventing Save() from calling
   _semaphore.Wait() after _semaphore is disposed.
2. Dispose(): moved _pendingSettings read+null under _lock (lines 488-493).
   Eliminates data race with timer callback. Pending save still flushed
   outside _lock to avoid lock-order inversion.

Test: Dispose_ConcurrentWithDebounceTimer_DoesNotThrowObjectDisposed — queues
50 debounced saves with 1ms delay, then Disposes while timer callbacks may be
in flight. Verifies no ObjectDisposedException and idempotent double-Dispose.

Verification results:
- Build: 0 errors, 0 warnings
- Focused: 1/1 pass, 33ms
- Shared.Tests: 210 pass, 0 fail, 0 skip
- Full solution: BatteryHealth 71, CustomMouse 38, ShellIntegration 146, NetworkAcceleration 62+2 skip, ViveTool 231 — 548 pass, 2 skip, 0 fail
- Canonical verify-hermes.ps1: 0 errors, 0 warnings, EXIT_CODE=0
