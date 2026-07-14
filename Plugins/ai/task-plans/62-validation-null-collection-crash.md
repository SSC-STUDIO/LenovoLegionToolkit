# Task Plan

## Goal
Fix two `NullReferenceException` crashes in `PluginValidationService` when a plugin manifest's JSON contains explicit `null` tokens for `optimizationActions` (line 205) or `requiredFiles` (line 468). `System.Text.Json` overwrites the C# `[]` collection initializers with `null` when the JSON has `"optimizationActions": null` or `"requiredFiles": null`. The validation service iterates these without null guards, causing NRE. Same defect class as revision 59 (null collections), but in `PluginValidationService` which was missed.

## Baseline
- HEAD: `df43926` (revision-61 BumpSemVer strict validation). Working tree clean.
- Revision 59 fixed `PluginManifestMigrator` null collection guards. `PluginPackager` already had `?? []` guard on `RequiredFiles`.
- `PluginValidationService` was missed — both `ValidateUnifiedManifest` (line 205) and `ValidatePackageContents` (line 468) iterate null-vulnerable collections.
- `Models.cs` line 106: `OptimizationActions { get; set; } = []` — overwritten to `null` by System.Text.Json when JSON has `"optimizationActions": null`.
- `Models.cs` line 139: `RequiredFiles { get; set; } = []` — same issue.
- Existing tests: `StoreJsonGeneratorTests`, `PluginVersionSynchronizerTests`. No `PluginValidationService` tests exist.

## Scope
Owned files:
- `Tools/PluginTooling.Core/PluginValidationService.cs` — add `?? []` null guards to both `foreach` loops
- `Tests/PluginTooling.Tests/PluginValidationServiceTests.cs` — NEW: regression tests for null-collection tolerance
- `ai/task-plans/62-validation-null-collection-crash.md` — this plan document

## Steps
1. Write this task plan.
2. Fix `PluginValidationService.cs` line 205: `manifest.Contributes.OptimizationActions` → `(manifest.Contributes.OptimizationActions ?? [])`
3. Fix `PluginValidationService.cs` line 468: `plugin.UnifiedManifest.Package.RequiredFiles` → `(plugin.UnifiedManifest.Package.RequiredFiles ?? [])`
4. Create `Tests/PluginTooling.Tests/PluginValidationServiceTests.cs` with two tests:
   - `RunAsync_ToleratesNullOptimizationActions`: creates a plugin with `"optimizationActions": null` in manifest, runs validation with `SkipBuild=true`, `SkipTests=true`, asserts no exception thrown and report has 0 failures from NRE.
   - `RunAsync_ToleratesNullRequiredFilesWithOutputDir`: creates a plugin with `"requiredFiles": null` in manifest, creates a fake `OutputDirectory` with a dummy file so `ValidatePackageContents` is reached, runs validation, asserts no NRE.
5. Run focused tests: `dotnet test ... --filter "FullyQualifiedName~PluginValidationServiceTests"`
6. Run canonical verification: `scripts/verify-hermes.ps1`
7. Update plan evidence section with exact commands and results.
8. Stage, commit, push.

## Verification
-.Focused test command: `dotnet test Tests/PluginTooling.Tests/PluginTooling.Tests.csproj -c Release --filter "FullyQualifiedName~PluginValidationServiceTests"`
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`
- Expected: focused tests pass (2 tests), canonical 0 errors/0 warnings.

## Risks
- The `PluginValidationService.RunAsync` method is async and calls `_repository.Load` which requires a full repo structure (`.sln` + `Plugins/` dir). Test must create proper fixture.
- `ValidatePackageContents` only runs when `Directory.Exists(plugin.OutputDirectory)` — test must create a fake output directory to reach the null-guarded `foreach` loop.
- The `PluginContext.OutputDirectory` is derived from `plugin.DirectoryPath` and `ExpectedAssemblyName` — need to check how it's computed to create the right path.

## Stop Conditions
- If build fails after edit — re-read file and fix.
- If focused tests fail — debug and fix.
- If canonical verification fails — stop and report.
- If diff hash matches any forbidden value — stop and report.

## Evidence

### Focused tests
Command: `dotnet test Tests/PluginTooling.Tests/PluginTooling.Tests.csproj -c Release --filter "FullyQualifiedName~PluginValidationServiceTests"`
Result: 2 passed, 0 failed, 0 skipped, exit 0 (88 ms)

### Canonical verification
Command: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`
Result: 0 warnings, 0 errors, exit 0
- BatteryHealth: 71 passed
- CustomMouse: 52 passed
- ShellIntegration: 157 passed
- NetworkAcceleration: 63 passed + 2 skipped
- ViveTool: 234 passed
