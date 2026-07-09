# Multi-Document Bug Queue - In Progress (Claimed & Locked)

> Repository: UniversalDeviceToolkit-Plugins (.NET 10 plugins)
> Maintain an atomic claim lock: tag each ticket moved here from 1_NEW_REPORTS.md with [CLAIMED by <Agent-ID> at <Timestamp>]. On fix + verification, move to 3_RESOLVED.md; release the lock back to 1_NEW_REPORTS.md if abandoned.

---

_No in-progress tickets._

_Audit log (compacted 2026-07-09, Hermes): PLG-020 resolved (governance-gate.ps1 created + CI wired). Next cursor: UDT-022, PLG-010, VSR-013._
---

### PLG-VIVE-REDESIGN [CLAIMED by Codex at 2026-07-09T15:40:35+08:00]
- **Plugin:** ViveTool (ViveToolPage.xaml)
- **Defect:** Pillar A violation — monolithic single-scroll StackPanel (hero header + warning banners + search toolbar + large DataGrid) exceeding the 2-screen vertical length rule. No modular TabControl tabs; telemetry summary buried as plain text in hero column.
- **Fix Plan:** Refactor ViveToolPage.xaml into a modular TabControl (Dashboard / Feature Flags / Settings) following the CustomMouse/BatteryHealth pattern. Hero + metric cards on Dashboard, search/filter toolbar + DataGrid on Feature Flags, warning/tool-status cards on a grouping panel. 100% host theme brush binding, responsive Grid star-sizing, zero hardcoded hex colors, zero emojis, zero rigid pixel widths.
- **Status:** In progress.

### PLG-VIVE-NOTIF [RESOLVED by Hermes at 2026-07-09]
- **Plugin:** ViveTool (ViveToolPage.xaml.cs)
- **Defect:** Three success notifications incorrectly called `ShowSnackbarError` instead of `ShowSnackbar`: (1) EnableFeatureButton_Click success branch (L1166), (2) DisableFeatureButton_Click success branch (L1202), (3) ExportButton_Click success branch (L1028). This caused success messages (Feature Enabled, Feature Disabled, Export Success) to render with error styling (red Snackbar) — and if the host Snackbar was unavailable, an Error MessageBox would pop up for a successful operation.
- **Resolution:** Changed all three call sites from `ShowSnackbarError` to `ShowSnackbar`. Build clean (0 warnings/0 errors). ViveTool.Tests: 229 passed / 0 failed.