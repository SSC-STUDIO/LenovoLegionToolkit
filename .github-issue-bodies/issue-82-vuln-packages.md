## Summary

In `.github/workflows/Ci-tests.yml`, the step `Check for vulnerable packages` runs `dotnet list package --vulnerable --include-transitive` but failures are downgraded to warnings; the job still passes.

## Impact

- High/Critical CVEs can merge to `master` unnoticed
- Dependabot helps but does not replace runtime gate on transitive deps

## Proposed fix

- Fail the job on High/Critical vulnerabilities
- Allow Low/Medium as warnings during transition period
- Optionally add `dotnet list package --outdated` summary to PR checks

Reported by DevOps & QA audit (co-mcp-agent-9).
