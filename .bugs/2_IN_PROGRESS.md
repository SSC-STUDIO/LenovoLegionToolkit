# Multi-Document Bug Queue - In Progress (Claimed & Locked)

> Repository: UniversalDeviceToolkit (Main, .NET 10 / WPF)
> Maintain an atomic claim lock: tag each ticket moved here from 1_NEW_REPORTS.md with [CLAIMED by <Agent-ID> at <Timestamp>]. On fix + verification, move to 3_RESOLVED.md; release the lock back to 1_NEW_REPORTS.md if abandoned.

---

- [ ] **[UDT-022]** [Resource Leak] CancellationTokenSource not disposed in DriverKeyListener.cs:55. StopAsync sets _cancellationTokenSource = null after CancelAsync() but never disposes; internal timer/registration resources leak until GC finalizer.
  - **Claimed**: [CLAIMED by Codex at 2026-07-09 06:18:14]

- [ ] **[UDT-026]** [Code Quality / Event Handler Leak] DashboardGroupControl subscribes to child IsVisibleChanged without unsubscribe (DashboardGroupControl.cs:~L71). Violates subscribe/unsubscribe pairing pattern.
  - **Claimed**: [CLAIMED by Codex at 2026-07-09 06:18:14]

- [ ] **[UDT-027]** [Code Quality / Event Handler Leak] SettingsPage subscribes IsVisibleChanged += SettingsPage_IsVisibleChanged (SettingsPage.xaml.cs:~L42) with no unsubscribe anywhere.
  - **Claimed**: [CLAIMED by Codex at 2026-07-09 06:18:14]

- [ ] **[UDT-028]** [Code Quality / Event Handler Leak] AddAutomationStepWindow subscribes IsVisibleChanged += AddAutomationStepWindow_IsVisibleChanged (AddAutomationStepWindow.xaml.cs:~L24) with no Closed detach.
  - **Claimed**: [CLAIMED by Codex at 2026-07-09 06:18:14]

- [ ] **[UDT-029]** [Code Quality / Event Handler Leak] PluginExtensionsPage subscribes IsVisibleChanged += PluginExtensionsPage_IsVisibleChanged (PluginExtensionsPage.xaml.cs:~L61); existing Unloaded handler fails to detach it.
  - **Claimed**: [CLAIMED by Codex at 2026-07-09 06:18:14]
