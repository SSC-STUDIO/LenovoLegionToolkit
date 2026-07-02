## Summary

WPF `Application_Startup` in `App.xaml.cs` is an `async void` method (~1700 lines) orchestrating single-instance, compatibility, IoC, plugins, MainWindow, background init, and OSD.

## Impact

- Unhandled exceptions in async continuations may not surface reliably via `AppDomain_UnhandledException`
- Hard to unit-test startup ordering and failure paths
- Long-term maintenance risk as startup logic grows

## Proposed fix

1. Extract `StartupOrchestrator` with explicit `Task<int>` entry and top-level try/catch
2. Keep `Application_Startup` as thin dispatcher
3. Add integration tests for single-instance and IPC-ready timing

## References

- `UniversalDeviceToolkit.WPF/App.xaml.cs` (`Application_Startup`)
- Existing tests: `CrashReportStartupGuardTests`, `StartupDeviceSetupCoordinatorTests`

Reported by DevOps & QA audit (co-mcp-agent-9).
