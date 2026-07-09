# Multi-Document Bug Queue - New & Unassigned

> Repository: UniversalDeviceToolkit (Main Repo, .NET 10 WPF)
> Writer: Codex + OpenCode Bug Reporter. New defects only.
> Lifecycle: claim -> 2_IN_PROGRESS.md with [CLAIMED by <Agent-ID> at <Timestamp>] -> fix & verify (dotnet test/dotnet build) -> 3_RESOLVED.md -> archive 4_ARCHIVED.md -> transcribe root cause to KNOWLEDGE_BASE.md.
> HYGIENE: Do NOT append audit-log / no-finding stubs to this file. Only write CONFIRMED new defect tickets here. When you find no defects, write NOWHERE.

## New Tickets

- [x] **[BUG-2026-07-09-005]** [Thread Safety / Sync-over-async] Log.Shutdown() and Log.Dispose() block the caller thread via ShutdownAsync().GetAwaiter().GetResult() in UniversalDeviceToolkit.Lib/Utils/Log.cs. CLAIMED by Codex-Agent-001 at 2026-07-09 20:10 CST -> resolved; see 3_RESOLVED.md.
- [x] **[BUG-2026-07-09-006]** [Thread Safety / Sync-over-async] AIController.Dispose() blocks the disposing thread via Task.Run(async () => UnsubscribeChangedAsync(...)).GetAwaiter().GetResult() in AIController.cs. CLAIMED by Codex-Agent-002 at 2026-07-09 20:45 CST -> resolved; see 3_RESOLVED.md.
- [x] **[BUG-2026-07-09-007]** [Thread Safety / Non-atomic dispose] Non-atomic check-then-set _disposed in AIController.Dispose and GPUController.Dispose allows concurrent double-dispose of TelemetryDispatcher/CTS/Process objects (Pillar A). CLAIMED by Codex-Agent-003 at 2026-07-09 21:30 CST -> in progress; see 2_IN_PROGRESS.md.

- [x] **[BUG-2026-07-09-008]** [Thread Safety / Sync-over-async + Non-atomic dispose] BatteryDischargeRateMonitorService.Dispose(bool) (UniversalDeviceToolkit.Lib/Services/BatteryDischargeRateMonitorService.cs:205) blocks the disposing thread via taskToWait?.Wait(TimeSpan.FromSeconds(5)) -- sync-over-async anti-pattern. StopAsync() already uses non-blocking await taskToWait.WaitAsync(...) (line 158), but Dispose breaks parity by blocking. Additionally the if (_disposed) return; at line 195 is a non-atomic check-then-set that allows concurrent double-dispose of the CTS/refresh task (Pillar A). CLAIMED by Codex-Agent-001 at 2026-07-09 22:05 CST -> resolved; see 3_RESOLVED.md.
NOWHERE (no further confirmed new defect tickets at this time).

