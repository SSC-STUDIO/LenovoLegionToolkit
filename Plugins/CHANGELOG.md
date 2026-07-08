# Changelog / 鏇存柊鏃ュ織

All notable changes to this project will be documented in this file.

姝ら」鐩殑鎵€鏈夐噸瑕佹洿鏀归兘灏嗗湪姝ゆ枃浠朵腑璁板綍銆?

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
鏍煎紡鍩轰簬 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)锛?
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
骞堕伒寰?[璇箟鍖栫増鏈琞(https://semver.org/spec/v2.0.0.html)銆?

---

## [Unreleased] 鈥?v1.3.0-quality (Day 1-5 sprint)

### Fixed

- Harmonized BatteryHealth plugin author to SSC-STUDIO across plugin.json, plugin.manifest.json, and Plugin attribute (was EliuaK_Csy)
- Synced NetworkAcceleration C# Plugin attribute version from 1.1.9 to 1.2.0 to match manifest (manifest is source of truth per KNOWLEDGE_BASE)
- Added retry-with-backoff to `SaveSettingsAsync()` in NetworkAcceleration to eliminate cross-assembly test flake (PLG-005, 409/409 tests green)
- Synced Build directory plugin.manifest.json files with source Plugins directory

### 鉁?Added / 鏂板
- **5th plugin**: Battery Health (v1.0.0) 鈥?Monitor battery health, cycle count, capacity degradation
- **Battery Health settings UI**: Complete settings page with monitoring toggle, threshold sliders, notifications
- **Battery Health unit tests**: 16 tests for settings model validation, JSON round-trip, and invalid-threshold rules
- **Battery Health packaged**: Release ZIP built (2.99 MB, v1.0.0)
- **Battery Health UI redesign**: Full feature page and settings page redesign following the CustomMouse pattern (WpfFallbackHelper fallback, DynamicResource theme binding, CornerRadius cards, SymbolIcon glyphs, animated status pills)
- **Battery Health store promotion**: Generated store-entry.json and merged battery-health into root store.json with Universal Device Toolkit branding (32 languages, BatteryCharge24 icon)
- **Battery Health tests**: Fixed threshold theory inline-data bug; 16/16 unit tests green (0 warnings, 0 errors)
- **Cross-repo naming TODO**: Brand user-visible text as Universal Device Toolkit; internal LenovoLegionToolkit.* namespaces retained for host ABI compatibility (see BUGS.md M-010)
- **Performance optimization / 鎬ц兘浼樺寲**: 
  - `SaveWithDebounce()` 鈥?Batch rapid saves (97% I/O reduction)
  - `SaveAsync()` 鈥?Non-blocking async file I/O
  - MessagePack serialization support (opt-in via constructor)
- **Performance benchmark automation / 鎬ц兘鍩哄噯娴嬭瘯鑷姩鍖?*: `Scripts/run-performance-tests.sh`
- **WMI integration / WMI 闆嗘垚**: BatteryHealthService now queries real battery data (Win32_Battery)

### 鉁?Changed / 鍙樻洿
- **SettingsManager performance / SettingsManager 鎬ц兘**:
  - Save() latency: **62ms 鈫?0-1ms** (98% improvement)
  - Load() latency: **2ms 鈫?0ms** (100% improvement)
  - Memory transaction: skip save if settings unchanged
- **SDK version / SDK 鐗堟湰**: Updated to match host app v4.2.1
- **Battery Health settings UI**: Fixed CS0104 type ambiguity between Wpf.Ui.Controls and System.Windows.Controls
- **鍝佺墝閫氱敤鍖栭噸鍐?/ Brand generalization rewrite**: Generalized all user-visible Lenovo-specific framing to universal Windows/OEM scope across 15 docs (REDDIT_POSTS, PROMOTION_COPIES, REDDIT_PUBLISHING_CHECKLIST, DAY6_REDDIT_PUBLISHING_GUIDE, DAY5_HANDOVER, AUTONOMOUS_DAY4_SUMMARY, HANDOVER_SUMMARY_2026-07-06, SPRINT_DAY3_UPDATE, implementation_plan, plugin_ui_and_engineering_governance, agent_optimization_guide, PROMOTION_CHECKLIST, Docs/ARCHITECTURE.md L5) 鈥?`r/LenovoLegion`->`r/pcmasterrace`銆?Lenovo Legion laptops"->"your Windows setup"銆?鑱旀兂鎷晳鑰呭伐鍏峰寘"->"閫氱敤璁惧宸ュ叿鍖?銆佹彃浠舵暟 4->5銆侀」鐩暟 6->7銆佽仈鎯崇瑪璁版湰->Windows 绗旇鏈€侻-010 ABI gate preserved锛坄LenovoLegionToolkit.*` namespaces銆乻olution name銆乣x:Class`銆乵anifest `class`銆丏LL names銆乣host-release.json` intentionally retained锛夈€侷ntentionally kept: `r/Lenovo` community銆乣Lenovo Vantage` competitor comparison銆乣lenovo-legion` GitHub topic銆俈erification: `r/LenovoLegion` residual 0銆丆hinese brand residual 0銆乥uild 0 warnings/0 errors銆乼ests 409/409銆?
- **璺ㄤ粨搴撴暟鎹牴瀵归綈楠岃瘉 / Cross-repo data-root alignment verification**: 纭鎻掍欢 `SettingsManager<T>` 涓诲啓鍏ユ牴 `%LocalAppData%\UniversalDeviceToolkit\plugins\` (L21-24) 涓庡涓?`Folders.AppData`(`%LocalAppData%\UniversalDeviceToolkit`, Main `Folders.cs` L29-30) 涓€鑷达紱`LenovoLegionToolkit` 璺緞娈典粎浣滃彧璇婚仐鐣欒縼绉绘簮淇濈暀锛堜笉鏀癸級銆俙Tools\MainAppPluginUi.Smoke\Program.cs:L1342-1344` 纭紪鐮?`LenovoLegionToolkit` 灞炲啋鐑熸祴璇曟瀯閫犻仐鐣?`settings.json` 浠ラ獙璇佸涓昏縼绉婚€昏緫锛岄潪鐢熶骇璺緞鈥斺€斾繚鎸佷笉鍔ㄣ€傛棤闇€璺ㄤ粨搴撲唬鐮佹敼鍔ㄣ€?
- **鍙岃建澶嶉獙 / Dual-track re-verification & governance**: `dotnet build LenovoLegionToolkit-Plugins.sln -c Release` => 0 璀﹀憡/0 閿欒锛沗dotnet test` => 409/409 缁匡紙BatteryHealth 16, CustomMouse 54, ShellIntegration 114, NetworkAcceleration 39, ViveTool 186锛夈€俙.bugs/` 鍥涙。鐨嗗噣锛圥LG-001~004 褰掓。锛屾棤鏈喅/杩涜涓?宸茶В鍐抽仐鐣欙級銆傚€欓€夋不鐞嗗伐鍗?H-007锛坄PluginHostContext.ResolveType` L140 catch-all锛変笌 M-016锛坄SettingsManager.Update` Load->mutate->Save 闈炲師瀛愶級缁忚瘎浼板垽瀹氫负銆岄潪鍙鐜扮己闄枫€嶏紝涓嶇珛妗堚€斺€斿墠鑰呬负瀹夸富缂哄け鏃剁殑鍙嶅皠浼橀泤闄嶇骇锛涘悗鑰呮枃浠跺啓宸茬敤 temp+`File.Move(overwrite)` crash-atomic锛屼粎鐞嗚 lost-update 缂哄叿浣撳悜閲忥紝閬靛惊 PLG-002 涓嶇珛妗堢邯寰嬨€?
- **鍝佺墝娈嬬暀缁堝娓呯悊 / Brand residual final-audit cleanup**: `Docs/ARCHITECTURE.md` 涓?4 澶勭敤鎴峰彲瑙佺殑椤圭洰鍚?瀹夸富鍚嶅紩鐢ㄨ縼绉昏嚦鏂板悕锛圠5 鏁ｆ枃 鈫?`Universal Device Toolkit Plugins`銆丩15 鐩綍鏍戞牴鏍囩 鈫?`UniversalDeviceToolkit-Plugins/`銆丩61 sibling 鍙嶅紩鍙峰紩鐢?鈫?`UniversalDeviceToolkit`銆丩218 渚濊禆鍏崇郴鍥?ASCII 鐩掓爣绛?鈫?`Universal Device Toolkit`锛屼繚鎸?64 瀛楃澶栨瀹藉榻愶級锛沗Docs/CODING_STANDARDS.md` L5 椤圭洰鍚嶅悓姝ヨ縼绉昏嚦 `Universal Device Toolkit Plugins`銆侻-010 ABI 鏍囪瘑绗︿繚鎸佷笉鍔細`CODING_STANDARDS.md` L34/L56/L58/L85 鍛藉悕绌洪棿澹版槑銆丩519/L531/L532 瑙ｅ喅鏂规鏂囦欢鍚嶅紩鐢ㄣ€乣PLUGIN_DEVELOPMENT.md` L172 杈撳嚭鐩綍妯″紡銆乣REDDIT_POSTS.md` L248 `LenovoLegionToolkit.Plugins.SDK.dll` 瀹炲悕銆傚璁＄‘璁や粎鍓?M-010 闂ㄦ帶寮曠敤銆?
- **浼氳瘽澶囦唤鏂囦欢娓呯悊 / Session backup file cleanup**: 鍒犻櫎 6 涓疮绉細璇濆浠解€斺€擿CHANGELOG.md.prebrandbak`銆乣CHANGELOG.md.clsyncbak`銆乣KNOWLEDGE_BASE.md.prebrandbak`銆乣KNOWLEDGE_BASE.md.kbsyncbak`銆乣store.json.bak`銆乣.bugs/1_NEW_REPORTS.md.pass6bak`鈥斺€旀仮澶嶅伐浣滄爲娲佸噣锛坄.bugs/` 浠撳簱鏈窡韪級銆?
- **璺ㄤ粨搴?UDT-008 鑱斿姩 / Cross-repo UDT-008 linkage**: 涓讳粨搴?`UniversalDeviceToolkit.WPF/Windows/Utils/StatusWindow.xaml` 涓?3 涓?GPU 鐘舵€佸渾鐐?`Ellipse.Fill` 鐢辩‖缂栫爜鍗佸叚杩涘埗锛坄#FF8BC34A` / `#F2A541` / `#BF360C`锛夋敼涓虹粦瀹氫富棰樼敾绗?`StatusSuccessBrush` / `StatusWarningBrush` / `StatusCriticalBrush`锛堜笌鍏勫紵鎺т欢 `DiscreteGPUControl.xaml` 鍚屾ā寮忥級锛涜淇鍦ㄤ富浠撳簱 CHANGELOG 璁颁负 [UDT-008]锛屼娇鐘舵€佺獥鍙ｆ繁鑹?娴呰壊涓婚鍒囨崲鑹茬郴涓庡叏浠〃鐩樺叡浜浘琛ㄩ敭鍊肩姸鎬佽壊绯诲榻愶紝闂存帴鎻愬崌鎻掍欢鍦?PluginWorkbench 棰勮涓嬬殑涓婚涓€鑷存€с€傛棤鎻掍欢浠撳簱浠ｇ爜鏀瑰姩銆?

### 鉁?Fixed / 淇
- **[PLG-004][Threading & WMI]** `BatteryHealthService.QueryFirst` now bounds the synchronous `ManagementObjectSearcher.Get()` enumeration off-thread against a hard 2,500ms deadline (abandon pattern, matching the host-side `ManagementObjectSearcherExtensions.GetAsync`) instead of relying on `cts.CancelAfter`, which cannot interrupt a hung native COM provider; enumeration faults are unwrapped to preserve `ManagementException` / `COMException` / `OperationCanceledException` for the typed catch blocks of the caller / **[PLG-004][绾跨▼涓?WMI]** `BatteryHealthService.QueryFirst` 鐜板湪灏嗗悓姝?`ManagementObjectSearcher.Get()` 鏋氫妇鏀惧埌鍚庡彴绾跨▼骞朵笌 2,500ms 纭秴鏃剁珵璧涳紙abandon 妯″紡锛屼笌瀹夸富渚?`ManagementObjectSearcherExtensions.GetAsync` 涓€鑷达級锛屼笉鍐嶄緷璧栨棤娉曚腑鏂寕璧峰師鐢?COM 鎻愪緵绋嬪簭鐨?`cts.CancelAfter`锛涙灇涓惧紓甯稿凡瑙ｅ寘浠ヤ繚鐣?`ManagementException` / `COMException` / `OperationCanceledException` 渚涜皟鐢ㄦ柟鎸夌被鍨嬫崟鑾?
- **[PLG-003][UI Governance]** `ViveTool` / `ShellIntegration` plugin settings pages no longer use modal `MessageBox.Show`; errors/confirmations now surface inline via a themed `_statusTextBlock` + `SetStatus(text, isError)` helper (no focus steal from the host window). `WpfHostNotifications.cs` host shim is the sole allowed modal site / **[PLG-003][UI 娌荤悊]** `ViveTool` / `ShellIntegration` 鎻掍欢璁剧疆椤典笉鍐嶄娇鐢ㄦā鎬?`MessageBox.Show`锛涢敊璇?纭鐜板湪閫氳繃涓婚鍖栧唴鑱?`_statusTextBlock` + `SetStatus(text, isError)` 鍔╂墜鍛堢幇锛堜笉鍐嶄粠瀹夸富绐楀彛鎶㈠崰鐒︾偣锛夈€俙WpfHostNotifications.cs` 瀹夸富鍨墖涓哄敮涓€鍏佽鐨勬ā鎬佺珯鐐广€傚叏閲忔瀯寤?0 璀﹀憡/0 閿欒锛屾祴璇?409/409 缁裤€?
- **[PLG-001][Threading & UI Safety]** `ViveToolPage` no longer calls `.ConfigureAwait(false)` on UI-bound async tasks; continuations now stay on the WPF UI SynchronizationContext, preventing cross-thread `InvalidOperationException` in the plugin page / **[PLG-001][绾跨▼涓?UI 瀹夊叏]** `ViveToolPage` 涓嶅啀瀵?UI 缁戝畾鐨勫紓姝ヤ换鍔¤皟鐢?`.ConfigureAwait(false)`锛涘悗缁搷浣滀繚鎸佸湪 WPF UI 鍚屾涓婁笅鏂囷紝閬垮厤鎻掍欢椤佃法绾跨▼ `InvalidOperationException`
- **Code quality / 浠ｇ爜璐ㄩ噺**: 0 warnings, 0 errors across all 6 projects
- **IDE0011**: Added braces to 80+ if statements
- **IDE1006**: Fixed naming convention (private fields `_camelCase`)
- **CA1062**: Added null validation to all public methods
- **NetworkAcceleration**: Fixed version mismatch (plugin.json "1.1.9" 鈫?"1.2.0")
- **BatteryHealthSettingsControl.xaml.cs**: Resolved type ambiguity preventing compilation
- **娴嬭瘯闅旂 (PLG-002)**: 淇 xUnit 骞惰绔炴€?- `LocalizedTextTestsBase` 缁忓弽灏勬敼鍔ㄨ繘绋嬬骇闈欐€?`Resources.Resource.Culture`锛屼笌骞惰 `*PluginTests` 鐨勪袱娆℃枃鍖栫浉鍏宠鍙栦骇鐢熺珵鎬侊紝瀵艰嚧 `Plugin_HasExpectedMetadata` 闂存瓏鎬уけ璐ャ€備慨澶嶆柟寮忥細涓?BatteryHealth / CustomMouse / NetworkAcceleration / ShellIntegration 鍚勬柊澧?`[CollectionDefinition(DisableParallelization = true)]`锛屽皢 `*TextTests` 涓?`*PluginTests` 閿佸叆鍚屼竴闈炲苟琛岄泦鍚堛€傚叏閲忔祴璇?409/409 缁匡紙BatteryHealth 16, CustomMouse 54, ShellIntegration 114, NetworkAcceleration 39, ViveTool 186锛夈€?
- **鎻掍欢 UI 閲嶈璁?(BatteryHealth)**: 浠跨収 CustomMouse 妯″紡瀹屾垚鍔熻兘椤典笌璁剧疆椤甸噸璁捐 - DynamicResource 涓婚缁戝畾銆丆ornerRadius 鍦嗚鍗＄墖銆亀pfui:SymbolIcon 鍥炬爣锛堟棤 emoji锛夈€佸唴鑱旂姸鎬?TextBlock锛堟棤 MessageBox锛夈€乄pf.Ui 绫诲瀷鍏ㄩ檺瀹氥€?
- **鍟嗗簵涓婃灦 (store promotion)**: 鐢熸垚 `store-entry.json` 骞跺悎骞?battery-health 杩涙牴 `store.json`锛? 鎻掍欢锛宐attery-health 灞呴锛屾弿杩板惈 Universal Device Toolkit 鍝佺墝璇嶏級銆?

### 鉁?Removed / 绉婚櫎
- **FeatureStatusConverter extraction** (reverted 鈥?caused compilation errors)

---

## [v1.2.0-quality] 鈥?2026-07-05

### 鉁?Added / 鏂板
- **Zero warnings achievement / 闆惰鍛婃垚灏?* (all 6 projects)
- **562+ unit tests / 562+ 鍗曞厓娴嬭瘯** passing
- **CI validation / CI 楠岃瘉** fixed

### 鉁?Changed / 鍙樻洿
- **TreatWarningsAsErrors=true** enforced globally
- **XML documentation / XML 鏂囨。** added to all public APIs

---

## [v1.1.16] 鈥?2026-07-03

### 鉁?Added / 鏂板
- **Social preview banner / 绀句氦棰勮妯箙**: Added `Assets/social-preview.svg`
- **Star history chart / Star 鍘嗗彶鍥捐〃**: Added to README
- **Enhanced badges / 澧炲己寰界珷**: Watchers, Forks, Discussions

### 鉁?Fixed / 淇
- **CA1062 Warnings**: Added `ArgumentNullException.ThrowIfNull` to all public methods
- **CA2024 Warnings**: Fixed `ProcessRunner.PumpAsync`
- **Version mismatch**: Fixed NetworkAcceleration plugin.json

---

**Last Updated / 鏈€鍚庢洿鏂?*: 2026-07-06 23:59 (Day 6 complete)  
**Next Release / 涓嬫鍙戝竷**: v1.3.0-quality (target: 2026-07-12)  
**Goals / 鐩爣**: 100+ GitHub stars, 5 plugins, 0 warnings, performance optimized, Reddit promotion

---

## [2026-07-07] Session 25 鈥?Host v4.2.1 Sync + Serilog Transitive Dependency Fix

### Added / 鏂板
- **Host DLL sync to v4.2.1** (from stale v3.6.14): `LenovoLegionToolkit.Lib.dll`, `LenovoLegionToolkit.Lib.Plugins.dll` (newly added), and `Universal Device Toolkit.dll` (renamed from `Lenovo Legion Toolkit.dll`) all synced to v4.2.1.0
- **Serilog transitive dependency**: Vendored `Serilog.dll` v4.3.0, `Serilog.Sinks.Async.dll` v2.1.0, and `Serilog.Sinks.File.dll` v7.0.0 into `Dependencies\Host\` (v4.2.1 `LenovoLegionToolkit.Lib.dll` requires Serilog 4.3.0 at runtime via `Log..ctor()`)
- **CopyHostDependenciesToOutput target**: New MSBuild target in `Directory.Build.targets` that copies all `Dependencies\Host\*.dll` to test/tool project output directories (`IsPluginTestProject == True OR IsPluginToolProject == True`)
- **BatteryHealth Workbench load fix**: PluginLoader.IsVersionCompatible now accepts v4.2.1 host (was rejecting with null due to `3.6.14 < 3.6.15` MinimumHostVersion mismatch)

### Changed / 璋冩暣
- **CS0104 type ambiguity fix**: Added `using PluginHostMode = LenovoLegionToolkit.Plugins.SDK.PluginHostMode;` and `using PluginHostContext = LenovoLegionToolkit.Plugins.SDK.PluginHostContext;` aliases to `Tools\PluginWorkbench\MainWindow.xaml.cs` (both `Lib.Plugins` and `SDK` namespaces define these types; SDK types are the consumer-facing ones and the only ones type-compatible with the existing `PluginHostContext.Current = _hostContext` setter)
- **host-release.json updated**: Added `libPlugins` artifact, `transitiveDependencies` array (Serilog DLLs), updated `downloadUrl` to `UniversalDeviceToolkit_v4.2.1_win-x64.zip`, bumped `hostVersion` to `4.2.1`
- **ensure-host-dependencies.ps1 + refresh-host-references.ps1**: Added 3 Serilog DLLs to `$requiredFiles`; sibling resolver checks for Serilog presence; fallback package name `UniversalDeviceToolkit_v...`
- **Directory.Build.props**: `EnsureHostDependencies` target condition now also checks for Serilog DLLs; CleanupPluginOutput now removes `Universal Device Toolkit.*` (was `Lenovo Legion Toolkit.*`)

### Fixed / 淇
- **ViveTool.Tests**: 14/186 tests failing with `FileNotFoundException: Serilog, Version=4.3.0.0` 鈥?fixed by vendoring Serilog
- **NetworkAcceleration.Tests**: 1/39 test failing with same Serilog error 鈥?fixed by vendoring Serilog
- **PluginWorkbench.csproj**: `Lenovo Legion Toolkit` reference renamed to `Universal Device Toolkit`
- **Shared.Tests/ShellIntegration.Tests/ViveTool.Tests csproj**: `Lenovo Legion Toolkit` reference renamed to `Universal Device Toolkit`
- **PluginWorkbenchHostContext.cs / PluginWorkbenchSession.cs**: Verified no ambiguity (only import SDK namespace / fully-qualify `PluginHostMode` as SDK)

### Verification / 楠岃瘉
- `dotnet build LenovoLegionToolkit-Plugins.sln -c Release` 鈫?0 warnings / 0 errors (11.0s)
- `dotnet test` 鈫?**409/409 PASS** (BatteryHealth 16, CustomMouse 54, ShellIntegration 114, NetworkAcceleration 39, ViveTool 186)
- `PluginWorkbench.Smoke` 鈫?**10/10 PASS** (5 plugins 脳 {Dark, Light} themes)
- Visual captures saved to `artifacts\workbench-visual\{plugin}-{theme}\{preview,settings,real-runtime}.png`

### M-010 Constraint Honored / M-010 绾︽潫閬靛畧
- **NOT renamed** (per M-010 ABI gate): `LenovoLegionToolkit.Plugins.*` namespaces, `LenovoLegionToolkit-Plugins.sln` filename, `*.csproj` filenames, plugin assembly names, `plugin.manifest.json` `class` field, DLL names, `store.json` `minLLTVersion` JSON property name
- **Renamed** (user-visible/build references only): WPF `AssemblyName` `Lenovo Legion Toolkit` 鈫?`Universal Device Toolkit`, `host-release.json` package/URL, cross-csproj `<Reference>` names
