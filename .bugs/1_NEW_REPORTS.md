# Multi-Document Bug Queue - New & Unassigned

> Repository: UniversalDeviceToolkit (Main Repo, .NET 10 WPF)
> Writer: Codex + OpenCode Bug Reporter. New defects only.
> Lifecycle: claim -> 2_IN_PROGRESS.md with [CLAIMED by <Agent-ID> at <Timestamp>] -> fix & verify (dotnet test/dotnet build) -> 3_RESOLVED.md -> archive 4_ARCHIVED.md -> transcribe root cause to KNOWLEDGE_BASE.md.
> HYGIENE: Do NOT append audit-log / no-finding stubs to this file. Only write CONFIRMED new defect tickets here. When you find no defects, write NOWHERE.

## New Tickets

- [x] **[BUG-2026-07-09-005]** [Thread Safety / Sync-over-async] Log.Shutdown() and Log.Dispose() block the caller thread via ShutdownAsync().GetAwaiter().GetResult() in UniversalDeviceToolkit.Lib/Utils/Log.cs. CLAIMED by Codex-Agent-001 at 2026-07-09 20:10 CST -> resolved; see 3_RESOLVED.md.

NOWHERE (no further confirmed new defect tickets at this time).
