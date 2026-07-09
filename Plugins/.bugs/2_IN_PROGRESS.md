# Multi-Document Bug Queue - In Progress (Claimed & Locked)

> Repository: UniversalDeviceToolkit-Plugins (.NET 10 plugins)
> Maintain an atomic claim lock: tag each ticket moved here from 1_NEW_REPORTS.md with [CLAIMED by <Agent-ID> at <Timestamp>]. On fix + verification, move to 3_RESOLVED.md; release the lock back to 1_NEW_REPORTS.md if abandoned.

---

- [ ] **[PLG-020]** [Governance / CI Enforcement — OPEN] Archived tickets PLG-001 and PLG-003 mandate a CI grep gate to enforce two zero-regression contracts at compile/PR time: (1) zero `ConfigureAwait(false)` in plugin UI code-behind (`**/*.xaml.cs`); (2) zero `MessageBox.Show` in plugin code-behind (`Plugins/**/*.cs`, excluding the allowed host shim `WpfHostNotifications.cs`). No such gate exists today; the contracts are enforced only by manual audit, so a single careless edit can re-introduce the async-void crash (PLG-013) / modal-dialog (PLG-003) regressions. *Plan*: Add a deterministic pure-PowerShell governance gate `Scripts/governance-gate.ps1` covering 4 rules (ConfigureAwait(false) in *.xaml.cs; MessageBox.Show in plugin code-behind; hardcoded hex colors in plugin *.xaml; async void handler missing try-catch). Wire it into `.github/workflows/validate.yml` as a standalone step that runs before the per-plugin .NET validation. Run locally to confirm zero violations. [CLAIMED by Codex-Agent-01 at 2026-07-09T]