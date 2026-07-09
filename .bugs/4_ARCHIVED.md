# Multi-Document Bug Queue - Archived (Closed & Knowledge Ingested)

> Repository: UniversalDeviceToolkit (Main Repo, .NET 10 WPF)
> Final closed state. The root cause and preventative rule for each entry here must be permanently transcribed into KNOWLEDGE_BASE.md.

---


### [ARCHIVED 2026-07-09 08:43 CST] BUG-2026-07-09-001: Solution file duplicate config entries + missing Any CPU mappings
- **Component**: UniversalDeviceToolkit.sln
- **Symptom**: 7x MSB4121 warnings, duplicate MSBuild task lines during dotnet build.
- **Root cause**: x64-only projects declared Debug|Any CPU/Release|Any CPU configs in the solution header but lacked matching project-config mappings; duplicate Debug|x64/Release|x64 pairs also present.
- **Fix**: Normalized config mappings: Debug|Any CPU = Debug|x64, Release|Any CPU = Release|x64 for x64-only projects; removed duplicate x64 line pairs.
- **Rule**: When adding an x64-only project (Platforms=x64) to a multi-config .sln, always map Any CPU -> x64 for both Debug and Release to avoid MSB4121 "project configuration not mapped" warnings.
- **Verification**: dotnet build -c Debug -m:1 --no-incremental → 0 warnings, 0 errors.
