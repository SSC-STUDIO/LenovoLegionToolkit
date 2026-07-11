# Task Plan — Revision 55: SettingsManager Save/SaveAsync Lock Unification

## Goal
Fix the thread-safety defect in `SettingsManager<T>` where `Save()` (synchronous)
and `SaveAsync()` (asynchronous) use different synchronization primitives to
protect the same settings file resource. `Save()` uses `lock(_lock)` (Monitor)
while `SaveAsync()` uses `_semaphore` (SemaphoreSlim). A concurrent call to
`Save()` and `SaveAsync()` on the same instance can interleave `.tmp` file
writes and `File.Move` calls, corrupting the settings file.

## Baseline
- Repository: UniversalDeviceToolkit-Plugins
- Branch: master (commit 9f8327c — StopAsync fix published)
- .NET SDK: 10.0.100-preview.5.25277.114
- Defect location: `Plugins/Shared/SettingsManager.cs`
  - Line 36: `private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);`
  - Line 103: `Load()` uses `lock (_lock)` -- this is fine (read-only cache access)
  - Line 168: `SaveAsync()` uses `await _semaphore.WaitAsync(cancellationToken)` -- correct
  - Line 276: `Save()` uses `lock (_lock)` -- MISMATCHED: should also use _semaphore
  - Line 357: `Update()` uses `lock (_lock)` and calls `Load()` + `Save()` internally
  - Line 427: `OnSaveDebounceTimerElapsed()` uses `lock (_lock)` and calls `Save()`
- The `lock(_lock)` and `_semaphore` are independent primitives. `Save()` holding
  `lock(_lock)` does NOT block `SaveAsync()` which waits on `_semaphore`, and vice
  versa. Both write to the same `.tmp` path and call `File.Move` on the same target.
- No existing test covers concurrent `Save()` + `SaveAsync()` on the same instance.
- Build baseline: `dotnet build` exits 0, 0 errors.
- Test baseline: `dotnet test` exits 0, 544 passed, 2 skipped, 0 failed.

## Scope
- Modify `Plugins/Shared/SettingsManager.cs` to unify the lock: `Save()` and
  `SaveAsync()` must use the same `_semaphore` to be mutually exclusive.
- Add a focused test in `Plugins/Shared.Tests/SettingsManagerEdgeCaseTests.cs`
  that exercises concurrent `Save()` + `SaveAsync()` on the same instance to
  verify no corruption and no exceptions.
- No other files are modified.

## Steps
1. Create this plan file at `ai/task-plans/rev55-settingsmanager-lock-unification.md`.
2. In `SettingsManager.cs`, change `Save()` to acquire `_semaphore.Wait()`
   before file operations and release in `finally`, replacing the inner
   `lock(_lock)` with `_semaphore.Wait()`/`_semaphore.Release()`. The outer
   `lock(_lock)` for cache state (_cachedSettings, _lastSavedJson) is kept to
   protect the in-memory transaction; the semaphore protects the file write.
3. In `SettingsManager.cs`, change `Update()` to also acquire `_semaphore`
   since it calls `Save()` which now uses the semaphore. The outer `lock(_lock)`
   is kept for memory state. Use `_semaphore.Wait()` / `_semaphore.Release()`
   in a try/finally inside the existing lock block.
4. In `SettingsManager.cs`, change `OnSaveDebounceTimerElapsed` to wrap the
   `Save()` call with `_semaphore.Wait()`/`Release()` since Save() no longer
   uses lock(_lock) for file protection.
5. Add a test `SaveAsync_ConcurrentWithSave_NoCorruption` in
   `SettingsManagerEdgeCaseTests.cs` that fires 20 concurrent operations
   (half Save, half SaveAsync) on the same SettingsManager instance and
   verifies the file is valid JSON/MessagePack with no exceptions.
6. Run focused tests: `dotnet test Plugins/Shared.Tests/ --filter SettingsManager`.
7. Run full solution gate: `dotnet test LenovoLegionToolkit-Plugins.sln -c Release --nologo`.
8. Update this plan with actual verification results.

## Verification
Commands executed and results (collected in this turn, revision 55):

### Build
```
dotnet build LenovoLegionToolkit-Plugins.sln -c Release --nologo
```
Exit code: 0 | 0 warnings, 0 errors | Duration: 13.21s

### Focused tests (SettingsManager)
```
dotnet test "Plugins/Shared.Tests/Shared.Tests.csproj" -c Release --nologo --filter "SettingsManager"
```
Exit code: 0 | 52 passed, 0 failed, 0 skipped | Duration: 561ms
- Save_ConcurrentWithSaveAsync_NoCorruption: PASS (the test exercising the fix)

### Full solution gate
```
dotnet test LenovoLegionToolkit-Plugins.sln -c Release --nologo
```
Exit code: 0
Results by project:
- BatteryHealth.Tests.dll: 71 passed, 0 skipped, 0 failed (821ms)
- CustomMouse.Tests.dll: 37 passed, 0 skipped, 0 failed (930ms)
- ShellIntegration.Tests.dll: 145 passed, 0 skipped, 0 failed (1s)
- NetworkAcceleration.Tests.dll: 62 passed, 2 skipped, 0 failed (12s)
- ViveTool.Tests.dll: 229 passed, 0 skipped, 0 failed (27s)
Grand total: 544 passed, 2 skipped, 0 failed

## Risks
1. Changing `Save()` from `lock(_lock)` to `_semaphore.Wait()` introduces
   async blocking. However, `Save()` is already synchronous and uses
   `File.WriteAllBytes`/`File.WriteAllText`, so the blocking is no worse than
   existing disk I/O. The semaphore is `new SemaphoreSlim(1,1)` which is
   safe for sync `Wait()` and async `WaitAsync()`.
2. The `Update()` method calls `Load()` then `Save()` inside `lock(_lock)`.
   `Load()` uses `lock(_lock)` (re-entrant, fine). `Save()` will now use
   `_semaphore` inside `lock(_lock)`. Lock ordering: _lock then _semaphore.
   `SaveAsync()` uses `_semaphore` then fires event outside. No reverse
   ordering, so no deadlock risk.
3. `Dispose()` calls `Save()` at line 413 (for pending flush) without holding
   `_lock`. After the fix, `Save()` will use `_semaphore`. If `SaveAsync()` is
   in-flight for the same instance, `Save()` will block on `_semaphore` until
   the async save finishes — this is correct behavior.
4. Two pre-existing test skips in NetworkAcceleration.Tests are unrelated.

## Stop Conditions
- If build produces errors after the lock change, stop and investigate.
- If any test that was previously passing now fails, stop and investigate.
- If the deadlock test itself hangs (timeout), investigate lock ordering.

## Evidence
(To be filled with actual verification results after implementation.)
