# Task Plan

## Goal
Remove stale "Lenovo Legion Toolkit" / "LenovoLegionToolkit" references from PluginTooling.Core that produce incorrect user-visible output. Three instances: (1) PluginScaffolder hardcodes the legacy product name in default plugin descriptions, (2) StoreJsonGenerator has two stale legacy DLL hash candidates that will never match real plugin DLLs.

## Baseline
- HEAD: 49d64b6 (revision 65, pushed)
- Prior fixes: null-collection guards (59,62), duplicate entry dedup (60), SemVer validation (61), telemetry baselines (63), mojibake Chinese (64), stale DLL name in DoctorService (65)
- Current canonical verification: 629 tests passed, 0 failed, 0 warnings, 0 errors

## Scope
- `Tools/PluginTooling.Core/PluginScaffolder.cs` — line 42: replace "Lenovo Legion Toolkit" with "Universal Device Toolkit"
- `Tools/PluginTooling.Core/StoreJsonGenerator.cs` — lines 343, 349: remove stale legacy DLL hash candidates
- `Tests/PluginTooling.Tests/PluginScaffolderTests.cs` — add regression test verifying default description references "Universal Device Toolkit" not legacy name

## Steps
1. Patch `PluginScaffolder.cs` line 42: "Lenovo Legion Toolkit" → "Universal Device Toolkit"
2. Patch `StoreJsonGenerator.cs` lines 343, 349: remove legacy `LenovoLegionToolkit.Plugins.*.dll` candidates
3. Add regression test: scaffold a plugin without explicit description, verify description contains "Universal Device Toolkit" and not "Lenovo Legion Toolkit"
4. Add regression test: verify `TryComputeMainDllHashFromZip` candidate set does not include any `LenovoLegionToolkit.*` patterns
5. Run focused tests
6. Run canonical verify-hermes.ps1

## Verification
- Focused: `dotnet test Tests/PluginTooling.Tests/ --filter "FullyQualifiedName~ScaffolderDefaultDescription|FullyQualifiedName~LegacyDllCandidates"`
- Full: `dotnet test Tests/PluginTooling.Tests/`
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`

## Risks
- Low risk: description string change is user-visible but only affects new plugins scaffolded without explicit description
- Low risk: removing legacy DLL hash candidates is safe — the primary candidates (ExpectedAssemblyName.dll, UniversalDeviceToolkit.Plugins.*.dll) already cover correct naming
- No breaking change to existing plugins or contracts

## Stop Conditions
- Stop after one coherent increment (all 3 stale references fixed + tests)
- Stop if canonical verification fails

## Evidence
- Focused: `dotnet test --filter "FullyQualifiedName~PluginScaffolderLegacyNameTests"` — exit 0, 4 passed, 0 failed (316 ms)
- Full PluginTooling.Tests: exit 0, 53 passed, 0 failed (749 ms)
- Canonical verify-hermes.ps1: exit 0, 0 warnings, 0 errors
  - BatteryHealth: 71, CustomMouse: 52, ShellIntegration: 157, NetworkAcceleration: 66+2 skipped, ViveTool: 234
  - Total: 580 plugin + 53 tooling = 633 passed, 0 failed
- Ad-hoc script: hermes-verify-rev66.sh (created, run, deleted)
- Diff: +18 -6 in PluginScaffolder.cs, +0 -2 in StoreJsonGenerator.cs, +90 in new test file
