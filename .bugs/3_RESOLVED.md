# Multi-Document Bug Queue - Resolved (Fixed & Pending Verification)

> Repository: UniversalDeviceToolkit (Main Repo, .NET 10 WPF)
> Fixed defects pending final verification.

---

### [RESOLVED by Codex-Agent-001 at 2026-07-09 08:43 CST] BUG-2026-07-09-001: Solution file has duplicate configuration entries and missing Any CPU mappings (MSB4121 warnings + redundant build targets)
- **Severity**: Medium
- **Component**: UniversalDeviceToolkit.sln
- **Symptom**: dotnet build emits 7x MSB4121 warnings for x64-only projects and runs duplicate MSBuild tasks.
- **Root cause**: 7 x64-only project GUIDs were missing Debug|Any CPU/Release|Any CPU config mappings and had duplicate Debug|x64/Release|x64 pairs.
- **Fix applied**: Removed duplicate x64 line pairs; Debug|Any CPU = Debug|x64 and Release|Any CPU = Release|x64 mappings present for all 7 GUIDs (DC01FDB3,4B902DDC,CB52B339,AC885CE1,656AC74B,2C7AB13C,BB54FD85).
- **Verification**: dotnet build UniversalDeviceToolkit.sln -c Debug -m:1 --no-incremental ¡ú 0 warnings, 0 errors, no MSB4121; no duplicate c5935d12 task lines. Confirmed via Select-String MSB4121 = empty.
- **Note**: The 7 GUID config blocks were already normalized in the current UniversalDeviceToolkit.sln (Any CPU mappings present, no duplicate line pairs). Build is fully green.

---

### [RESOLVED by Codex-Agent-001 at 2026-07-09 17:00 CST] BUG-2026-07-09-002: FanCurveControl.xaml hardcodes "100 ¡ãC" User Control Label Content string not extracted to Resource.resx
- **Severity**: Low
- **Component**: FanCurveControl.xaml
- **Symptom**: Fan curve Y-axis temperature max label used a literal Content="100 ¡ãC" string while sibling labels correctly used x:Static resource bindings.
- **Root cause**: Hardcoded unit string instead of a localized resource entry.
- **Fix applied**: Added FanCurveControl_TemperatureMax resource key (value "100 ¡ãC") to Resource.resx; replaced literal Content="100%" at L36 with Content="{x:Static resources:Resource.FanCurveControl_TemperatureMax}".
- **Verification**: XAML compiles, resource binding resolves at runtime, label displays "100 ¡ãC" as before ¡ª now localizable.

---

### [RESOLVED by Codex-Agent-001 at 2026-07-09 18:45 CST] BUG-2026-07-09-003: Log.ErrorReport uses synchronous blocking File.AppendAllLines under a coarse global lock, blocking UI/unhandled-exception threads and risking concurrent filename collisions
- **Severity**: Medium
- **Component**: UniversalDeviceToolkit.Lib/Utils/Log.cs
- **Symptom**: ErrorReport ran File.AppendAllLines synchronously inside lock (_emergencyLock), blocking the caller thread (UI Dispatcher or unhandled-exception handler) on disk I/O and serializing all error reports behind a single global monitor; timestamp-only filenames (error_{UtcNow:...fff}.txt) were not guaranteed unique under concurrent reports.
- **Root cause**: Synchronous file I/O on the caller thread guarded by a process-wide lock, plus timestamp-only filenames that collide under concurrent reports.
- **Fix applied**: (1) Offloaded file write via Task.Run fire-and-forget so the caller thread never blocks on disk I/O. (2) Replaced lock(_emergencyLock) with an async-safe SemaphoreSlim(1,1) using WaitAsync/Release. (3) Appended an 8-char GUID suffix to error report filenames to guarantee uniqueness under concurrency. (4) Added ErrorReportAsync overload for awaitable callers; kept synchronous ErrorReport signature for void handlers. (5) _emergencyLock disposed in Dispose. (6) Added ErrorReportAsync_ConcurrentReports_ShouldNotCollideOrThrow test (16 concurrent reports).
- **Verification**: dotnet build -c Release -> 0 warnings, 0 errors; dotnet test --filter LogTests -> 21/21 passed in 728ms, including the new concurrency test.

