# Multi-Document Bug Queue - In Progress (Claimed & Locked)

> Repository: UniversalDeviceToolkit-Plugins (.NET 10 plugins)
> Maintain an atomic claim lock: tag each ticket moved here from 1_NEW_REPORTS.md with [CLAIMED by <Agent-ID> at <Timestamp>]. On fix + verification, move to 3_RESOLVED.md; release the lock back to 1_NEW_REPORTS.md if abandoned.

---

_No claimed tickets. All tickets resolved and promoted to 3_RESOLVED.md or archived._

- [ ] **[PLG-013]** [Thread Safety / Unguarded async-void] BatteryHealthControl.xaml.cs RefreshButton_Click (L165) and ViveToolPage.xaml.cs StatusFilterComboBox_SelectionChanged (L1122) are async-void event handlers lacking try-catch guards. Per Pillar B / Governance °ÏA, all async-void UI handlers must be try-catch guarded to prevent unhandled exceptions crashing the host process. [CLAIMED by Codex-Agent-01 at 2026-07-09T07:21:55+08:00]

