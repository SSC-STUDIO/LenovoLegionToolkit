# Multi-Document Bug Queue - New & Unassigned

> Repository: UniversalDeviceToolkit (Main Repo, .NET 10 WPF)
> Writer: Codex + OpenCode Bug Reporter. New defects only.
> Lifecycle: claim -> 2_IN_PROGRESS.md with [CLAIMED by <Agent-ID> at <Timestamp>] -> fix & verify (dotnet test/dotnet build) -> 3_RESOLVED.md -> archive 4_ARCHIVED.md -> transcribe root cause to KNOWLEDGE_BASE.md.
> HYGIENE: Do NOT append audit-log / no-finding stubs to this file. Only write CONFIRMED new defect tickets here. When you find no defects, write NOWHERE.

## New Tickets

_No new tickets._

- [ ] **[BUG-2026-07-09-002]** [i18n / Hardcoded String] FanCurveControl.xaml:117 hardcodes "100 °C" as a User Control Label Content string not extracted to Resource.resx. *Root Cause*: The fan curve Y-axis label at L117 uses a literal Content="100 °C" string while sibling label L69 correctly uses {x:Static resources:Resource.FanCurveControl_FanSpeed}. The "100 °C" value is a user-facing unit string; the percent labels ("100%".."20%" at L39–L63) are numeric-neutral and acceptable. *Suggested Fix*: Extract to Resource.resx as e.g. FanCurveControl_TemperatureMax = "100 °C" and use Content="{x:Static resources:Resource.FanCurveControl_TemperatureMax}" at L117.
