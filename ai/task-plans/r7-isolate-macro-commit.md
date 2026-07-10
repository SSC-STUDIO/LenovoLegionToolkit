# Task Plan — Isolate Macro Test Commit (R7)

## Goal

Revert the oversized commit that bundled 120+ unrelated files and create a clean, focused commit containing only the macro test changes: Structs.cs, MacroTests.cs, MacroIdentifierJsonConverter.cs, .csproj fix, and the task plan.

## Baseline

- **Current HEAD**: `29d6135c` (fix: resolve file-lock race)
- **No macro commit exists** — all 153 files are unstaged/modified
- **Macro files in working tree**: Structs.cs (modified), MacroTests.cs (new), MacroIdentifierJsonConverter.cs (new), .csproj (modified), r6-macro-tests.md (new)
- **Build**: 0 errors, 93 pre-existing warnings
- **Tests**: 4371 passed, 0 failed, 30 skipped

## Scope

### In scope (this commit)
1. `UniversalDeviceToolkit.Lib.Macro/Structs.cs` — added `[JsonConverter(typeof(MacroIdentifierJsonConverter))]` attribute
2. `UniversalDeviceToolkit.Lib.Macro/Utils/TypeConverters/MacroIdentifierJsonConverter.cs` — new JsonConverter for dictionary key serialization
3. `UniversalDeviceToolkit.Tests/MacroTests.cs` — 22 test methods for MacroController
4. `UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj` — fixed doubled backslash in ProjectReference paths
5. `ai/task-plans/r6-macro-tests.md` — task plan with actual evidence (8495 bytes)

### Out of scope (left unstaged)
- All 120+ other modified/untracked files (network, WPF, plugins, localization, etc.)

## Steps

### Step 1: Create task plan (this file)
- Created `ai/task-plans/r7-isolate-macro-commit.md` with actual evidence.

### Step 2: Stage only macro-related files
```
git add UniversalDeviceToolkit.Lib.Macro/Structs.cs
git add UniversalDeviceToolkit.Lib.Macro/Utils/TypeConverters/MacroIdentifierJsonConverter.cs
git add UniversalDeviceToolkit.Tests/MacroTests.cs
git add UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj
git add ai/task-plans/r6-macro-tests.md
```

### Step 3: Verify staged files
```
git diff --cached --stat
```
Expected: exactly 5 files, all macro-related.

### Step 4: Commit
```
git commit -m "test: add MacroController tests and fix JSON serialization"
```

### Step 5: Verify commit
```
git log --oneline -1
git diff --stat HEAD~1
```
Expected: exactly 5 files in the commit.

### Step 6: Push
```
git push
```

## Verification

### Staged file check
```
Command: git diff --cached --name-only
Result: Should list exactly 5 macro-related files
```

### Commit verification
```
Command: git diff --stat HEAD~1
Result: Should show exactly 5 files changed
```

### Build verification (post-commit)
```
Command: dotnet build --no-restore -c Release -m:1
Result: exit 0, 0 errors
```

### Test verification (post-commit)
```
Command: dotnet test ... --filter "FullyQualifiedName~Macro" --nologo
Result: exit 0, 22 passed, 0 failed
```

## Risks

1. **JsonConverter format**: The new `"Source:Key"` format differs from old TypeConverter format. Risk is low because no valid serialized macro.json files exist (the production path previously crashed).
2. **120+ unstaged files remain**: These are preserved in the working tree and can be committed in future revisions.

## Stop Conditions

- [x] Task plan created with actual evidence
- [ ] Only macro-related files staged and committed
- [ ] 120+ other files remain unstaged
- [ ] Build passes post-commit
- [ ] Tests pass post-commit

## Evidence

### Files to commit (verified in working tree)
| File | Status | Lines | Description |
|------|--------|-------|-------------|
| `UniversalDeviceToolkit.Lib.Macro/Structs.cs` | modified | +2 | JsonConverter attribute |
| `UniversalDeviceToolkit.Lib.Macro/Utils/TypeConverters/MacroIdentifierJsonConverter.cs` | new | 57 | JSON dictionary key converter |
| `UniversalDeviceToolkit.Tests/MacroTests.cs` | new | 338 | 22 test methods |
| `UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj` | modified | +1 | Fixed ProjectReference paths |
| `ai/task-plans/r6-macro-tests.md` | new | 200+ | Task plan (8495 bytes) |

### Why these 5 files (not just 3)
The master listed 3 files but the build requires 5:
- `MacroIdentifierJsonConverter.cs` is referenced by `Structs.cs` — without it, the solution won't compile
- `.csproj` fix corrects doubled backslash in ProjectReference paths — without it, the test project can't resolve references
- All 5 files are directly macro-related and within the in-scope boundary

### Git state after commit
- HEAD: new commit with 5 macro files
- Working tree: 120+ files still unstaged (preserved for future revisions)
- Branch: master, ahead 1 from origin
