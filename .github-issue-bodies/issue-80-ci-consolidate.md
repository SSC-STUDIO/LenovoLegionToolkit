## Summary

Three GitHub Actions workflows all trigger on `push`/`pull_request` to `master`, causing redundant builds and inconsistent test behavior:

| Workflow | Build config | Test config |
|----------|--------------|-------------|
| `Ci-tests.yml` | Release | Release + TFM `net10.0-windows10.0.26100.0` |
| `windows.yml` | Debug + Release | **Debug** only |
| `Build.yml` | Release via `Make.bat` | No tests |

## Impact

- Wasted CI minutes on every PR
- Possible "green Debug / red Release" drift
- Contributors cannot tell which workflow is the source of truth

## Proposed fix

1. Merge into a single PR gate workflow: restore → build Release → unit fail-fast → full test suite → CLI smoke
2. Keep `Build.yml` as nightly or `workflow_dispatch` for installer artifact validation
3. Remove or demote `windows.yml` after parity verification

## References

- `.github/workflows/Ci-tests.yml`
- `.github/workflows/windows.yml`
- `.github/workflows/Build.yml`
- `Docs/DEPLOYMENT.md`

Reported by DevOps & QA audit (co-mcp-agent-9).
