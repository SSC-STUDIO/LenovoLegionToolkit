# Findings

## 2026-02-27 Plugin Translation Audit
- `progress.md` was not present in plugin repository root before this session.
- Resource scope found in plugin repository:
  - `Plugins/ViveTool/Resources/Resource.resx`
  - `Plugins/ViveTool/Resources/Resource.de.resx`
  - `Plugins/ViveTool/Resources/Resource.ja.resx`
  - `Plugins/ViveTool/Resources/Resource.ko.resx`
  - `Plugins/ViveTool/Resources/Resource.zh-hans.resx`
  - `Plugins/ViveTool/Resources/Resource.zh-hant.resx`
  - `Plugins/Template/Resources/Resource.resx`
- XML node-based structural audit result:
  - `locale_files=5`
  - `total_missing=38`
  - `total_extra=0`
  - `total_placeholder_mismatch=0`
  - `nonzero_files=5`
- Missing keys are concentrated in ViveTool locale files; this repository currently does not provide 20+ locale packs.
- This session kept plugin source/resources unchanged and focused completion work on the main repo 20+ locale task.

## 2026-02-27 CustomMouse Optimization Extension Alignment (Phase 4 complete)
- Converted `custom-mouse` capability shape to match host optimization-extension route:
  - `GetFeatureExtension()` removed from active entry flow (`null`).
  - `GetOptimizationCategory()` now provides plugin category `custom.mouse` with enable/disable actions for auto-theme cursor style mode.
- Added and persisted plugin setting `AutoThemeCursorStyle` for optimization action state evaluation.
- Updated plugin metadata consistency for release `1.0.5` across:
  - plugin attribute version,
  - csproj assembly/file/package versions,
  - `plugin.json` manifest,
  - `store.json` listing,
  - plugin changelog entry.
- Updated `CustomMouse.Tests` assertions to new capability contract and verified all tests pass (`20/20`).

## 2026-02-28 CustomMouse Legacy Cursor Theme Restore Findings (Phase 5 complete)
- Restored historical cursor asset tree from older plugin code lineage under:
  - `Plugins/CustomMouse/Resources/W11-CC-V2.2-HDPI`
- Confirmed classic animation assets now exist for both themes:
  - `Dark/Regular/02. classic/{Busy.ani,Working.ani,Install.inf}`
  - `Light/Regular/02. classic/{Busy.ani,Working.ani,Install.inf}`
- Current CustomMouse runtime behavior is aligned to user request:
  - Detect current Windows light/dark mode (`AppsUseLightTheme`)
  - Apply theme-matching cursor style via INF execution first
  - Fallback to direct HKCU cursor registry scheme apply when INF path fails
  - Backup and restore previous cursor scheme on disable/uninstall path
- Metadata/version alignment completed for this phase:
  - CustomMouse plugin attribute/csproj/plugin.json/store/changelog aligned to `1.0.6`.
- Verification for this phase:
  - `CustomMouse.Tests`: PASS (`21/21`)
  - plugin solution build: PASS (`LenovoLegionToolkit-Plugins.sln`, Release)
- Known environment note:
  - Initial direct `dotnet test` command timed out in this workspace; build + `--no-build` rerun passed successfully.

## 2026-02-28 Additional Plugin Test Completion (Network/Shell/Vive)
- To close the remaining plugin-test scope requested by user, executed dedicated plugin test projects after the CustomMouse 1.0.6 validation pass.
- Results:
  - `NetworkAcceleration.Tests`: PASS (`7/7`)
  - `ShellIntegration.Tests`: PASS (`5/5`)
  - `ViveTool.Tests`: PASS (`4/4`)
- Conclusion: official plugin test-project set is green in current workspace (`custom-mouse`, `network-acceleration`, `shell-integration`, `vive-tool`).
- Store metadata consistency aligned for this release batch:
  - `store.json` top-level `version` updated to `1.0.6`
  - `store.json` `lastUpdated` refreshed to `2026-02-28T08:00:00Z`
  - `custom-mouse` `releaseDate` refreshed to `2026-02-28T00:00:00Z`
  - JSON validation via `ConvertFrom-Json`: PASS

## 2026-02-28 Plugin UI Duplicate-Title Cleanup + Typography Unification (Phase 6 complete)
- Root cause confirmed: several plugin controls rendered internal large headers while host wrapper/settings window already rendered plugin titles, causing double-title appearance.
- Updated plugin controls to remove redundant in-control headings and keep only explanatory text + functional controls:
  - `Plugins/CustomMouse/CustomMouseControl.xaml`
  - `Plugins/CustomMouse/CustomMouseSettingsControl.xaml`
  - `Plugins/NetworkAcceleration/NetworkAccelerationControl.xaml`
  - `Plugins/NetworkAcceleration/NetworkAccelerationSettingsControl.xaml`
  - `Plugins/ShellIntegration/ShellIntegrationSettingsControl.xaml`
