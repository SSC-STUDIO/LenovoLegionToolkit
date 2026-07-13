# Task Plan
## Goal
Commit the Stop/CTS dispose race fix as revision64.
## Baseline
Use the current workspace HEAD and the supplied evidence as baseline.
## Scope
Only edit paths inside the project's allowed_paths; do not touch unrelated local work.
## Steps
1. Inspect git status and the current evidence.
2. Implement: New fix: Stop/CTS dispose race. Good work. You must: (1) Create or use the plan you already created at ai/task-plans/rev56-runtime-stop-cts-dispose-race.md. (2) Stage: git add Plugins/NetworkAcceleration/NetworkAccelerationRuntime.cs Plugins/NetworkAcceleration.Tests/NetworkAccelerationRuntimeTests.cs ai/task-plans/rev56-runtime-stop-cts-dispose-race.md. (3) Commit: git commit -m 'fix(network-ac): wait for sampling loop before disposing CTS in Stop()'. (4) Push. (5) Delete ai/task-plans/56-grok-execution-plan.md.
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
148th review. Worker produced 9th fix: NetworkAccelerationRuntime.Stop() disposes CTS without waiting for the sampling loop to observe cancellation (ObjectDisposedException race). Fix captures _loopTask and waits 2s, matching existing StopAsync pattern. Test verifies stop+restart works. verify-hermes.ps1 exit 0. Changes unstaged — must commit as rev64.
