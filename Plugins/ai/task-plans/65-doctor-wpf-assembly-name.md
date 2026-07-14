# Task Plan

## Goal
Fix DoctorService WPF assembly check that hardcodes the stale legacy name "Lenovo Legion Toolkit.dll" instead of reading the actual WPF artifact name from host-release.json. The doctor check always reports FAIL because the real DLL is "Universal Device Toolkit.dll".

## Baseline
- HEAD: 7f92cf5 (revision 64, pushed)
- Working tree clean except untracked zombie file (not ours)
- DoctorService.cs line 24: `var wpfPath = Path.Combine(repository.HostDependenciesRoot, "Lenovo Legion Toolkit.dll");`
- host-release.json line 9: `"wpf": "Universal Device Toolkit.dll"`
- No HostReleaseManifest model exists in Models.cs
- No DoctorService tests exist

## Scope
- Tools/PluginTooling.Core/DoctorService.cs — read host-release.json, use wpf artifact name
- Tools/PluginTooling.Core/Models.cs — add HostReleaseManifest model with Artifacts.Wpf
- Tests/PluginTooling.Tests/DoctorServiceTests.cs — new regression test

## Steps
1. Add HostReleaseManifest model to Models.cs (with nested HostReleaseArtifacts class containing Wpf property)
2. Modify DoctorService.Run to:
   a. If host-release.json exists and parses, use its artifacts.wpf name for the WPF DLL check
   b. Fall back to "Universal Device Toolkit.dll" (current correct default) if not parseable
   c. Never use the stale "Lenovo Legion Toolkit.dll" name
3. Write DoctorServiceTests.cs with tests:
   a. Test that doctor check uses the WPF name from host-release.json
   b. Test that fallback uses correct name when host-release.json missing
4. Run focused tests and canonical verification

## Verification
- Focused: `dotnet test Tests/PluginTooling.Tests/ --filter "FullyQualifiedName~DoctorServiceTests"`
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`

## Risks
- DoctorService.Run requires PluginRepository.Load which needs .sln + Plugins/ dir — test must create minimal temp repo structure
- JSON deserialization must handle the BOM prefix (ReadJsonFile already strips BOM)
- Low risk: only changes doctor diagnostic output, not runtime behavior

## Stop Conditions
- Focused tests pass AND canonical verify-hermes.ps1 exits 0
- Stop after one coherent increment

## Evidence
- Focused: `dotnet test Tests/PluginTooling.Tests/ --filter "FullyQualifiedName~DoctorServiceTests"` — exit 0, 4 passed, 0 failed, 0 skipped (75 ms)
- Full PluginTooling.Tests: exit 0, 49 passed, 0 failed, 0 skipped (720 ms)
- Canonical verify-hermes.ps1: exit 0, 0 warnings, 0 errors
  - BatteryHealth: 71 passed, CustomMouse: 52 passed, ShellIntegration: 157 passed
  - NetworkAcceleration: 66 passed + 2 skipped, ViveTool: 234 passed
  - Total: 580 plugin tests + 49 tooling tests = 629 passed, 0 failed
- Diff: +55 -2 across 2 product files, +1 test file (145 lines), +1 plan file
