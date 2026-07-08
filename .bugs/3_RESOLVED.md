# Multi-Document Bug Queue - Resolved (Fixed & Pending Verification)

> Repository: UniversalDeviceToolkit (Main, .NET 10 / WPF)
> Fixed defects pending final verification. Attach Unit test / OCR check proof, then move to `4_ARCHIVED.md` and transcribe the root cause + preventative rule to `KNOWLEDGE_BASE.md`.

---

- [x] **[UDT-025]** [Reliability / ReflectionTypeLoadException] Static constructors in AutomationJsonConverters.cs used `Assembly.GetTypes()` without `ReflectionTypeLoadException` handling at lines 38, 65, 155, 191, 218, 264. If any type in the assembly fails to load (missing dependency), `GetTypes()` throws `ReflectionTypeLoadException`; since these are static constructors, the exception propagates as `TypeInitializationException`, making the entire converter (and all JSON serialization of automation pipelines) unusable. PluginLoader.cs:122 already demonstrates the correct SafeGetTypes pattern.
  - **Claimed**: [CLAIMED by Codex at 2026-07-09 05:24:34]
  - **Resolved**: [RESOLVED by Codex at 2026-07-09] — All 6 type-discovery sites in `AutomationJsonConverters.cs` (line 38, 65, 155, 191, 218, 264) verified to route through `SafeGetTypes()` (extension in `AssemblyTypeLoaderExtensions.cs`) instead of bare `Assembly.GetTypes()`. The extension catches `ReflectionTypeLoadException`, logs each loader exception via `Log.Instance.Warning`, and returns only the successfully-loaded types (`ex.Types` filtered for nulls); a general `Exception` catch returns `[]`. Static constructors therefore never throw `TypeInitializationException`.
  - **Verification**: `dotnet build UniversalDeviceToolkit.Lib.Automation.csproj -c Debug` => 0 warnings / 0 errors. Confirmed via `Select-String` that every site at the reported line numbers calls `.SafeGetTypes()`.
