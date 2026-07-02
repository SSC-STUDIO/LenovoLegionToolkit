## Summary

CLI (`llt.exe`) communicates with the running WPF app via IPC (`IpcServer`), which starts during background initialization (after MainWindow is shown). There is no documented retry/backoff when the app is still starting.

## Impact

- Users/scripts calling CLI immediately after launch may get failures despite app running
- CI smoke tests CLI DLL directly, not the IPC startup race

## Proposed fix

1. CLI client: retry with backoff when IPC endpoint not ready (configurable timeout)
2. Optional: expose `status` readiness field indicating background init complete
3. Add integration test for CLI call during startup window

## References

- `UniversalDeviceToolkit.CLI/Program.cs`
- `UniversalDeviceToolkit.WPF/App.xaml.cs` (`StartBackgroundInitialization`, `IpcServer`)

Reported by DevOps & QA audit (co-mcp-agent-9).