- Updated fallback UI builders (XAML load fallback paths) to match same rule and removed bold heading creation:
  - `CustomMouseControl.xaml.cs`
  - `CustomMouseSettingsControl.xaml.cs`
  - `NetworkAccelerationControl.xaml.cs`
  - `NetworkAccelerationSettingsControl.xaml.cs`
  - `ShellIntegrationSettingsControl.xaml.cs`
- Resulting behavior:
  - No in-page duplicate top titles inside plugin content areas.
  - No bold title text in these plugin control surfaces.
  - Host title remains the single source of truth for plugin page/settings headings.

## 2026-02-28 Plugin UI Visual Polish (ViveTool + Network) (Phase 7 complete)
- Refined Network Acceleration UI surfaces:
  - `NetworkAccelerationControl.xaml`: upgraded to clean card-based layout for quick actions + preferred mode.
  - `NetworkAccelerationSettingsControl.xaml`: grouped settings with card hierarchy and clearer save/status region.
- Refined ViVeTool settings UI:
  - `ViveToolSettingsPage.xaml`: simplified section grouping and reduced visual noise while retaining all core actions.
  - `ViveToolSettingsPage.xaml.cs`: upgraded fallback UI layout to match polished XAML structure.
- Synchronized fallback style improvements for network pages:
  - `NetworkAccelerationControl.xaml.cs`
  - `NetworkAccelerationSettingsControl.xaml.cs`
- Version/changelog/store consistency completed:
  - `network-acceleration`: `1.0.4`
  - `vive-tool`: `1.1.3`
  - store index version: `1.0.7`
- Additional consistency tweak:
  - `ViveToolPage.xaml.cs` fallback title weight reduced from `Bold` to `Medium`.

## 2026-02-28 Plugin Multi-Language Finalization + Local Action Verification (Phase 8 complete)
- Completed metadata-level localization for plugin cards/pages in:
  - `Plugins/CustomMouse/CustomMouseText.cs` + `CustomMousePlugin.cs`
  - `Plugins/NetworkAcceleration/NetworkAccelerationText.cs` + `NetworkAccelerationPlugin.cs`
  - `Plugins/ShellIntegration/ShellIntegrationText.cs` + `ShellIntegrationPlugin.cs`
- Result: `Name` and `Description` now follow `CurrentUICulture` (zh-hans / zh-hant explicit, non-zh fallback English), matching the app language behavior for these plugins.
- Removed remaining hardcoded fallback English text in ViveTool UI:
  - `ViveToolPage.xaml.cs` fallback search placeholder now uses `Resource.ViveTool_SearchPlaceholder`.
  - `ViveToolSettingsPage.xaml.cs` executable/config file dialog filter text now localized by current culture.
- Updated tests to avoid hardcoded-English metadata assertions:
  - `CustomMousePluginTests.cs`
  - `NetworkAccelerationPluginTests.cs`
  - `ShellIntegrationPluginTests.cs`
- Local machine action-level verification (non-mock, direct method execution) completed via temporary verifier app:
  - `CustomMouse.ApplyCursorStyleForCurrentThemeAsync` returned `True` and produced `theme=light`.
  - `CustomMouse.OnUninstalled` restoration flow executed.
  - `ShellIntegration.DisableShellAsync` returned `True`.
  - `ShellIntegration.EnableShellAsync` returned `True`.
  - Detected shell install path on this machine: `C:\Program Files\Nilesoft Shell\shell.dll`.

## 2026-02-28 ViveTool Bundled Runtime + Custom Path Override (Phase 9 complete)
- Added bundled runtime assets under:
  - `Plugins/ViveTool/Bundled/ViVeTool.exe`
  - `Plugins/ViveTool/Bundled/Albacore.ViVe.dll`
  - `Plugins/ViveTool/Bundled/Newtonsoft.Json.dll`
  - `Plugins/ViveTool/Bundled/FeatureDictionary.pfs`
- Updated ViveTool package output rules so bundled files are copied to plugin output:
  - `Plugins/ViveTool/LenovoLegionToolkit.Plugins.ViveTool.csproj`
- Updated path resolution behavior in `ViveToolService`:
  - Priority 1: user custom `ViveToolPath`
  - Priority 2: bundled plugin runtime (`<plugin>/Bundled/ViVeTool.exe`)
  - Priority 3+: existing fallback discovery (AppData/runtime download, PATH, current directory)
- Custom path behavior remains supported via settings UI; only default source changed to bundled runtime first.
- Updated settings copy text in resource files to clearly describe bundled default + custom override:
  - `Resource.resx`
  - `Resource.zh-hans.resx`
  - `Resource.zh-hant.resx`
- Added dedicated tests:
  - `ViveToolServicePathTests.GetViveToolPathAsync_UsesBundledRuntimeByDefault`
  - `ViveToolServicePathTests.GetViveToolPathAsync_PrefersUserSpecifiedPath`
- ViveTool version metadata synchronized to `1.1.4` in:
  - plugin attribute
  - csproj version fields
  - `Plugin.json`
  - `CHANGELOG.md`
  - `store.json` entry (`vive-tool`) and changelog link
