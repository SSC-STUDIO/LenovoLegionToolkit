# Task Plan

## Goal
Fix a real crash defect in StoreJsonGenerator.Generate where duplicate plugin entry IDs in store.json cause ToDictionary to throw ArgumentException, then cover the fix with focused regression tests and pass canonical verification.

## Baseline
- HEAD: 27393b5 (docs(plan): expand revision-60 task plan with detailed defect analysis and evidence)
- Previous fix commit: 40560ab (fix(store): tolerate duplicate entry IDs in StoreJsonGenerator with last-wins dedup)
- Original HEAD before fix: 68618cc (fix(store): null-guard collection property access across tooling layer)
- Working tree: clean — fix and tests already committed and pushed to origin/master

## Scope
- Tools/PluginTooling.Core/StoreJsonGenerator.cs — replaced ToDictionary with GroupBy.Last() dedup, dedup MergeExisting AddRange path (already committed as 40560ab)
- Tests/PluginTooling.Tests/StoreJsonGeneratorTests.cs — added 2 regression tests for duplicate entry ID crash (already committed as 40560ab)
- ai/task-plans/60-grok-execution-plan.md — this plan file, updated with real defect analysis and evidence

## Steps
1. Identified defect: StoreJsonGenerator.Generate line 24 calls `repository.StoreDocument?.Plugins.ToDictionary(entry => entry.Id, entry => entry, StringComparer.OrdinalIgnoreCase)`. When store.json contains two entries with the same Id (manual edit, merge artifact, or corrupted file), ToDictionary throws ArgumentException("An item with the same key has already been added") and the entire generate/check pipeline crashes with no recovery path.
2. Root cause: No deduplication guard on StoreDocument.Plugins before ToDictionary or before AddRange into the output store. The MergeExisting path at line 42 (store.Plugins.AddRange(...)) also propagates duplicates into the output, and ReplaceOrAdd only replaces the first match — the second duplicate survives and ends up in the written store.json.
3. Fixed StoreJsonGenerator.cs line 24: replaced ToDictionary with GroupBy(entry => entry.Id, entry => entry, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase) for last-wins dedup (matching ReplaceOrAdd semantics).
4. Fixed StoreJsonGenerator.cs MergeExisting path: replaced AddRange(Select(Clone)) with GroupBy(entry => entry.Id, entry => entry, StringComparer.OrdinalIgnoreCase).Select(group => Clone(group.Last())) + ReplaceOrAdd to deduplicate before merge.
5. Added regression test Generate_MergeExisting_DuplicateStoreEntryIds_DeduplicatesLastWins: creates store.json with two entries sharing Id "dup-plugin" (different names/fileSizes), calls Generate with mergeExisting=true, asserts no exception, asserts single entry in output, asserts entry name matches manifest (not store.json duplicates).
6. Added regression test Generate_NoMergeExisting_DuplicateStoreEntryIds_DoesNotThrow: creates store.json with two entries sharing Id "dup-nomerge", calls Generate with mergeExisting=false, asserts no exception, asserts single entry in output.
7. Ran focused tests and canonical verification — all passed.
8. Committed as 40560ab and pushed to origin/master (68618cc..40560ab).

## Verification
Focused command: `dotnet test Tests/PluginTooling.Tests/PluginTooling.Tests.csproj -c Release --filter "FullyQualifiedName~StoreJsonGeneratorTests"`
Canonical command: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`

## Risks
- The fix changes ToDictionary to GroupBy+ToDictionary which is O(n) extra memory for the grouping — negligible for the small number of plugins in a typical store.json.
- Last-wins semantics: if a user intentionally has two different plugins with the same Id (which is itself a bug), only the last one survives. This is the correct behavior — duplicate Ids are invalid in store.json.
- No regression risk: the GroupBy approach produces the same result as ToDictionary when there are no duplicates.

## Stop Conditions
Stop after verification passes and the detailed plan gate is satisfied with all required headings and 800+ UTF-8 bytes of real content.

## Evidence

### Focused tests
Command: `dotnet test Tests/PluginTooling.Tests/PluginTooling.Tests.csproj -c Release --filter "FullyQualifiedName~StoreJsonGeneratorTests"`
Result: 20 passed, 0 failed, exit 0 (717 ms)
New tests: Generate_MergeExisting_DuplicateStoreEntryIds_DeduplicatesLastWins, Generate_NoMergeExisting_DuplicateStoreEntryIds_DoesNotThrow

### Canonical verification
Command: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`
Result: 0 warnings, 0 errors, exit 0
- Build: 0 warnings, 0 errors
- BatteryHealth.Tests: 71 passed, 0 failed
- CustomMouse.Tests: 52 passed, 0 failed
- ShellIntegration.Tests: 157 passed, 0 failed
- NetworkAcceleration.Tests: 63 passed, 2 skipped
- ViveTool.Tests: 234 passed, 0 failed

### Commit
- SHA: 40560ab (fix) + 27393b5 (plan expansion)
- Message: fix(store): tolerate duplicate entry IDs in StoreJsonGenerator with last-wins dedup
- Pushed: 68618cc..40560ab..27393b5 master -> master (origin/master)
- Files: 4 changed, +217 -5 (fix commit), 1 changed +59 -18 (plan commit)
- Working tree: clean post-push

### Changed files
- Tests/PluginTooling.Tests/StoreJsonGeneratorTests.cs (+141 lines: 2 new regression tests, committed in 40560ab)
- Tools/PluginTooling.Core/StoreJsonGenerator.cs (+15 -5: GroupBy Last() dedup in ToDictionary and MergeExisting paths, committed in 40560ab)
- ai/task-plans/60-grok-execution-plan.md (detailed plan with evidence, committed in 27393b5 and this update)
- ai/task-plans/60-duplicate-store-entry-crash.md (initial defect analysis, committed in 40560ab)
