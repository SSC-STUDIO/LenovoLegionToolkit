# Task Plan

## Goal
Fix unvalidated `CursorThemeMode` enum cast in `CustomMousePlugin.LoadSettings()` that silently produces invalid enum values when configuration stores an out-of-range integer, causing wrong cursor theme behavior.

## Baseline
HEAD commit `3003e05`. Working tree clean except for the two CustomMouse files modified by this task. `CustomMousePlugin.LoadSettings()` at line 390 performs `(CursorThemeMode)Configuration.GetValue(...)` — an unchecked integer-to-enum cast. `CursorThemeMode` is a 4-value enum (Auto=0, Light=1, Dark=2, WindowsDefault=3). Any persisted value outside this range produces an undefined enum member that silently falls through switch statements to the default cursor theme.

## Scope
Only two product files and one plan file. No other plugins, tests, solution files, READMEs, or AI contracts are touched.

## Steps
1. Extract `internal static CursorThemeMode SanitizeCursorThemeMode(int raw)` on `CustomMousePlugin` that validates via `Enum.IsDefined` and falls back to `CursorThemeMode.Auto`.
2. Refactor `LoadSettings()` to delegate to the new helper instead of inline cast.
3. Add 7 regression test cases via two `[Theory]` methods:
   - `SanitizeCursorThemeMode_WithOutOfRangeInt_FallsBackToAuto` — tests 999, -1, 100 all return Auto
   - `SanitizeCursorThemeMode_WithValidInt_PreservesValue` — tests 0→Auto, 1→Light, 2→Dark, 3→WindowsDefault
4. Run focused test: `dotnet test Plugins/CustomMouse.Tests/CustomMouse.Tests.csproj -c Release --nologo`
5. Run canonical verification: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`

## Verification
```
# Focused
dotnet test Plugins/CustomMouse.Tests/CustomMouse.Tests.csproj -c Release --nologo

# Canonical
powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1
```
Both must exit 0. All solution tests must pass with no new failures.

## Risks
- Low: helper is `internal static` — no behavioral change for valid enum values.
- Low: tests are pure unit tests calling the helper directly — no `Configuration` dependency, no side effects.

## Stop Conditions
Stop after successful commit and push of the 3-file diff. All verification gates must pass.

## Evidence

### Commit
`5c4cea8` — `fix(custommouse): sanitize corrupt CursorThemeMode enum with IsDefined fallback`
Committed and pushed to origin/master. 3 files changed, 105 insertions, 1 deletion.

### Diff (3 files)
1. `Plugins/CustomMouse/CustomMousePlugin.cs` — +12/-1
   - Added `internal static CursorThemeMode SanitizeCursorThemeMode(int raw)` (line 383-388)
   - `LoadSettings()` now calls `SanitizeCursorThemeMode(rawThemeMode)` instead of inline `(CursorThemeMode)` cast
2. `Plugins/CustomMouse.Tests/CustomMousePluginTests.cs` — +25/-0
   - Removed 3 unused `using` statements (Lib.Utils, Lib.Optimization, Lib.Plugins)
   - Added `[Theory] SanitizeCursorThemeMode_WithOutOfRangeInt_FallsBackToAuto` — 3 cases: 999, -1, 100
   - Added `[Theory] SanitizeCursorThemeMode_WithValidInt_PreservesValue` — 4 cases: 0→Auto, 1→Light, 2→Dark, 3→WindowsDefault
3. `ai/task-plans/rev66-custommouse-corrupt-enum-fallback.md` — this plan file

### Defect
`CustomMousePlugin.LoadSettings()` line 390: `(CursorThemeMode)Configuration.GetValue(...)` performs unchecked int→enum cast. Corrupted config values (e.g. 999, -1) produce undefined `CursorThemeMode` members that silently apply wrong cursor theme via switch fallthrough to default.

### Fix
- `internal static CursorThemeMode SanitizeCursorThemeMode(int raw)` — validates via `Enum.IsDefined`, falls back to `CursorThemeMode.Auto`
- `LoadSettings()` delegates to helper instead of inline cast
- Tests call helper directly (no `Configuration` dependency required)

### Regression Tests (7 test cases via 2 Theory methods)
- 3 out-of-range values (999, -1, 100) → `Auto`
- 4 valid values (0→Auto, 1→Light, 2→Dark, 3→WindowsDefault) → preserved

### Focused Verification (post-commit)
```
dotnet test Plugins/CustomMouse.Tests/CustomMouse.Tests.csproj -c Release --nologo
```
Result: 45 passed, 0 failed, 0 skipped. Exit code: 0.

### Canonical Verification (post-commit)
```
powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1
```
Result:
- CustomMouse.Tests: 45 passed, 0 failed
- BatteryHealth.Tests: 71 passed, 0 failed
- ShellIntegration.Tests: 146 passed, 0 failed
- NetworkAcceleration.Tests: 63 passed, 0 failed, 2 skipped (pre-existing)
- ViveTool.Tests: 234 passed, 0 failed
- Build: 0 warnings, 0 errors
- Total: 559 passed, 0 failed, 2 skipped
- Exit code: 0
