# Goal Contract

## Outcome
Deliver one bounded, verified improvement to Universal Device Toolkit Plugins per revision. Fix a real plugin defect or invariant, cover it with focused tests, pass the configured verification, and leave a reviewable diff.

## Required Evidence
- Exact defect and reproduction or invariant.
- Changed files and relevant callers/interfaces.
- Focused regression test result.
- `scripts/verify-hermes.ps1` result.
- Remaining baseline failures and risks.

## Constraints
- One writable Worker Session for this repository.
- Preserve unrelated existing changes.
- Do not reset, clean, stash, force-push, amend, rebase, or switch branches.
- Do not claim completion while configured verification fails.

## Stop Conditions
Stop and request review when the bounded defect is fixed and verified, when a genuine secret/privilege boundary is reached, or when the same reproducible blocker remains after two focused attempts.
