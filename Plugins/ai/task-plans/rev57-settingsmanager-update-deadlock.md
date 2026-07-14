# Task Plan

## Goal
Fix a lock-order inversion deadlock in `SettingsManager<T>.Update()` that could permanently hang the host process when `Update()` and `SaveAsync()` ran concurrently on different threads.

## Baseline
- HEAD: e1478b6 (master, origin/master)
- Previous commit: c9d357c
- Working tree clean except for the two source files and orchestrator-owned ai/ contracts.

## Scope
- `Plugins/Shared/SettingsManager.cs` — `Update()` method restructure
- `Plugins/Shared.Tests/SettingsManagerEdgeCaseTests.cs` — regression test
- `ai/task-plans/rev57-settingsmanager-update-deadlock.md` — this plan

## Steps
1. Identify the deadlock: `Update()` held `_lock` and called `Save()`, which acquires `_semaphore` in Phase 2. If another thread's `SaveAsync()` held `_semaphore` and tried to acquire `_lock` at its cache-update block, both threads would block forever.
2. Fix: Move `Save(settings)` outside `lock(_lock)` in `Update()`. Load and mutate under `_lock`, release `_lock`, then call `Save()` — breaking the `_lock` → `_semaphore` lock-order cycle.
3. Add regression test: `Update_ConcurrentWithSaveAsync_DoesNotDeadlock` with `ManualResetEventSlim` gate and 5-second timeout.
4. Build the solution and run the focused test.
5. Run the full solution gate and `verify-hermes.ps1`.

## Verification
- Focused: `dotnet test Plugins/Shared.Tests/Shared.Tests.csproj -c Release --nologo --filter "Update_ConcurrentWithSaveAsync"` → exit 0, 1/1 passed
- Full gate: `dotnet test UniversalDeviceToolkit-Plugins.sln -c Release --nologo` → exit 0, 0 failures
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1` → exit 0

## Risks
- `Update()` now releases `_lock` before `Save()`, so another thread could `Load()` between the mutate and save phases. This is acceptable: `Save()` serializes via `_semaphore` and the memory transaction comparison (`_lastSavedJson`) prevents stale overwrites. The lock-order safety gain outweighs the narrow interleaving window.

## Stop Conditions
Stop after the deadlock fix is verified and published.

## Evidence

### Attempt 5 — 2026-07-13 (UTC+8)

**Defect:** `SettingsManager<T>.Update()` held `_lock` while calling `Save()`, which acquires `_semaphore` in Phase 2. If another thread was running `SaveAsync()` (holding `_semaphore`, waiting for `_lock` in its cache-update block), both threads would deadlock permanently.

**Root cause:** `Update()` was a single `lock(_lock) { Load(); updateAction(); Save(); }` block. `Save()` releases `_lock` after Phase 1 then calls `_semaphore.Wait()` — but `Update()` never released `_lock`, so `SaveAsync()`'s inner `lock(_lock)` at line 234 could never proceed while `Update()` blocked on `_semaphore`.

**Fix:** Split `Update()` into two phases: (1) load + mutate under `_lock`, then release `_lock`; (2) call `Save(settings)` outside the lock. This breaks the lock-order cycle.

**Changed files:**
- `Plugins/Shared/SettingsManager.cs` — `Update()` method restructured (lines ~389-411)
- `Plugins/Shared.Tests/SettingsManagerEdgeCaseTests.cs` — added `Update_ConcurrentWithSaveAsync_DoesNotDeadlock` regression test

**Verification results:**
- `dotnet build UniversalDeviceToolkit-Plugins.sln -c Release --nologo` → exit 0, 0 errors, 0 warnings
- `dotnet test Plugins/Shared.Tests/Shared.Tests.csproj -c Release --nologo --filter "Update_ConcurrentWithSaveAsync"` → exit 0, 1/1 passed (879 ms)
- `dotnet test UniversalDeviceToolkit-Plugins.sln -c Release --nologo` → exit 0, 0 failures (all projects passed)
- `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1` → exit 0
