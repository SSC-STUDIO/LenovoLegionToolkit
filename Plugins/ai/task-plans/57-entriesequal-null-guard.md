# Task Plan

## Goal

Fix null-dereference crash in `StoreJsonGenerator.EntriesEqual` when an existing `store.json` contains explicit `"null"` values for collection fields (`localizedNames`, `localizedDescriptions`, `localizedTags`, `supportedLanguages`, `dependencies`, `tags`). `System.Text.Json` deserializes `"field": null` to `null` even when the C# property has a non-null default, causing `NullReferenceException` in `StringDictionariesEqual`, `TagDictionariesEqual`, and `SequenceEqual` calls within `EntriesEqual`.

## Baseline

- HEAD: `90c9001` on `master`, clean tree.
- Previous fix: rev56 `BumpStoreVersion` 2-part SemVer.
- Tests: 577+ pass, 0 failures. Build: 0 warnings, 0 errors.

## Scope

- `Tools/PluginTooling.Core/StoreJsonGenerator.cs` — null-guard `StringDictionariesEqual`, `TagDictionariesEqual`, and `EntriesEqual` sequence comparisons.
- `Tests/PluginTooling.Tests/StoreJsonGeneratorTests.cs` — regression test.
- `ai/task-plans/57-entriesequal-null-guard.md` — this plan.

## Steps

1. Add null-coalescing fallback in `StringDictionariesEqual`: treat null as empty dictionary.
2. Same for `TagDictionariesEqual`.
3. In `EntriesEqual`, guard `SequenceEqual` calls for `SupportedLanguages`, `Dependencies`, `Tags` — use `NullOrEmptyEqual` helper or inline null check.
4. Add regression test: `Generate_MergeExisting_HandlesNullCollectionFieldsInExistingStore` — creates `store.json` with explicit nulls, verifies no crash and correct merge.

## Verification

- Focused: `dotnet test -c Release --filter "FullyQualifiedName~StoreJsonGeneratorTests"` (expect 14 pass).
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1` (expect exit 0).

## Risks

- Low risk: null-coalescing is additive, doesn't change behavior for non-null inputs.
- Merge semantics for null-vs-empty are equivalent (both mean "no localized values").

## Stop Conditions

- If the fix introduces a test failure, revert and report.
- If the null scenario is unreachable via JSON deserialization (unlikely), report as non-defect.

## Evidence

- **Defect**: `StoreJsonGenerator` methods `Clone`, `CloneStringDictionary`, `CloneTagDictionary`, `StringDictionariesEqual`, `TagDictionariesEqual`, and `EntriesEqual` all dereferenced collection properties (`LocalizedNames`, `LocalizedDescriptions`, `LocalizedTags`, `SupportedLanguages`, `Dependencies`, `Tags`) without null guards. When `System.Text.Json` deserializes a `store.json` containing `"field": null`, it sets the property to `null` even though the C# default is `[]`. This causes `NullReferenceException` during `MergeExisting` generation.
- **Fix**: Added `?? []` null-coalescing to all 6 affected methods. Changed parameter types to nullable where needed.
- **Focused test**: 14 passed, 0 failed, 0 skipped (`StoreJsonGeneratorTests`).
- **Canonical**: `verify-hermes.ps1` exit 0, 0 warnings, 0 errors.
- **Diff hash**: `c83ea05c...` ≠ forbidden `e3b0c44...`.
