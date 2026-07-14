# Task Plan — ShellIntegrationProfile Enum Sanitization Regression Test

## Goal
Add regression tests verifying that `ShellIntegrationProfile.Normalize()` sanitizes undefined enum values for `BackgroundEffect` (ShellVisualEffect) and `ColorScheme` (ShellColorScheme) to safe defaults, preventing corrupt JSON config from leaking garbage into the rendered .nss theme.

## Baseline
- HEAD `9fdc628` on master.
- `ShellIntegrationProfile.cs` already contains `SanitizeBackgroundEffect()` and `SanitizeColorScheme()` helpers, called from `Normalize()`. Fix is correct.
- No regression tests exist for these helpers.
- `ShellIntegrationProfileTests.cs` already has Normalize tests (clamping numeric fields). The new enum tests follow the same pattern.

## Root Cause
JSON deserializer preserves undefined enum integers (e.g., `(ShellVisualEffect)999`) as raw values. `Normalize()` previously passed these through unvalidated (`BackgroundEffect = BackgroundEffect`). The expression helpers (`GetEffectExpression`, `GetColorSchemeExpression`) have `_ =>` fallbacks, but the object model itself holds garbage — any direct reader gets an invalid enum.

## Scope
- `Plugins/ShellIntegration/ShellIntegrationProfile.cs` — already fixed (no new edits needed).
- `Plugins/ShellIntegration.Tests/ShellIntegrationProfileTests.cs` — add 4 test methods (2 Theory with InlineData each).
- `ai/task-plans/56-grok-execution-plan.md` — append evidence under `## Evidence`.

## Steps
1. Read `ShellIntegrationProfileTests.cs` to find insertion point.
2. Add 4 test methods:
   - `SanitizeBackgroundEffect_OutOfRangeValue_ReturnsAcrylic` — `(ShellVisualEffect)999` → Acrylic
   - `SanitizeBackgroundEffect_ValidValue_PreservesValue` — `ShellVisualEffect.Blur` → Blur
   - `SanitizeColorScheme_OutOfRangeValue_ReturnsAuto` — `(ShellColorScheme)999` → Auto
   - `SanitizeColorScheme_ValidValue_PreservesValue` — `ShellColorScheme.Dark` → Dark
3. Run focused tests: `dotnet test Plugins/ShellIntegration.Tests/ShellIntegration.Tests.csproj -c Release --filter "FullyQualifiedName~SanitizeBackgroundEffect|FullyQualifiedName~SanitizeColorScheme"`
4. Run canonical gate: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1`
5. Append evidence to `56-grok-execution-plan.md`.

## Verification
```bash
# Focused
dotnet test Plugins/ShellIntegration.Tests/ShellIntegration.Tests.csproj -c Release --nologo --filter "FullyQualifiedName~SanitizeBackgroundEffect|FullyQualifiedName~SanitizeColorScheme"

# Canonical
powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1
```

## Risks
- None — adding tests only. No production code changes this attempt.

## Stop Conditions
- All 4 new tests pass and canonical gate exits 0.