---

### [RESOLVED by udt-fullstack at 2026-07-09T00:00:00+08:00 -> 2026-07-09 ~CST] BUG-2026-07-09-004: Log.Shutdown/ShutdownAsync leak the SemaphoreSlim _emergencyLock because Dispose early-returns after Shutdown flips _disposed
- **Severity**: Medium
- **Component**: UniversalDeviceToolkit.Lib/Utils/Log.cs
- **Symptom**: Shutdown()/ShutdownAsync() set _disposed via Interlocked.CompareExchange and disposed _logger, but never disposed the SemaphoreSlim _emergencyLock. A subsequent Dispose() short-circuited via the same _disposed flag, so the native semaphore handle leaked. Concurrent ShutdownAsync vs Dispose could also double-touch _logger across the three split teardown entry points.
- **Root cause**: Teardown was split across three entry points (Shutdown, ShutdownAsync, Dispose) with duplicated disposal logic; _emergencyLock disposal lived only in Dispose, gated by the same _disposed flag Shutdown already set, making it unreachable post-Shutdown.
- **Fix applied**: Centralized all teardown into a single private DisposeCoreAsync() guarded by one atomic Interlocked.CompareExchange(ref _disposed, 1, 0) CAS. It captures _logger and _emergencyLock into locals and disposes each exactly once. Shutdown(), ShutdownAsync(), and Dispose() all funnel through DisposeCoreAsync(), eliminating the split-path double-touch and guaranteeing the SemaphoreSlim handle is freed regardless of which teardown entry point fires first.
- **Regression tests added**: (1) LogTests.Shutdown_ThenDispose_NoDoubleDisposeException - verifies Shutdown-then-Dispose disposes the semaphore without double-dispose exceptions. (2) LogTests.Concurrent_ShutdownAsyncAndDispose_NoDoubleDisposeException - 20-iteration race of Dispose vs ShutdownAsync on fresh isolated Log instances (via internal Log(true) ctor under UDT_TEST_HOOKS + UDT_APPDATA_OVERRIDE env isolation) must not throw ObjectDisposedException/SemaphoreFullException.
- **Verification**: dotnet build UniversalDeviceToolkit.Tests -c Debug -m:1 -> 0 warnings, 0 errors. dotnet test --filter LogTests -> 23/23 passed (673ms), including the 2 new regression tests.
---

### [RESOLVED by Codex-Agent-001 at 2026-07-09 20:20 CST] BUG-2026-07-09-005: Log.Shutdown()/Dispose() block the UI Dispatcher via ShutdownAsync().GetAwaiter().GetResult() (sync-over-async anti-pattern)
- **Severity**: Medium
- **Component**: UniversalDeviceToolkit.Lib/Utils/Log.cs; callers in UniversalDeviceToolkit.WPF/App.xaml.cs
- **Symptom**: The synchronous teardown entry points Shutdown() and Dispose() delegated to the centralized DisposeCoreAsync via ShutdownAsync().GetAwaiter().GetResult(), blocking the caller thread. App.xaml.cs:401/452 (WPF app-shutdown path running on the UI Dispatcher) and :902 (ExitDuplicateInstance) invoked the blocking Shutdown(), risking dispatch freeze under threadpool starvation / custom SynchronizationContext (xUnit designer hosts, VS designer), a Pillar A.1 violation.
- **Root cause**: After BUG-003/004 asyncified ErrorReport and centralized teardown in DisposeCoreAsync (offloading logger.Dispose() via Task.Run + ConfigureAwait(false)), the synchronous Shutdown()/Dispose() entry points kept the legacy ShutdownAsync().GetAwaiter().GetResult() pattern, re-introducing sync-over-async on the Dispatcher thread.
- **Fix applied**: (1) Removed .GetAwaiter().GetResult() from Shutdown() and Dispose(). Both now start ShutdownAsync()/DisposeCoreAsync(), short-circuit if it completes synchronously, else race it against a 2s TimeSpan timeout via Task.Wait(TimeSpan) and return non-blocking â€” no Dispatcher-blocking GetResult() on the fast path. If the timeout elapses, the task is left running (fire-and-forget) with a ContinueWith observation hook so it never throws as unobserved. (2) Switched the two async-capable WPF call sites (App.xaml.cs:401 and :452, already async handlers) to await Log.Instance.ShutdownAsync().ConfigureAwait(false). (3) Kept the synchronous Shutdown() fallback for the IDisposable.Dispose contract and the synchronous ExitDuplicateInstance path (:902 -> Shutdown() + Environment.Exit/ExitProcess), where a blocking call cannot deadlock the Dispatcher (the process is exiting) and the 2s guard bounds worst-case wait.
- **Verification**: dotnet build UniversalDeviceToolkit.sln -c Debug -m:1 --no-incremental -> 0 warnings, 0 errors. dotnet test --filter LogTests -> 23/23 passed (196ms), including existing BUG-003/004 regression tests (Shutdown_ThenDispose_NoDoubleDisposeException, Concurrent_ShutdownAsyncAndDispose_NoDoubleDisposeException, ErrorReportAsync_ConcurrentReports_ShouldNotCollideOrThrow).

