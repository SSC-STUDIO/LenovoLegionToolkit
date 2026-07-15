# Task Plan
## Goal
Publication only — commit and push already-approved rev67-68 changes.
## Baseline
Use the current workspace HEAD and the supplied evidence as baseline.
## Scope
Only edit paths inside the project's allowed_paths; do not touch unrelated local work.
## Steps
1. Inspect git status and the current evidence.
2. Implement: 199th review. STOP resubmitting. The code was already approved. Execute these publication commands NOW:

1. Delete the zombie file: `rm -f ai/task-plans/62-grok-execution-plan.md`
2. Stage ONLY approved files:
   `git add Tools/PluginWorkbench/PluginWorkbenchThemeService.cs Tests/PluginTooling.Tests/PluginWorkbenchThemeServiceLegacyPackUriTests.cs Plugins/ShellIntegration/ShellIntegrationConfigService.cs Plugins/ShellIntegration.Tests/ShellIntegrationConfigServiceTests.cs Plugins/CustomMouse/plugin.manifest.json Plugins/CustomMouse/store-entry.json Plugins/ShellIntegration/plugin.manifest.json Plugins/ShellIntegration/store-entry.json Plugins/ViveTool/plugin.manifest.json Plugins/ViveTool/store-entry.json ai/task-plans/67-workbench-theme-legacy-pack-uri.md ai/task-plans/68-shell-integration-legacy-comment.md`
3. Commit: `git commit -m 'fix(rebrand): replace legacy Lenovo Legion Toolkit references in ShellIntegration comments, workbench pack URIs, and plugin manifests'`
4. Push: `git push`

Do NOT resubmit for review. Do NOT add ai/task-plans/62-grok-execution-plan.md. Just execute steps 1-4 above.
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
199th review. Same diff resubmitted without changes. Worker must execute the commit+push publication step that was already approved in review 198. No further code review needed.
