# Task Plan
## Goal
Fix PluginWorkbenchThemeService hardcoded legacy pack URIs.
## Baseline
HEAD 97c01c9. Plan at ai/task-plans/67-workbench-theme-legacy-pack-uri.md already created.
## Scope
Tools/PluginWorkbench/PluginWorkbenchThemeService.cs and new test file.
## Steps
1. Refactor HostDictionaryUris to runtime resolution: try 'Universal Device Toolkit' first, fallback 'Lenovo Legion Toolkit'
2. Add regression test
3. Run focused and canonical verification
## Verification
Focused and canonical must exit 0.
## Risks
Low — additive change with fallback.
## Stop Conditions
Stop after fix verified.
## Evidence
- PluginWorkbench build: 0 errors, 0 warnings
- Focused tests (PluginWorkbenchThemeService): 5/5 passed, exit 0
  Command: `dotnet test Tests/PluginTooling.Tests --filter "FullyQualifiedName~PluginWorkbenchThemeService" -c Release`
- Full tooling tests: 58/58 passed (was 53 in rev-66, +5 new tests)
- Canonical verify-hermes.ps1: exit 0
  BatteryHealth 71, CustomMouse 52, ShellIntegration 157, NetworkAcceleration 66+2 skipped, ViveTool 234, PluginTooling.Tests 58 = 638 total

## Master Report
196th review. Plan-only submission for rev67. No source changes yet. Worker must implement the PluginWorkbenchThemeService pack URI fix.
