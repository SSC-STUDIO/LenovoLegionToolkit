## Summary

Single-instance activation uses Mutex + EventWaitHandle with legacy name compatibility and ACK timeout (`SINGLE_INSTANCE_ACTIVATION_TIMEOUT_MS = 1200`), but there are no dedicated automated tests for duplicate-launch behavior.

## Impact

- Regressions in second-instance activation or legacy mutex handoff may go unnoticed
- Recovery mode (`RECOVERY_SINGLE_INSTANCE_SUFFIX`) is untested

## Proposed fix

- Add integration tests spawning two processes (or mocked Mutex/Event handles)
- Cover: duplicate exit, bring-to-front signal, legacy name fallback

## References

- `UniversalDeviceToolkit.WPF/App.xaml.cs` (`EnsureSingleInstance`, `ExitDuplicateInstance`)

Reported by DevOps & QA audit (co-mcp-agent-9).
