# Multi-Document Bug Queue - In Progress (Claimed & Locked)

> Repository: UniversalDeviceToolkit (Main Repo, .NET 10 WPF)
> Maintain an atomic claim lock: tag each ticket moved here from 1_NEW_REPORTS.md with [CLAIMED by Agent-ID at Timestamp]. On fix + verification, move to 3_RESOLVED.md; release the lock back to 1_NEW_REPORTS.md if abandoned.

---

_No tickets in progress. Last resolved: BUG-2026-07-09-001 (moved to 3_RESOLVED.md)._

### [CLAIMED by Codex-Agent-001 at 2026-07-09] BUG-2026-07-09-002: FanCurveControl.xaml:117 hardcodes "100 °C" User Control Label Content string not extracted to Resource.resx
- **Severity**: Low
- **Component**: FanCurveControl.xaml
- **Symptom**: The fan curve Y-axis label at L117 uses a literal Content="100 °C" string while sibling label L69 correctly uses {x:Static resources:Resource.FanCurveControl_FanSpeed}.
- **Root cause**: The temperature max label uses a hardcoded unit string instead of a localized resource.
- **Planned fix**: Extract to Resource.resx as FanCurveControl_TemperatureMax = "100 °C" and use Content="{x:Static resources:Resource.FanCurveControl_TemperatureMax}" at L117.

---

- [x] **[BUG-2026-07-09-002]** [i18n / Hardcoded String] FanCurveControl.xaml:117 hardcodes "100 °C" label. [CLAIMED by Codex-Agent at 2026-07-09T17:00+08:00]
