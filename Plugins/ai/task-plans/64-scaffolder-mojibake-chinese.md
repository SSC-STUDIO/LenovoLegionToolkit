# Task Plan

## Goal
Fix mojibake (garbled UTF-8) Chinese strings in `PluginScaffolder.cs` that produce broken zh-Hans `.resx` resources and `CHANGELOG.md` for every scaffolded plugin.

## Baseline
- HEAD: `f4b41b9` (revision 63, pushed)
- Branch: master, working tree clean except stale `62-grok-execution-plan.md`
- Defect location: `Tools/PluginTooling.Core/PluginScaffolder.cs` lines 147 and 279
- Mojibake strings:
  - Line 147: `璁剧疆` → `设置`, `鍔熻兘棰勮` → `功能预览`, `璁剧疆棰勮` → `设置预览`
  - Line 279: `鍒濆鎻掍欢楠ㄦ灦` → `初始插件骨架`
- Root cause: Source file was likely edited with wrong encoding, causing UTF-8 bytes of Chinese text to be decoded as GBK and re-encoded, producing double-encoded mojibake

## Scope
- `Tools/PluginTooling.Core/PluginScaffolder.cs` — fix 4 mojibake strings on lines 147 and 279
- `Tests/PluginTooling.Tests/PluginScaffolderTests.cs` — new regression test verifying Chinese strings are correct

## Steps
1. Fix line 147: replace `璁剧疆` with `设置`, `鍔熻兘棰勮` with `功能预览`, `璁剧疆棰勮` with `设置预览`
2. Fix line 279: replace `鍒濆鎻掍欢楠ㄦ灦` with `初始插件骨架`
3. Write regression test `PluginScaffolderTests.cs` that calls `BuildPluginChangelog` via reflection or invokes the scaffolder to verify Chinese text is correct (not mojibake)
4. Run focused verification: `dotnet test Tests/PluginTooling.Tests/ --filter "FullyQualifiedName~PluginScaffolderTests"`
5. Run canonical verification: `scripts/verify-hermes.ps1`

## Verification
- Focused: `dotnet test Tests/PluginTooling.Tests/ --nologo --filter "FullyQualifiedName~PluginScaffolderTests"` — expect 0 failed
- Canonical: `powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1` — expect 0 warnings, 0 errors

## Risks
- Very low: changing string literals from mojibake to correct Chinese is a pure data fix
- No behavioral change to scaffolder logic, templating, or project structure
- Risk of introducing new mojibake if file is read/written with wrong encoding — mitigated by using `patch` tool which preserves file encoding

## Stop Conditions
- Stop after fix + tests pass, and canonical verification is green
- Stop if the mojibake pattern is more widespread than 2 locations (audit first)

## Evidence
To be filled after verification.
