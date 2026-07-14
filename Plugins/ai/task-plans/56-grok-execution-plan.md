# Task Plan

## Goal

Fix the `BumpStoreVersion` defect in `StoreJsonGenerator.cs` where a 2-part SemVer version string (e.g. `"1.0"`) causes `Version.TryParse` to produce `Build == -1`, so the patch increment `Build + 1` evaluates to `0` instead of `1`. The result is that `"1.0"` is "bumped" to `"1.0.0"` — the same logical version — rather than `"1.0.1"`.

## Baseline

- HEAD: `705b821` (Phase 3 ABI refactor)
- Tests: 577 passed, 2 skipped, 0 failed
- Build: 0 warnings, 0 errors (Release)
- Working tree: clean (only `ai/task-plans/56-grok-execution-plan.md` is modified)

## Scope

- Writable file: `Tools/PluginTooling.Core/StoreJsonGenerator.cs` (fix `BumpStoreVersion`)
- Test file: `Tests/PluginTooling.Tests/StoreJsonGeneratorTests.cs` (add regression test)
- Plan file: `ai/task-plans/56-grok-execution-plan.md` (this file)

## Steps

1. Update this plan with the defect description and evidence.
2. Fix `BumpStoreVersion` in `StoreJsonGenerator.cs` line 369-378: normalize `version.Build` to `Math.Max(version.Build, 0)` before incrementing so that 2-part versions like `"1.0"` are bumped to `"1.0.1"`.
3. Add a focused regression test `BumpStoreVersion_TwoPartVersion_BumpsPatch` in `StoreJsonGeneratorTests.cs` that asserts `"1.0"` → `"1.0.1"` and `"2.3"` → `"2.3.1"`.
4. Run focused test filter: `dotnet test --filter "FullyQualifiedName~StoreJsonGenerator"` and confirm pass.
5. Run canonical gate: `powershell -File scripts/verify-hermes.ps1` and confirm exit 0.
6. Update this plan with evidence.

## Verification

- Focused: `dotnet test --filter "FullyQualifiedName~StoreJsonGenerator"` — must pass with 0 failures.
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1` — must exit 0.

## Risks

- The fix is a one-line change to `Math.Max(version.Build, 0)` inside `BumpStoreVersion`.
- Risk of breaking existing 3-part version bump: `new Version(1, 0, 1)` → `Math.Max(1, 0) = 1` → `1 + 1 = 2` → `"1.0.2"`. Correct.
- Risk of 4-part version (e.g. `"1.0.1.2"`): `Version.TryParse` produces `Build = 1`, unaffected.

## Stop Conditions

- The regression test passes and canonical verification exits 0.
- If the fix causes any existing test to fail, stop and reassess.

## Evidence

- **Defect**: `BumpStoreVersion` in `StoreJsonGenerator.cs:369` used `version.Build + 1` directly. `Version.TryParse("1.0")` produces `Build == -1`, so the bump yielded `0` instead of `1`, mapping `"1.0"` → `"1.0.0"` (no actual bump).
- **Fix**: Added `Math.Max(version.Build, 0)` before incrementing. Now `"1.0"` → `"1.0.1"`, `"2.3"` → `"2.3.1"`, `"1.0.5"` → `"1.0.6"` (unchanged behavior for 3-part versions).
- **Test**: `Generate_MergeExisting_BumpsTwoPartStoreVersion` in `StoreJsonGeneratorTests.cs` — creates a `store.json` with `"storeVersion": "1.0"`, generates with `MergeExisting=true`, asserts `StoreVersion == "1.0.1"`.
- **Focused test**: `dotnet test --filter "FullyQualifiedName~StoreJsonGeneratorTests"` → 13 passed, 0 failed, 0 skipped.
- **Canonical verification**: `powershell -File scripts/verify-hermes.ps1` → exit 0. Build: 0 warnings, 0 errors.
