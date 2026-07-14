# Task Plan

## Goal

Fix NullReferenceException in `PluginValidationService.ValidateStoreEntryCompatibility` when
`store-entry.json` or `plugin.manifest.json` contains explicit `null` for collection fields
(`tags`, `dependencies`, `supportedLanguages`).

## Baseline

- HEAD: `5f5ea68` on `origin/master`
- Working tree: clean (only untracked `ai/task-plans/58-grok-execution-plan.md`)
- Recent fixes: BumpStoreVersion SemVer, StoreJsonGenerator null-guards, CreateUnifiedManifest null-guards
- Build: 0 warnings, 0 errors

## Scope

- `Tools/PluginTooling.Core/PluginValidationService.cs` — null-guard `.SequenceEqual()` calls
- `Tools/PluginTooling.Core/PluginRepository.cs` — null-coalesce in `ToStoreEntry`
- `Tests/PluginTooling.Tests/StoreJsonGeneratorTests.cs` — add regression test

## Steps

1. Fix `ToStoreEntry` in `PluginRepository.cs` to null-coalesce collection fields: `manifest.Store.Tags ?? []`, etc.
2. Fix `ValidateStoreEntryCompatibility` in `PluginValidationService.cs` lines 412-414 to use `(collection ?? []).SequenceEqual(other ?? [], ...)` pattern.
3. Add focused regression test that creates an `OfficialStoreEntry` with null collections and verifies no NRE.
4. Run focused tests: `dotnet test -c Release --filter "StoreJsonGeneratorTests"`
5. Run canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`
6. Update this plan with evidence.

## Verification

Focused: `dotnet test -c Release --filter "StoreJsonGeneratorTests"` must pass.
Canonical: `verify-hermes.ps1` must exit 0.

## Risks

- Low risk: null-coalescing is safe and backward-compatible.
- `ToStoreEntry` change ensures `OfficialStoreEntry` record never has null collections.

## Stop Conditions

- Stop after verification passes with exit 0.
- If test host crash persists after 2 retries, report as environment issue.

## Evidence

### Defect
`PluginValidationService.ValidateStoreEntryCompatibility` (lines 412-414) called
`.SequenceEqual()` directly on `Tags`/`Dependencies`/`SupportedLanguages` without
null guards. When `store-entry.json` or `plugin.manifest.json` contains explicit
`"tags": null`, the deserialized `OfficialStoreEntry` carries null collections,
and `.SequenceEqual(null)` throws `NullReferenceException`.

Additionally, `PluginRepository.ToStoreEntry` passed `manifest.Store.Tags` (which
can be null from JSON deserialization) directly to the `OfficialStoreEntry` record
constructor without null-coalescing.

### Fix
1. `PluginRepository.ToStoreEntry`: null-coalesce collection fields (`?? []`)
2. `PluginValidationService.ValidateStoreEntryCompatibility`: use
   `(collection ?? []).SequenceEqual(other ?? [], ...)` pattern

### Verification
- Focused: `dotnet test -c Release --filter "FullyQualifiedName~StoreJsonGeneratorTests|FullyQualifiedName~CreateUnifiedManifestNullGuardTests"`
  → 19 passed, 0 failed, 0 skipped (exit 0)
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`
  → 0 warnings, 0 errors (exit 0)
- Diff hash: `64eb43ffa4c157a17c44bc871b92ff4f0babfad0a766386a3c7b6e733868ae6e`
- Changed files: 3 (PluginRepository.cs, PluginValidationService.cs, StoreJsonGeneratorTests.cs)
- Lines: +68 -6
