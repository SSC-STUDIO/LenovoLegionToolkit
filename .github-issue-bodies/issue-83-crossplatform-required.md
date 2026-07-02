## Summary

The main PR gate (`Ci-tests.yml`) only runs `UniversalDeviceToolkit.Tests`. Cross-platform diagnostics tests run in a separate workflow (`CrossPlatformCli.yml`) and may not block merges if not configured as required checks.

## Impact

- Regressions in `UniversalDeviceToolkit.CrossPlatform` CLI can merge without running on all three OS runners
- Release workflow tests Windows suite only before publish

## Proposed fix

1. Add `CrossPlatformCli.yml` as a required status check for `master`
2. Optionally run cross-platform tests in Release workflow before asset publish (v5+ CLI asset)

## References

- `.github/workflows/CrossPlatformCli.yml`
- `.github/workflows/Ci-tests.yml`

Reported by DevOps & QA audit (co-mcp-agent-9).
