# Task Plan

## Goal
Fix BumpSemVer to reject non-standard version strings (2-part "1.2" and 4-part "1.2.3.4") that silently produce incorrect bumped versions or lose data. Enforce strict 3-part SemVer (major.minor.patch) input validation.

## Baseline
- HEAD: ba0d976 (docs(plan): rewrite revision-60 plan with full defect analysis and verification evidence)
- Working tree: clean
- BumpSemVer at PluginVersionSynchronizer.cs line 44 uses `Version.TryParse` which accepts 2-part and 4-part versions, silently dropping components or producing wrong results
- Existing tests only cover 3-part inputs — the edge cases are untested

## Scope
- Tools/PluginTooling.Core/PluginVersionSynchronizer.cs — replace Version.TryParse with strict 3-part SemVer validation
- Tests/PluginTooling.Tests/PluginVersionSynchronizerTests.cs — add regression tests for rejected 2-part and 4-part inputs
- ai/task-plans/61-bumpsemver-strict-validation.md — this plan file

## Steps
1. Replace `Version.TryParse` in BumpSemVer with a regex match for `^\d+\.\d+\.\d+$` to enforce exactly 3-part SemVer.
2. Parse major, minor, patch from the regex groups instead of `Version` struct.
3. Bump the correct component and return the 3-part result string.
4. Add regression tests: BumpSemVer rejects "1.2" (2-part), rejects "1.2.3.4" (4-part), rejects "1.2.3-alpha" (pre-release suffix), rejects empty string.
5. Run focused tests and canonical verification.

## Verification
Focused: `dotnet test Tests/PluginTooling.Tests/PluginTooling.Tests.csproj -c Release --filter "FullyQualifiedName~PluginVersionSynchronizerTests"`
Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`

## Risks
- Breaking change: if any plugin currently uses a 2-part or 4-part version in plugin.manifest.json, `--part` bump will now throw FormatException instead of silently producing wrong output. This is correct — the old behavior was a silent data-loss bug.
- No risk to existing 3-part SemVer inputs — the regex `^\d+\.\d+\.\d+$` matches exactly what `Version.TryParse` accepted for 3-part versions.
- No callers other than `Bump()` which passes through `plugin.UnifiedManifest.Version` — validation will catch malformed versions at bump time rather than silently corrupting them.

## Stop Conditions
Stop after verification passes with the new strict validation and regression tests in place.

## Evidence

### Focused tests
Command: `dotnet test Tests/PluginTooling.Tests/PluginTooling.Tests.csproj -c Release --filter "FullyQualifiedName~PluginVersionSynchronizerTests"`
Result: 11 passed, 0 failed, exit 0 (143 ms)
New tests: BumpSemVer_RejectsNonThreePartSemVer (5 inline cases: "1.2", "1.2.3.4", "1.2.3-alpha", "", "   ")

### Canonical verification
Command: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`
Result: 0 warnings, 0 errors, exit 0
- BatteryHealth.Tests: 71 passed, 0 failed
- CustomMouse.Tests: 52 passed, 0 failed
- ShellIntegration.Tests: 157 passed, 0 failed
- NetworkAcceleration.Tests: 63 passed, 2 skipped
- ViveTool.Tests: 234 passed, 0 failed

### Changed files
- Tools/PluginTooling.Core/PluginVersionSynchronizer.cs (+16 -7): BumpSemVer replaced Version.TryParse with regex-validated 3-part SemVer; added SemVerRegex source generator; class marked partial.
- Tests/PluginTooling.Tests/PluginVersionSynchronizerTests.cs (+11): added BumpSemVer_RejectsNonThreePartSemVer theory with 5 cases.
- ai/task-plans/61-bumpsemver-strict-validation.md: this plan file.