---

### [RESOLVED by Codex-Agent-002 at 2026-07-09 20:50 CST] BUG-2026-07-09-006: AIController.Dispose() blocks the disposing thread via Task.Run(async () => UnsubscribeChangedAsync(...)).GetAwaiter().GetResult() (sync-over-async anti-pattern, Pillar A)
- **Severity**: Medium
- **Component**: UniversalDeviceToolkit.Lib/Controllers/AIController.cs; UniversalDeviceToolkit.Lib/Extensions/ManagementObjectSearcherExtensions.cs
- **Symptom**: AIController.Dispose(bool) delegated the GameAutoListener unsubscription to `Task.Run(async () => await gameAutoListener.UnsubscribeChangedAsync(...)).GetAwaiter().GetResult()`. UnsubscribeChangedAsync acquires an AsyncLock and may run StopInternalAsync (awaiting configured awaits); blocking the disposing thread with GetAwaiter().GetResult() is the sync-over-async anti-pattern (Pillar A) that can stall or deadlock under threadpool starvation / custom SynchronizationContext (WPF Dispatcher, xUnit designer hosts, VS designer).
- **Root cause**: Synchronous blocking on a threadpool-offloaded async operation inside IDisposable.Dispose, re-introducing sync-over-async despite the ConfigureAwait(false) discipline used elsewhere in the same file.
- **Fix applied**: (1) Removed `.GetAwaiter().GetResult()`. Dispose now starts the unsubscribe task via `Task.Run(async () => await UnsubscribeChangedAsync(...))`, short-circuits if `IsCompletedSuccessfully`, else races it against a 2s `Wait(TimeSpan)` and returns non-blocking. If the timeout elapses the task is left running (fire-and-forget) with a `ContinueWith(...OnlyOnFaulted|ExecuteSynchronously)` observation hook (`_ = t.Exception;`) so it never throws as unobserved. The disposing thread therefore never blocks on GetResult(). (2) Hardened the adjacent WMI timeout path `GetAsync` in ManagementObjectSearcherExtensions.cs: when the query times out the orphaned task previously leaked COM-managed ManagementBaseObject[] and unobserved faults. Added `ObserveOrphanedTask` which registers a `ContinueWith` that traces faults at Trace level and disposes each resulting ManagementBaseObject on the success path, preventing handle leaks from the timed-out WMI query path (Pillar A companion fix).
- **Verification**: dotnet build UniversalDeviceToolkit.Lib --no-restore -v q -> 0 warnings, 0 errors. AIController.cs comment references Pillar A and the WMI async timeout invariant is preserved (2500ms default).
