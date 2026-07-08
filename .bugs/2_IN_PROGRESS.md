# Multi-Document Bug Queue - In Progress (Claimed & Locked)

> Repository: UniversalDeviceToolkit (Main Repo, .NET 10 WPF)
> Maintain an atomic claim lock: tag each ticket moved here from 1_NEW_REPORTS.md with [CLAIMED by <Agent-ID> at <Timestamp>]. On fix + verification, move to 3_RESOLVED.md; release the lock back to 1_NEW_REPORTS.md if abandoned.

---

### [CLAIMED by Codex-Agent-001 at 2026-07-09 14:30 CST] BUG-2026-07-09-001: Solution file has duplicate configuration entries and missing Any CPU mappings (MSB4121 warnings + redundant build targets)
- **Severity**: Medium
- **Component**: `UniversalDeviceToolkit.sln`
- **Symptom**: `dotnet build` emits 7x MSB4121 warnings for x64-only projects and runs duplicate MSBuild tasks (`c5935d12` repeated lines).
- **Root cause**: 7 x64-only project GUIDs (`DC01FDB3`,`4B902DDC`,`CB52B339`,`AC885CE1`,`656AC74B`,`2C7AB13C`,`BB54FD85`) are missing `Debug|Any CPU`/`Release|Any CPU` config mappings (solution declares those configs) AND have duplicate `Debug|x64`/`Release|x64` mapping line pairs.
- **Fix plan**: Remove duplicate x64 line pairs; add `Debug|Any CPU = Debug|x64` and `Release|Any CPU = Release|x64` for the 7 GUIDs.
- **Verification**: `dotnet build UniversalDeviceToolkit.sln -c Debug` → 0 MSB4121, no duplicate task lines; same for `-c Release`.
