# Task Plan
## Goal
Make the configured verification command pass without hiding baseline failures.
## Baseline
Use the current workspace HEAD and the supplied evidence as baseline.
## Scope
Only edit paths inside the project's allowed_paths; do not touch unrelated local work.
## Steps
1. Inspect git status and the current evidence.
2. Implement: Fix the failing verification command and provide fresh evidence.
3. Add or update a focused regression check when behavior is testable.
4. Run the configured Hermes verification command.
5. Update the active task-plan Evidence section with commands and outcomes.
## Verification
Run the project verification script and require exit code 0.
## Risks
Partial fix, wrong file, or unverified behavior.
## Stop Conditions
Stop after one coherent increment if verification passes or a human-only blocker appears.
## Evidence
Fill after execution with exact commands, exit codes, and key log lines.

## Master Report
165th review. HEAD 8879532 on origin/master. 14 commits verified. Working tree clean — cosmetic UI change reverted. No new source changes. ORCHESTRATOR: kill this session.
