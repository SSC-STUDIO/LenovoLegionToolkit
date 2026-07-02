## Summary

`Tools/VisualRegression.Smoke` provides OSD/dashboard/settings visual capture and diff (local artifacts under `output/osd-visual-verify/`), but no GitHub Actions workflow runs it automatically. `MainAppPluginUi.Smoke.yml` is `workflow_dispatch` only on a self-hosted runner.

## Impact

- UI/OSD visual regressions can ship without automated detection
- Visual verification relies on manual local runs

## Proposed fix

1. Add scheduled workflow (e.g. nightly) running VisualRegression key scenarios on self-hosted runner
2. Upload screenshot artifacts + JSON metadata on failure
3. Wire OSD overlay scenario into CI after baseline is committed

## References

- `Tools/VisualRegression.Smoke/Program.cs`
- `.github/workflows/MainAppPluginUi.Smoke.yml`

Reported by DevOps & QA audit (co-mcp-agent-9).
