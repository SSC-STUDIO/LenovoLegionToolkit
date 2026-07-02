## Summary

`UniversalDeviceToolkit.Tests/coverlet.runsettings` narrows coverage collection to `[LenovoLegionToolkit.Lib.Plugins]*` only. CI collects coverage but does not upload reports or enforce thresholds.

```xml
<Include>[LenovoLegionToolkit.Lib.Plugins]*</Include>
<!-- comment: Add Threshold when baseline is stable -->
```

## Impact

- Core Lib/WPF/CLI changes can ship without meaningful coverage signal
- Test debt can accumulate silently
- Release workflow runs coverage but never fails on regression

## Proposed fix

1. Upload Cobertura/OpenCover artifacts (or Codecov) on PR and Release
2. Phase in broader `Include` scopes (Plugins → Lib → CLI.Lib)
3. Add line-coverage threshold once baseline is measured (e.g. 60% → 70%)

## References

- `UniversalDeviceToolkit.Tests/coverlet.runsettings`
- `.github/workflows/Ci-tests.yml` (XPlat Code Coverage step)

Reported by DevOps & QA audit (co-mcp-agent-9).
