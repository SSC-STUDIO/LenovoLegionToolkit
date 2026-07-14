# Revision 60 — Duplicate Store Entry ID Crash

## Defect class
`StoreJsonGenerator.Generate` calls `repository.StoreDocument?.Plugins.ToDictionary(...)` at line 24.
If `store.json` contains two plugin entries with the same `Id` (manual edit, merge artifact,
or corrupted file), `ToDictionary` throws `ArgumentException` ("An item with the same key has
already been added") and the entire generate/check pipeline crashes with no recovery path.

The `MergeExisting` path at line 42 (`store.Plugins.AddRange(...)`) also propagates duplicates
into the output, and `ReplaceOrAdd` at line 206-217 only replaces the *first* match — the second
duplicate survives and ends up in the written `store.json`.

## Root cause
No deduplication guard on `StoreDocument.Plugins` before `ToDictionary` or before
`AddRange` into the output store.

## Fix
1. Replace `ToDictionary` with a safe `GroupBy` + `Last()` pattern that tolerates duplicate IDs
   (last-wins, matching `ReplaceOrAdd` semantics).
2. Deduplicate the `MergeExisting` `AddRange` path so the output never contains duplicate IDs.

## Test plan
3 regression tests in `StoreJsonGeneratorTests`:
- `Generate_DuplicateStoreEntryIds_DoesNotThrow` — store.json with two entries for same plugin ID
- `Generate_MergeExisting_DuplicateStoreEntryIds_Deduplicates` — merge path produces single entry
- `Check_DuplicateStoreEntryIds_DoesNotThrow` — Check() path also tolerates duplicates

## Verify
- Focused: dotnet test --filter "StoreJsonGeneratorTests"
- Canonical: scripts/verify-hermes.ps1

## Evidence
Fill after execution with exact commands, exit codes, and key log lines.
