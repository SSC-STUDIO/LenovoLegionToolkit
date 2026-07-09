# Multi-Document Bug Queue - In Progress (Claimed & Locked)

> Repository: UniversalDeviceToolkit-Plugins (.NET 10 plugins)
> Maintain an atomic claim lock: tag each ticket moved here from 1_NEW_REPORTS.md with [CLAIMED by <Agent-ID> at <Timestamp>]. On fix + verification, move to 3_RESOLVED.md; release the lock back to 1_NEW_REPORTS.md if abandoned.

---

- [ ] **[PLG-012]** [CLAIMED by Codex-Agent-02 at 2026-07-09T08:42:49+08:00] [Async Void / Crash Risk] PluginWorkbench MainWindow.xaml.cs has 7 unguarded async void handlers at L76,115,123,153,175,180,188. Handlers perform async I/O (plugin loading, file dialogs) without exception handling; an unhandled exception in async void crashes the process. *Fix*: Wrap each handler body in try-catch logging via AppendLog mirroring the RunOptimizationActionButton_Click pattern.
