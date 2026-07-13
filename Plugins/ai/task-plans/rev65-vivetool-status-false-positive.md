# Task Plan

## Goal
Fix ViveToolFeatureService false-positive status matching where `Contains("enabled")` incorrectly matches "not enabled" output, returning Enabled instead of Unknown.

## Baseline
- HEAD: `2bdc335` on origin/master.
- `ViveToolFeatureService.GetFeatureStatusAsync` (line 177): `output.Contains("enabled")` — substring match false-positives on "not enabled" output.
- `ViveToolFeatureService.ParseStatusFromLine` (line 630): `line.Contains("Enabled", OrdinalIgnoreCase)` — same substring flaw.
- ViveTool.Tests: 231 pass, 0 fail, 0 skip (before fix).

## Scope
- `Plugins/ViveTool/Services/ViveToolFeatureService.cs` — Replace `Contains` with negative-lookbehind regex in both methods.
- `Plugins/ViveTool.Tests/ViveToolFeatureServiceTests.cs` — Add regression tests for "not enabled" edge cases.

## Steps
1. Replace `output.Contains("enabled")` with `Regex.IsMatch(output, @"(?<!not\s)\benabled\b")` in `GetFeatureStatusAsync`. Same for "disabled" and "default".
2. Replace `line.Contains("Enabled", ...)` with `Regex.IsMatch(line, @"(?<!\bnot\s)\benabled\b", IgnoreCase)` in `ParseStatusFromLine`. Same for "Disabled" and "Default".
3. Add 3 `[InlineData]` entries to `ParseStatusFromLine_RecognizesSupportedStates`: "Feature state is not Enabled", "not Disabled", "not Default" → all expected `Unknown`.
4. Run focused test, then full ViveTool suite, then canonical gate.

## Verification
- Focused: `dotnet test ViveTool.Tests.csproj --filter "ParseStatusFromLine_RecognizesSupportedStates"` → 7/7 pass
- Full ViveTool: `dotnet test ViveTool.Tests.csproj` → 234 pass, 0 fail
- Canonical: `scripts/verify-hermes.ps1` → 0 errors, 0 warnings

## Risks
- Regex `(?<!not\s)` won't match "notnot Enabled" (unrealistic vivetool output). Acceptable.
- No behavioral change for normal "Enabled"/"Disabled" output — regex still matches standalone words.

## Stop Conditions
Stop after one coherent increment if verification passes.

## Evidence
- **Defect**: `GetFeatureStatusAsync` line 177: `output.Contains("enabled")` returns `Enabled` for "not enabled". `ParseStatusFromLine` line 630: `line.Contains("Enabled", OrdinalIgnoreCase)` same flaw. Substring containment ≠ exact status word.
- **Fix**: Negative-lookbehind regex `(?<!not\s)\benabled\b` prevents "not enabled" from matching as Enabled. Applied to both methods. `GetFeatureStatusAsync` uses pre-lowered output; `ParseStatusFromLine` uses `RegexOptions.IgnoreCase`.
- **Regression test**: 3 new `[InlineData]` entries: "Feature state is not Enabled/Disabled/Default" → `Unknown`.
- **Focused**: `dotnet test --filter "ParseStatusFromLine_RecognizesSupportedStates"` → 7/7 pass, 82ms, exit 0.
- **Full ViveTool**: 234 pass (+3), 0 fail.
- **Canonical**: `scripts/verify-hermes.ps1` → 0 errors, 0 warnings. 552 pass + 2 skip.
- **Changed files**: `ViveToolFeatureService.cs` (+15/-11), `ViveToolFeatureServiceTests.cs` (+4/-0).
