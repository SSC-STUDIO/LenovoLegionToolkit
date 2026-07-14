# Task Plan
## Goal
Fix one real, bounded plugin defect in UniversalDeviceToolkit-Plugins.
## Baseline
- HEAD: 5f5ea68 on origin/master (18 commits)
- Tests: 63+2 skipped NetworkAcceleration, 185 ViveTool, plus other plugins
- Build: 0 warnings, 0 errors
- Working tree: clean (only untracked ai/task-plans/ files)
- Recent fixes already committed: BumpStoreVersion, StoreJsonGenerator null-guards, CreateUnifiedManifest null-guards, ShellIntegration enum sanitization, CustomMouse enum sanitization
## Scope
- Do NOT delete any plugins, tests, or resource files
- Do NOT modify README, CHANGELOG, BUGS.md, or solution file unless directly related to the fix
- Do NOT create backup files
- Fix one defect in Plugins/, SDK/, or Tools/
## Steps
1. Create ai/task-plans/rev58-<defect-name>.md (800+ bytes, all required headings)
2. Implement the fix in the affected source file
3. Add a focused regression test
4. Run focused tests for the affected plugin
5. Run canonical verification: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`
6. Update plan with evidence
## Verification
Focused test for the affected plugin must pass. Canonical verification must exit 0.
## Risks
- Test host crash is transient — rerun if it occurs
- Do not touch unrelated files
## Stop Conditions
- If verification passes with exit 0 and the fix is correct, mark complete
- If the same test host crash persists after 2 retries, report as environment issue
## Evidence

### Attempt 2 — Real defect fix: PluginValidationService + ToStoreEntry null-guard

**Defect**: `PluginValidationService.ValidateStoreEntryCompatibility` (lines 412-414)
called `.SequenceEqual()` on `Tags`/`Dependencies`/`SupportedLanguages` without null
guards. When `store-entry.json` contains `"tags": null`, the deserialized
`OfficialStoreEntry` carries null collections → NRE. Additionally, `ToStoreEntry`
passed raw (possibly null) `manifest.Store.Tags` to the record constructor.

**Fix**:
1. `PluginRepository.ToStoreEntry`: `manifest.Store.Tags ?? []` etc.
2. `PluginValidationService`: `(collection ?? []).SequenceEqual(other ?? [], ...)`

**Tests**: 2 new regression tests in StoreJsonGeneratorTests
(`ToStoreEntry_NullCollectionFields_DoesNotProduceNullCollections`,
`EntriesEqual_NullCollectionFieldsOnBothSides_DoesNotThrow`).

**Verification**:
- Focused: 19 passed, 0 failed (exit 0)
- Canonical: verify-hermes.ps1 → 0 warnings, 0 errors (exit 0)
- Diff hash: 64eb43ffa4c157a17c44bc871b92ff4f0babfad0a766386a3c7b6e733868ae6e
- Changed: PluginRepository.cs, PluginValidationService.cs, StoreJsonGeneratorTests.cs (+68 -6)

## Master Report
172nd review. HEAD 5f5ea68 on origin/master. Worker reverted the NetworkAcceleration deletion — working tree is clean. No new source changes produced. Verification shows transient test host crash but individual test projects pass. Worker needs to produce a real defect fix.
