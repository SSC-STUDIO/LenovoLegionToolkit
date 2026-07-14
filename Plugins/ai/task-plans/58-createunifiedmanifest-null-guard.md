# Rev58 — Null-Guard Collection Properties in CreateUnifiedManifest

## Defect

`PluginRepository.CreateUnifiedManifest` (lines 274–276) accesses
`storeEntry?.Tags.ToList()`, `storeEntry?.Dependencies.ToList()`, and
`storeEntry?.SupportedLanguages.ToList()`. The `?.` only guards `storeEntry`
itself being null — it does NOT guard the collection properties.

`OfficialStoreEntry` is a positional record with `IReadOnlyList<string>` parameters.
When `store-entry.json` contains explicit `"tags": null`, `"dependencies": null`,
or `"supportedLanguages": null`, System.Text.Json sets those constructor
parameters to `null` (records don't apply property default initializers).
The `?.` short-circuit doesn't fire (storeEntry is non-null), so `.ToList()`
is called on `null` → `NullReferenceException`.

This crashes any CLI invocation that loads a plugin whose `store-entry.json`
has null collection fields (e.g. `hermes plugin build`, `generate-store`).

## Fix

Change the three expressions from:
```csharp
Tags = storeEntry?.Tags.ToList() ?? [],
Dependencies = storeEntry?.Dependencies.ToList() ?? [],
SupportedLanguages = storeEntry?.SupportedLanguages.ToList() ?? ["en"],
```
to:
```csharp
Tags = (storeEntry?.Tags ?? []).ToList(),
Dependencies = (storeEntry?.Dependencies ?? []).ToList(),
SupportedLanguages = (storeEntry?.SupportedLanguages ?? ["en"]).ToList(),
```

The `?? []` now applies to the collection property itself (not the result of
.ToList()), so null collections are coalesced before .ToList() is called.

## Test Plan

Add a regression test in `PluginTooling.Tests` that:
1. Creates an `OfficialStoreEntry` with null Tags/Dependencies/SupportedLanguages.
2. Calls `PluginRepository.CreateUnifiedManifest`.
3. Verifies no NRE and the resulting `PluginStoreMetadata` has the expected
   defaults (empty Tags, empty Dependencies, ["en"] SupportedLanguages).

## Evidence

- **Defect**: `PluginRepository.CreateUnifiedManifest` lines 274–276 used
  `storeEntry?.Tags.ToList() ?? []` — the `??` applies to the result of
  `.ToList()`, which throws NRE when `Tags` is null (storeEntry non-null).
- **Root cause**: `OfficialStoreEntry` is a positional record; JSON `"tags": null`
  sets the constructor parameter to `null` regardless of property defaults.
- **Fix**: `(storeEntry?.Tags ?? []).ToList()` — null-coalesce BEFORE `.ToList()`.
- **Test**: `CreateUnifiedManifestNullGuardTests` — 2 new tests (null collections,
  null storeEntry), both pass. Total: 17 passed, 0 failed.
- **Verify**: `verify-hermes.ps1` → 0 warnings, 0 errors, exit 0.
