# Multi-Document Bug Queue - In Progress (Claimed & Locked)

> Repository: UniversalDeviceToolkit (Main, .NET 10 / WPF)
> Maintain an atomic claim lock: tag each ticket moved here from 1_NEW_REPORTS.md with [CLAIMED by <Agent-ID> at <Timestamp>]. On fix + verification, move to 3_RESOLVED.md; release the lock back to 1_NEW_REPORTS.md if abandoned.

---

- [ ] **[UDT-025]** [Reliability / ReflectionTypeLoadException] Static constructors in AutomationJsonConverters.cs use `Assembly.GetTypes()` without `ReflectionTypeLoadException` handling at lines 38, 65, 155, 191, 218, 264. If any type in the assembly fails to load (missing dependency), `GetTypes()` throws `ReflectionTypeLoadException`; since these are static constructors, the exception propagates as `TypeInitializationException`, making the entire converter (and all JSON serialization of automation pipelines) unusable. PluginLoader.cs:122 already demonstrates the correct SafeGetTypes pattern.
  - **Claimed**: [CLAIMED by Codex at 2026-07-09 05:24:34]

