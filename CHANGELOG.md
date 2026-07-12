# Changelog / 鏇存柊鏃ュ織

All notable changes to this project will be documented in this file.
姝ら」鐩殑鎵€鏈夐噸瑕佹洿鏀归兘灏嗗湪姝ゆ枃浠朵腑璁板綍�?

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
鏍煎紡鍩轰簬 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)�?
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
骞堕伒寰?[璇箟鍖栫増鏈琞(https://semver.org/spec/v2.0.0.html)�?

## [Unreleased]
### Changed
- **README screenshots refreshed (2026-07-12)**: Updated `Assets/Screenshot_main.png` (1300×850 home, dark) and `Assets/Screenshot_zh-hans.png` (zh-Hans UI). Brand binaries unified under repo-root `Assets/`.
- **Docs sync (EN / zh-Hans)**: Aligned download notes (winget pending), language-pack privacy, feature glance table, network acceleration legacy note, documentation index, screenshots & troubleshooting sections, and Star History block in `README_zh-hans.md`.
- **Code comments**: PerformanceTest console/report strings and remaining C# `//` comments are English-only.

### Fixed
- **Log.Shutdown/Dispose SemaphoreSlim handle leak (BUG-2026-07-09-004)**: Centralized all teardown into a single DisposeCoreAsync() to guarantee _logger and _emergencyLock are disposed exactly once regardless of whether Shutdown, ShutdownAsync, or Dispose fires first. Added regression tests verifying Shutdown-then-Dispose and concurrent ShutdownAsync/Dispose races do not throw double-dispose exceptions.

### Added / 鏂板�?
- FlaUI + WinRT OCR automated UI verification pipeline (Pillar C) �?base class `FlaUiTestBase`, collection definition, setup tests, and main window tests for automated UI validation / FlaUI + WinRT OCR 鑷姩鍖?UI 楠岃瘉绠￠亾锛圥illar C锛夆€斺�?鍩虹�?`FlaUiTestBase`銆侀泦鍚堝畾涔夈€佸畨瑁呮祴璇曘€佷富绐楀彛娴嬭瘯锛岀敤浜庤嚜鍔ㄥ�?UI 楠岃�?
- Admin test runner script (`run_flaui_tests_admin.ps1`) for easy FlaUI test execution with auto-elevation / 绠＄悊鍛樻祴璇曡繍琛岃剼鏈紙`run_flaui_tests_admin.ps1`锛夛紝鏀寔鑷姩鎻愭潈锛岀畝鍖?FlaUI 娴嬭瘯鎵ц�?
- FlaUI testing guide (`Docs/FlaUI_Testing.md`) with prerequisites, quick start, troubleshooting, and writing new tests / FlaUI 娴嬭瘯鎸囧崡锛坄Docs/FlaUI_Testing.md`锛夛紝鍖呭惈鍏堝喅鏉′欢銆佸揩閫熷紑濮嬨€佹晠闅滄帓闄ゅ拰缂栧啓鏂版祴璇?

### Fixed / 淇�?

- [Build][Pillar 0] BUG-2026-07-09-001: UniversalDeviceToolkit.sln normalized for the 7 x64-only project GUIDs (DC01FDB3,4B902DDC,CB52B339,AC885CE1,656AC74B,2C7AB13C,BB54FD85); Debug|Any CPU = Debug|x64 / Release|Any CPU = Release|x64 mappings present, duplicate Debug|x64/Release|x64 pairs removed. Eliminates 7x MSB4121 warnings and duplicate MSBuild task lines. Verified: dotnet build -c Debug -m:1 --no-incremental -> 0 warnings, 0 errors, no MSB4121.

- [Localization][Pillar B] ColorPickerControl Hex label now binds `{x:Static resources:Resource.Color_Hex}` instead of a hardcoded `"Hex"` string (`ColorPickerControl.xaml` L60), so the label is localized across all 78+ locales / [鏈湴鍖朷[鏀煴 B] ColorPickerControl 鍗佸叚杩涘埗鏍囩鐜扮粦�?`{x:Static resources:Resource.Color_Hex}`锛屾浛浠ｇ‖缂栫爜 `"Hex"`锛坄ColorPickerControl.xaml` �?0琛岋級锛屼娇璇ユ爣绛惧湪 78+ 璇█涓嬫湰鍦板�?
- [Localization][Pillar B] ColorPickerControl OK button now binds `{x:Static resources:Resource.OK}` instead of a hardcoded `"OK"` string (`ColorPickerControl.xaml` L131), so the button is localized across all 78+ locales / [鏈湴鍖朷[鏀煴 B] ColorPickerControl 纭畾鎸夐挳鐜扮粦�?`{x:Static resources:Resource.OK}`锛屾浛浠ｇ‖缂栫爜 `"OK"`锛坄ColorPickerControl.xaml` �?31琛岋級锛屼娇璇ユ寜閽湪 78+ 璇█涓嬫湰鍦板�?
- [UDT-007][Threading & WMI] WMI extension default timeouts were actually lowered `5000ms` to `2500ms` in both `ManagementObjectSearcherExtensions` and `ManagementEventWatcherExtensions` (the phantom UDT-001 archive `**Fixed**` claim had never landed in code, verified via `git show HEAD`) and a 3-space to 4-space indentation regression on the four declaration lines was corrected / [UDT-007][绾跨▼涓?WMI] 涓や�?WMI 鎵╁睍鐨勯粯璁よ秴鏃剁湡姝ｇ�?`5000ms` 涓嬭皟鑷?`2500ms`锛堟鍓嶇殑 UDT-001 褰掓�?`**Fixed**` 澹扮О浠庢湭鐪熸钀藉湴浜庝唬鐮侊紝�?`git show HEAD` 鏍稿疄锛夛紝骞朵慨姝ｄ簡鍥涘鏂规硶澹版槑琛?3 绌烘牸鍒?4 绌烘牸鐨勭缉杩涘洖褰?
- [UDT-008][UI & Localization] 3 GPU status-dot `Ellipse.Fill` literals in `StatusWindow.xaml` were hardcoded raw hex (`#FF8BC34A` / `#F2A541` / `#BF360C`); now bound to `{DynamicResource StatusSuccessBrush}` / `{DynamicResource StatusWarningBrush}` / `{DynamicResource StatusCriticalBrush}` so they follow light/dark theme swaps and align with the shared chart-keyed status-color vocabulary used across all dashboard indicators / [UDT-008][UI 涓庢湰鍦板寲] `StatusWindow.xaml` �?3 �?GPU 鐘舵€佸渾鐐?`Ellipse.Fill` 鍘熶负纭紪鐮佸崄鍏繘鍒堕鑹诧紙`#FF8BC34A` / `#F2A541` / `#BF360C`锛夛紱鐜扮粦�?`{DynamicResource StatusSuccessBrush}` / `{DynamicResource StatusWarningBrush}` / `{DynamicResource StatusCriticalBrush}`锛岃窡闅忔繁娴呰壊涓婚鍒囨崲锛屽苟涓庡叏浠〃鐩樻寚鏍囧叡浜殑鍥捐〃閿€肩姸鎬佽壊绯诲�?
- [Test Reliability] `SensorReadTimeoutSeconds` virtual seam added to `AbstractSensorsController` so the flaky detailed-sensor-read test can override the 2s production cap to 60s via a mock (`SensorReadTimeoutSeconds => 60`), restoring deterministic test timing without weakening the production timeout / [娴嬭瘯鍙潬鎬 `AbstractSensorsController` 鏂板�?`SensorReadTimeoutSeconds` 铏氭帴鍙ｏ紝浣夸笉绋冲畾鐨勪紶鎰熷櫒璇︾粏璇诲彇娴嬭瘯鍙€氳繃 mock 瑕嗙�?2 绉掔敓浜т笂闄愪负 60 绉掞紙`SensorReadTimeoutSeconds => 60`锛夛紝鍦ㄤ笉鍓婂急鐢熶骇瓒呮椂鐨勫墠鎻愪笅鎭㈠娴嬭瘯鏃跺簭鐨勭‘瀹氭�?
- [Brand Migration] Uzbek (`uz`) resource file user-facing brand string in `ExcludeRefreshRatesWindow_NoRefreshRatesFound_Message` (`Resource.uz.resx` L476) now reads `Universal Device Toolkit` instead of the legacy `Lenovo Legion Toolkit`, so the no-refresh-rate-found message uses the current product name in Uzbek / [鍝佺墝杩佺Щ] 涔屽吂鍒厠璇紙`uz`锛夎祫婧愭枃浠朵腑闈㈠悜鐢ㄦ埛鍝佺墝瀛楃涓?`ExcludeRefreshRatesWindow_NoRefreshRatesFound_Message`锛坄Resource.uz.resx` �?76琛岋級鐜版敼�?`Universal Device Toolkit`锛屾浛浠ｆ棫�?`Lenovo Legion Toolkit`锛屼娇涔屽吂鍒厠璇?鏈壘鍒板埛鏂扮�?鎻愮ず浣跨敤褰撳墠浜у搧鍚?
- [Brand Migration] PerformanceTest console boot banner and report title now display `Universal Device Toolkit` instead of the legacy `Lenovo Legion Toolkit` (`Program.cs` L21 boot banner, L339 report title) / [鍝佺墝杩佺Щ] PerformanceTest 鎺у埗鍙板惎鍔ㄦí骞呬笌鎶ュ憡鏍囬鐜版樉绀?`Universal Device Toolkit`锛屾浛浠ｆ棫�?`Lenovo Legion Toolkit`锛坄Program.cs` �?1琛屽惎鍔ㄦí骞呫€佺339琛屾姤鍛婃爣棰橈�?
- [Pre-existing Fix] `UniversalDeviceToolkit.PerformanceTest.csproj` XML declaration had a stray `"` between `?` and `>` (`<?xml ... ?>"?>`), making MSBuild unable to parse the project; byte-level corrected to `<?xml version="1.0" encoding="utf-8"?>` (this project was never in the main solution `UniversalDeviceToolkit.sln` and had never compiled before) / [棰勫瓨淇] `UniversalDeviceToolkit.PerformanceTest.csproj` XML 澹版槑涓?`?` �?`>` 涔嬮棿澶氫簡涓€�?`"`锛坄<?xml ... ?>"?>`锛夛紝瀵艰�?MSBuild 鏃犳硶瑙ｆ瀽椤圭洰锛涘凡瀛楄妭绾т慨姝ｄ负 `<?xml version="1.0" encoding="utf-8"?>`锛堣椤圭洰浠庢湭鍖呭惈鍦ㄤ富瑙ｅ喅鏂规�?`UniversalDeviceToolkit.sln` 涓紝姝ゅ墠浠庢湭缂栬瘧�?
- [Pre-existing Fix] 9 corrupted Chinese string literals in `UniversalDeviceToolkit.PerformanceTest/Program.cs` whose last Chinese glyph + closing `"` had been replaced by `U+FFFD` + `?` (causing CS1010 newline-in-constant errors when the project was first compiled) reconstructed from context and restored: `L33 "�?`, `L35 "�?`, `L38 "�?`, `L45 "�?`, `L145 "�?"`, `L227 "�?`, `L240 "�?`, `L314 "�?`, `L357 "�?` / [棰勫瓨淇] `UniversalDeviceToolkit.PerformanceTest/Program.cs` �?9 澶勪腑鏂囧瓧绗︿覆瀛楅潰閲忔渶鍚庝竴涓腑鏂囧瓧涓庨棴鍚堝紩鍙?`"` 琚浛鎹负 `U+FFFD` + `?`锛堝鑷撮」鐩娆＄紪璇戞椂鎶?CS1010 甯搁噺涓湁鎹㈣绗︼級锛屽凡鏍规嵁涓婁笅鏂囬噸寤烘仮澶嶏細`L33 "�?`銆乣L35 "�?`銆乣L38 "�?`銆乣L45 "�?`銆乣L145 "�?"`銆乣L227 "�?`銆乣L240 "�?`銆乣L314 "�?`銆乣L357 "�?`
- [UDT-006][Runtime Stability] `MacroController.Stop()` no longer orphans the global `WH_KEYBOARD_LL` macro hook or fires macros twice: the hook handle is now cleared first and `UnhookWindowsHookEx` runs FIRST, with recorder/player teardown isolated in separate traced `try/catch` blocks (was a silent comment-only `catch { }` whose `finally` still zeroed the field, so a teardown throw skipped the unhook and the next `Start()` re-installed a second hook) / [UDT-006][杩愯鏃剁ǔ瀹氭€ `MacroController.Stop()` 涓嶅啀娉勬紡鍏ㄥ眬 `WH_KEYBOARD_LL` 瀹忛挬瀛愭垨瀵艰嚧瀹忛噸澶嶈Е鍙戯細鐜板厛娓呯悊閽╁瓙鍙ユ焺骞舵渶浼樺厛鎵ц `UnhookWindowsHookEx`锛屽綍鍒跺櫒/鎾斁鍣ㄦ媶鍒嗗悇鑷嫭绔嬪甫鎵撶偣�?`try/catch`锛堝師涓洪潤榛樼殑娉ㄩ噴鍗犱�?`catch { }`锛屽�?`finally` 浠嶆竻闆跺瓧娈碉紝涓€鏃︽媶鍒嗘姏寮傚父鍒欒烦杩囧嵏閽╋紝涓嬫 `Start()` 閲嶅瀹夎绗簩涓挬瀛愶�?
- [UDT-005][Test Reliability] FlaUI main-window UI tests no longer hard-fail when the runner lacks an admin/desktop session: `FlaUIMainWindowTests` methods switched from `[Fact]` to `[SkippableFact]` (with `MainWindow!` null-forgiving on the two deref sites), so an unavailable elevation now yields a clean skip instead of a `SkipException` failure (previously 3 failures) / [UDT-005][娴嬭瘯鍙潬鎬 鍙楃鐞嗗憳鏉冮�?妗岄潰浼氳瘽闄愬埗鏃?FlaUI 涓荤獥鍙ｆ祴璇曚笉鍐嶇‖鎬ф姤閿欙細`FlaUIMainWindowTests` 鏂规硶鐢?`[Fact]` 鏀逛负 `[SkippableFact]`锛堜袱澶勮В寮曠敤�?`MainWindow!` 绌烘姂鍒讹級锛岀己灏戞彁鏉冩椂鐜板湪涓哄共鍑€璺宠繃鑰岄�?`SkipException` 澶辫触锛堟鍓嶄�?3 涓け璐ワ級
- [Brand Migration] FPS self-monitoring blacklist (`FpsSensorController`) now also excludes the renamed `Universal Device Toolkit` process alongside the legacy `Lenovo Legion Toolkit`, so the app own window is no longer counted as a monitored foreground app / [鍝佺墝杩佺Щ] FPS 鑷洃鍚粦鍚嶅崟锛坄FpsSensorController`锛夌幇鍚屾椂鎺掗櫎鏂板悕 `Universal Device Toolkit` 涓庢棫鍚?`Lenovo Legion Toolkit` 杩涚▼锛岄伩鍏嶆湰宸ュ叿绐楀彛琚鍏ュ墠鍙扮洃�?
- [Brand Migration] HWiNFO custom sensor group now registers under `Universal Device Toolkit`; on stop the legacy `Lenovo Legion Toolkit` registry group is cleaned too, so pre-rename sensors do not linger in HWiNFO / [鍝佺墝杩佺Щ] HWiNFO 鑷畾涔変紶鎰熷櫒鍒嗙粍鐜版敞鍐屼簬 `Universal Device Toolkit`锛涘仠姝㈡椂涓€骞舵竻鐞嗘棫 `Lenovo Legion Toolkit` 娉ㄥ唽琛ㄥ垎缁勶紝鏃х増浼犳劅鍣ㄤ笉鍐嶆畫鐣欎�?HWiNFO
- [Brand Migration] Plugin Workbench host resource resolver now matches both `Universal Device Toolkit` and `Lenovo Legion Toolkit` host assemblies (was legacy-only), restoring resource lookup under the renamed host / [鍝佺墝杩佺Щ] 鎻掍欢宸ヤ綔鍙板涓昏祫婧愯В鏋愮幇鍚屾椂鍖归�?`Universal Device Toolkit` �?`Lenovo Legion Toolkit` 瀹夸富绋嬪簭闆嗭紙鍘熶粎鍖归厤鏃у悕锛夛紝鎭㈠閲嶅懡鍚嶅涓讳笅鐨勮祫婧愭煡�?
- [Brand Migration] Audit confirms the user-facing brand is fully migrated to `Universal Device Toolkit`; `LenovoLegionToolkit.*` assembly/namespace identifiers are intentionally retained as cross-repository ABI contracts on which plugin loading depends / [鍝佺墝杩佺Щ] 瀹¤纭鐢ㄦ埛闈㈠悜鍝佺墝宸插畬鎴愬悜 `Universal Device Toolkit` 鐨勮縼绉伙紱`LenovoLegionToolkit.*` 绋嬪簭闆?鍛藉悕绌洪棿鏍囪瘑绗︿綔涓鸿法浠撳簱 ABI 濂戠害鏈夋剰淇濈暀锛堟彃浠跺姞杞戒緷璧栬鍚嶇О�?
- [UDT-001][Threading & WMI] WMI helper extension default timeouts in `ManagementObjectSearcherExtensions` and `ManagementEventWatcherExtensions` lowered from `5000ms` to `2500ms` to respect the WMI timeout ceiling; default callers rely on a linked `CancellationToken` / [UDT-001][绾跨▼涓?WMI] `ManagementObjectSearcherExtensions` �?`ManagementEventWatcherExtensions` �?WMI 鍔╂墜榛樿瓒呮椂鐢?`5000ms` 涓嬭皟鑷?`2500ms`锛岄伒�?WMI 瓒呮椂涓婇檺锛涢粯璁よ皟鐢ㄦ柟浣跨敤鑱斿姩鐨?`CancellationToken`
- [UDT-002][Threading & WMI] WMI method-invoke bound in `WMI.CallInternalAsync` lowered from a hardcoded `10000ms` to a `2500ms` constant (`WmiInvokeTimeoutMs`); a hung WMI method now fails fast instead of stalling the caller 10s / [UDT-002][绾跨▼涓?WMI] `WMI.CallInternalAsync` �?WMI 鏂规硶璋冪敤涓婇檺鐢辩‖缂栫爜 `10000ms` 涓嬭皟涓?`2500ms` 甯搁噺锛坄WmiInvokeTimeoutMs`锛夛紱鎸傝捣�?WMI 鏂规硶鐜板湪蹇€熷け璐ワ紝涓嶅啀闃诲璋冪敤�?10 �?
- [UDT-003][Threading & WMI] `AmdOverclockingController.FetchCommands` no longer runs synchronous `ManagementObject` construction and `WMI.InvokeMethodAndGetValue` on the calling thread; converted to `FetchCommandsAsync` wrapped in `Task.Run(...).WaitAsync(2500ms)` with `.ConfigureAwait(false)` so AMD overclocking no longer stalls the UI 5-10s / [UDT-003][绾跨▼涓?WMI] `AmdOverclockingController.FetchCommands` 涓嶅啀鍦ㄨ皟鐢ㄧ嚎绋嬪悓姝ユ墽�?`ManagementObject` 鏋勯€犱�?`WMI.InvokeMethodAndGetValue`锛涘凡鏀逛负 `FetchCommandsAsync`锛岄€氳�?`Task.Run(...).WaitAsync(2500ms)` �?`.ConfigureAwait(false)` 鍖呰锛岄伩�?AMD 瓒呴鍔熻兘闃诲�?UI 5-10 �?
- [UDT-004][Runtime Stability & Telemetry] Empty `catch { }` in the `Registry` listener teardown at `Registry.cs` no longer silently swallows exceptions; it now traces via `Log.Instance.Trace(...)` matching the surrounding listener loop so cancelled/stuck listener tasks stay observable / [UDT-004][杩愯鏃剁ǔ瀹氭€т笌鍙娴嬫€�?`Registry.cs` 娉ㄥ唽琛ㄧ洃鍚櫒娓呯悊璺緞涓殑�?`catch { }` 涓嶅啀闈欓粯鍚炴帀寮傚父锛涚幇宸叉敼涓洪€氳繃 `Log.Instance.Trace(...)` 鎵撶偣锛堜笌鍛ㄥ洿鐩戝惉寰幆涓€鑷达級锛屼娇琚彇�?鍗′綇鐨勭洃鍚换鍔″彲琚娴?
- [Threading] `SmartFnLockController` no longer floods the thread pool with a new fire-and-forget polling task on every enable cycle; polling is coalesced onto a single guarded long-running task / [绾跨▼] `SmartFnLockController` 涓嶅啀姣忔鍚敤閮芥柊寤烘棤绛夊緟杞浠诲姟瀵艰嚧绾跨▼姹犳椽姘达紱杞宸插悎骞跺埌鍗曚釜鍙椾繚鎶ょ殑闀垮懆鏈熶换鍔′笂
- [Threading] `MacroController` global `WH_KEYBOARD_LL` macro-recording hook no longer drops keystrokes or leaks the hook; the hook thread now runs a Windows message pump so events keep being delivered and the hook is reliably unhooked on stop / [绾跨▼] `MacroController` 鍏ㄥ�?`WH_KEYBOARD_LL` 瀹忓綍鍒堕挬瀛愪笉鍐嶄涪閿垨娉勬紡閽╁瓙锛涢挬瀛愮嚎绋嬬幇鍦ㄨ繍琛?Windows 娑堟伅娉碉紝纭繚浜嬩欢鎸佺画娲惧彂锛屽苟鍦ㄥ仠姝㈡椂鍙潬鍗歌浇閽╁瓙
- Dashboard layout customizations (groups, items order, sensors toggle) are now persisted across app restarts; `DashboardSettings` IoC registration changed from transient to `SingleInstance()` so EditDashboardWindow and DashboardPage share the same in-memory cache and `SynchronizeStore()` writes are immediately visible to all consumers / 鎺у埗鍙拌嚜瀹氫箟甯冨眬锛堝垎缁勩€侀」鐩『搴忋€佷紶鎰熷櫒寮€鍏筹級鐜板湪鍙湪搴旂敤閲嶅惎鍚庢寔涔呭寲淇濈暀锛沗DashboardSettings` �?IoC 娉ㄥ唽浠庣灛鏃舵敼涓?`SingleInstance()`锛岀‘淇?EditDashboardWindow �?DashboardPage 鍏变韩鍚屼竴鍐呭瓨缂撳瓨锛宍SynchronizeStore()` 鍐欏叆瀵规墍鏈夋秷璐硅€呯珛鍗冲彲�?
- Update flow no longer falls back to GitHub because the installer file-name pattern required a hyphen that the actual installer never has; relaxed pattern matches `UniversalDeviceToolkitSetup*.exe` so in-app updates run again / 鏇存柊娴佺▼涓嶅啀鍥犲畨瑁呭寘鏂囦欢鍚嶅尮閰嶈鍒欓敊璇紙瑕佹眰杩炲瓧绗︼級鑰屽洖閫€鍒?GitHub锛涚幇宸叉敼涓哄尮閰嶅疄闄呯�?`UniversalDeviceToolkitSetup*.exe` 鍛藉悕锛屾仮澶嶅簲鐢ㄥ唴鑷姩鏇存柊
- Clicking external links (release page, device info link, etc.) no longer fails with `Win32Exception`; URL opening keeps `UseShellExecute=true` (required for protocol handlers) behind a strict HTTP/HTTPS scheme allow-list / 鎵撳紑澶栭儴閾炬帴锛堝彂琛岄〉銆佽澶囦俊鎭摼鎺ョ瓑锛変笉鍐嶅�?`Win32Exception` 澶辫触锛沀RL 寮€鍚繚鐣?`UseShellExecute=true`锛堝崗璁鐞嗗櫒闇€瑕侊級锛屽苟鐢变弗鏍肩�?HTTP/HTTPS 鍗忚鐧藉悕鍗曚繚鎶?
- Dashboard sensors card no longer shows overlapping shimmer and live gauge animations; removed the duplicate page-level sensor skeleton and keep only the layout-matched `SensorsControl` overlay, which now tracks compact/wide layout on resize / 鎺у埗鍙颁紶鎰熷櫒鍗＄墖涓嶅啀鍙犲姞涓ゅ shimmer 涓庝华琛ㄥ姩鐢伙紱绉婚櫎浠〃鐩橀〉閲嶅鐨勪紶鎰熷櫒楠ㄦ灦灞傦紝浠呬繚鐣欎笌甯冨眬璐村悎�?`SensorsControl` 瑕嗙洊灞傦紝骞跺湪绐楀彛缂╂斁鏃跺悓姝ョ揣�?瀹藉睆甯冨眬
- About Device window cards no longer show empty white blocks on the left; row content is placed in `CardControl.Header` per WPF-UI template / 鍏充簬璁惧绐楀彛鍗＄墖宸︿晶涓嶅啀鍑虹幇绌虹櫧鑹插潡锛涜鍐呭宸叉寜 WPF-UI 妯℃澘鏀惧叆 `CardControl.Header`
- Windows Optimization checkboxes now surface apply failures (registry access denied, command exit codes, verification mismatch) via snackbar instead of silently reverting; unchecking uses rollback where available / Windows 浼樺寲鍕鹃€夐」鍦ㄥ簲鐢ㄥけ璐ワ紙娉ㄥ唽琛ㄦ嫆缁濊闂€佸懡浠ら潪闆堕€€鍑恒€侀獙璇佷笉鍖归厤锛夋椂浼氶€氳�?Snackbar 鎻愮ず锛屼笉鍐嶉潤榛樺洖閫€锛涘彇娑堝嬀閫夋椂鍦ㄦ敮鎸佺殑鎯呭喌涓嬫墽琛屽洖�?
- Windows Optimization Shell Integration category no longer shows raw resource keys (`WindowsOptimization_Category_NilesoftShell_*`); host resources now supply localized titles/descriptions and plugin lookups fall back to them / Windows 浼樺寲椤?Shell Integration 鍒嗙被涓嶅啀鏄剧ず鏈炕璇戠殑璧勬簮閿紙`WindowsOptimization_Category_NilesoftShell_*`锛夛紝瀹夸富璧勬簮宸茶ˉ鍏ㄤ腑鑻辨枃鏂囨骞跺湪鎻掍欢璧勬簮缂哄け鏃跺洖閫€璇诲�?
- Settings page no longer crashes on open when `ColorPicker.Models.dll` was missing from Release output; the dependency is now referenced explicitly / 淇璁剧疆椤靛�?Release 杈撳嚭缂哄皯 `ColorPicker.Models.dll` 鑰屾棤娉曟墦寮€鐨勯棶棰橈紝鐜板凡鏄惧紡寮曠敤璇ヤ緷�?in `UniversalDeviceToolkit.WPF/Resources/Resource.sq.resx` now uses the same `at {0} ({1})` placeholder shape for `AutomationPipelineControl_SubtitlePart_AtTime` as the English resource (`n�?{0} ({1})`), so the automation-pipeline subtitle renders the time and timezone correctly instead of an arbitrary `HH:mm` colon-joined string / `UniversalDeviceToolkit.WPF/Resources/Resource.sq.resx` �?`AutomationPipelineControl_SubtitlePart_AtTime` 鐨勫崰浣嶇鏍煎紡宸蹭笌鑻辨枃璧勬簮 `at {0} ({1})` 瀵归綈涓?`n�?{0} ({1})`锛岃嚜鍔ㄥ寲娴佹按绾垮壇鏍囬鐜板湪鍙纭樉绀烘椂闂翠笌鏃跺尯锛岃€屼笉鏄敊璇�?`HH:mm` 鍐掑彿鎷兼帴
- Backfilled simplified Chinese (`zh-Hans`) UI strings for Plugin Extensions, Windows Optimization, sensors, and large-file cleanup by syncing from `zh-Hant` and manually translating newly added keys, so Chinese UI no longer mixes English labels like `Available`, `Configure`, or `Preparing download...` / �?`zh-Hant` 鍚屾骞惰ˉ鍏ㄦ柊澧為敭鐨勭畝浣撲腑鏂囩炕璇戯紝淇鎻掍欢鎵╁睍銆乄indows 浼樺寲銆佷紶鎰熷櫒涓庡ぇ鏂囦欢娓呯悊绛夌晫闈腑鑻辨贩鎺掞紙�?`Available`銆乣Configure`銆乣Preparing download...`�?
- Plugin Extensions install/configure/open/uninstall icon buttons now render explicit symbols and stay vertically centered in each card in Dark/Light themes / 鎻掍欢鎵╁睍椤靛畨瑁?閰嶇�?鎵撳�?鍗歌浇鍥炬爣鎸夐挳鍦ㄦ繁娴呰壊涓婚涓嬫纭樉绀虹鍙峰苟鍦ㄥ崱鐗囧唴鍨傜洿灞呬�?
- Packages page readme/download/cancel action buttons no longer render as empty boxes in light mode; icons use explicit `SymbolIcon` foreground like Plugin Extensions / 椹卞姩鍖呭垪琛ㄩ�?readme/涓嬭�?鍙栨秷鎿嶄綔鎸夐挳鍦ㄦ祬鑹叉ā寮忎笅涓嶅啀鏄剧ず涓虹┖鐧芥柟妗嗭紝鍥炬爣鏀圭敤涓庢彃浠舵墿灞曢〉涓€鑷寸殑鏄惧�?`SymbolIcon` 鍓嶆櫙鑹?
- Macro event card titles for mouse actions (MOVE, WHEEL DOWN/UP/LEFT/RIGHT, XBUTTON, LBUTTON, RBUTTON, MBUTTON) are now localized via `Lib.Macro.Resources.Resource` instead of hardcoded English strings / 瀹忎簨浠跺崱鐗囦腑榧犳爣鎿嶄綔鐨勬爣棰橈紙MOVE銆乄HEEL DOWN/UP/LEFT/RIGHT銆乆BUTTON銆丩BUTTON銆丷BUTTON銆丮BUTTON锛夌幇閫氳繃 `Lib.Macro.Resources.Resource` 鏈湴鍖栵紝涓嶅啀纭紪鐮佽嫳�?
- Fixed 7 pre-existing test failures (source-code structure tests outdated after refactors, WMI timeout in VantagePackageDownloaderTests); all 2343 tests now pass / 淇�?7 涓瀛樼殑娴嬭瘯澶辫触锛堟簮浠ｇ爜缁撴瀯娴嬭瘯鍦ㄩ噸鏋勫悗鏈悓姝ユ洿鏂般€乂antagePackageDownloaderTests �?WMI 瓒呮椂锛夛紝鐜板�?2343 涓祴璇曞叏閮ㄩ€氳�?
- Language selector and main window no longer pop up simultaneously on first launch; `LocalizationHelper.cs` changed `window.Show()` to `window.ShowDialog()` so language selection blocks until complete / 棣栨鍚姩鏃惰瑷€閫夋嫨绐楀彛涓庝富绐楀彛涓嶅啀鍚屾椂寮瑰嚭锛沗LocalizationHelper.cs` �?`window.Show()` 鏀逛负 `window.ShowDialog()`锛岃瑷€閫夋嫨瀹屾垚鍓嶉樆濉炲悗缁祦�?
- Card subtitle text (Chinese localization) no longer overflows and bloats card height; `CardHeaderControl` now enforces `MaxHeight=60` (�? lines) on subtitle `TextBlock`, and 25+ overly long Chinese `Message`-suffixed resource strings in `Resource.zh-hans.resx` were shortened to �?5 chars per line with intentional `\n` breaks / 鍗＄墖鍓爣棰樻枃鏈紙涓枃鏈湴鍖栵級涓嶅啀婧㈠嚭骞舵拺楂樺崱鐗囷紱`CardHeaderControl` 鐜板湪瀵瑰壇鏍囬 `TextBlock` 寮哄�?`MaxHeight=60`锛堢�?3 琛岋級锛宍Resource.zh-hans.resx` �?25+ 鏉¤繃闀跨殑 `Message` 鍚庣紑涓枃璧勬簮瀛楃涓插凡缂╃煭鑷虫瘡�?�?5 瀛楃骞舵坊鍔犲繀瑕佺�?`\n` 鎹㈣�?
- System-wide stutter after closing the app is fixed; `NativeWindowsMessageListener` (global `WH_KEYBOARD_LL` hook) and `UserInactivityAutoListener` (global `WH_KEYBOARD_LL` + `WH_MOUSE_LL` hooks) were not stopped on app exit, causing Windows to keep calling into the dead process �?`App.xaml.cs` `PerformShutdownAsync()` now properly stops both listeners, and `AppDomain.ProcessExit` handler added as defense-in-depth / 淇鍏抽棴搴旂敤鍚庣郴缁熷叏灞€鍗￠】鐨勯棶棰橈紱`NativeWindowsMessageListener`锛堝叏灞�?`WH_KEYBOARD_LL` 閽╁瓙锛夊拰 `UserInactivityAutoListener`锛堝叏灞�?`WH_KEYBOARD_LL` + `WH_MOUSE_LL` 閽╁瓙锛夊湪搴旂敤閫€鍑烘椂鏈鍋滄锛屽鑷?Windows 鎸佺画鍚戝凡姝昏繘绋嬪彂璧烽挬瀛愬洖璋冣€斺€擿App.xaml.cs` �?`PerformShutdownAsync()` 鐜板凡姝ｇ‘鍋滄杩欎袱涓洃鍚櫒锛屽苟鏂板�?`AppDomain.ProcessExit` 澶勭悊绋嬪簭浣滀负娣卞害闃插�?

### Improved / 鏀硅繘

- Automation page Quick Actions section title now uses the standard section typography tier so it reads smaller than the page title / 鑷姩鍖栭〉銆屽揩鎹锋搷浣溿€嶅尯鍧楁爣棰樻敼鐢ㄦ爣鍑嗗尯鍧楀瓧鍙凤紝灞傜骇浣庝簬椤甸潰涓绘爣�?
- Larger default body/caption typography with gentler high-DPI compensation; title-bar device model text is easier to read / 鎻愬崌榛樿姝ｆ枃瀛楀彿骞跺噺杞婚�?DPI 缂╁皬锛屾爣棰樻爮鏈哄瀷鏂囧瓧鏇存槗璇?
- About Device window uses compact grouped rows (label + value per line) instead of one card per field / 鍏充簬璁惧绐楀彛鏀逛负鍒嗙粍绱у噾琛屽竷灞€锛屼笉鍐嶄负姣忎釜瀛楁鍗曠嫭鍗犱竴寮犲崱�?
- About Device window is wider with reduced side margins so long hardware values (e.g. GPU names) have more room / 鍏充簬璁惧绐楀彛鍔犲骞跺噺灏忓乏鍙宠竟璺濓紝闀跨‖浠朵俊鎭紙濡?GPU 鍚嶇О锛夋湁鏇村鏄剧ず绌洪棿
- Theme is applied as soon as settings load so light/dark mode no longer flashes the App.xaml dark defaults on startup / 璁剧疆鍔犺浇鍚庣珛鍗冲簲鐢ㄤ富棰橈紝閬垮厤鍚姩鏃跺厛闂繁鑹查粯璁ゆ牱�?larger card/button corner radii, seamless light-mode navigation/content surfaces, skeleton loading on Packages and Keyboard Backlight pages, smoother page transitions and button press feedback / 缁熶�?Fluent 涓诲３楂樼骇鎰燂細鍔犲ぇ鍗＄墖涓庢寜閽渾瑙掋€佷慨澶嶆祬鑹叉ā寮忓鑸笌鍐呭鍖鸿壊宸€佷负椹卞姩鍖呬笌閿洏鑳屽厜椤靛鍔犻鏋跺睆鍔犺浇锛屽苟鏀硅繘椤甸潰鍒囨崲涓庢寜閽寜鍘嬪弽棣?
- README and promotion docs now lead with taglines, UDT-vs-Vantage comparison, audience positioning, and expanded EN/ZH social/release copy templates / README 涓庡鍙戞枃妗ｆ柊澧炴爣璇€乁DT 瀵规�?Vantage 琛ㄦ牸銆佸彈浼楀畾浣嶏紝骞舵墿鍏呬腑鑻辨枃鍙戝竷涓庣ぞ浜ゆ枃妗堟ā�?
- Promotion copy rewritten in conversational tone for forums and social posts (PROMOTION_*, COMMUNITY_OUTREACH) / 瀹ｅ彂鏂囨鏀逛负鍙ｈ鍖栧啓娉曪紝閫傚悎璁哄潧涓庣ぞ鍖虹湡浜哄彂�?
- Simplified Plugin Extensions card visuals: removed heavy selection/install chrome, toolbar boxes, and left accent bar; install progress is a bottom accent line plus spinner / 绠€鍖栨彃浠舵墿灞曞崱鐗囪瑙夛細绉婚櫎閫変�?瀹夎绮楄竟妗嗐€佸伐鍏锋潯绌烘涓庡乏渚ц壊鏉★紝瀹夎杩涘害鏀逛负搴曢儴缁嗙嚎鍔犳棆杞寚绀?
- Dashboard `SensorsControl` now shows a content-shaped skeleton overlay with shimmer animation while waiting for the first sensor reading; each of the CPU/Battery/GPU sections renders a 3-row gauge+bar+chart+legend placeholder that matches the real layout, then fades out via a 0.45s `EasingCubicOut` storyboard when data is ready (or whenever initial load is restarted) / 鎺у埗�?`SensorsControl` 鐜板湪鍦ㄧ瓑寰呴娆′紶鎰熷櫒鏁版嵁鏃舵樉绀轰笌鍐呭褰㈢姸涓€鑷寸殑楠ㄦ灦灞忚鐩栧眰锛堝�?shimmer 鍔ㄧ敾锛夛紱CPU/Battery/GPU 涓夋鍚勮嚜娓叉煋涓庡疄闄呭竷灞€涓€鑷寸殑 3 琛岋紙浠�?杩涘害鏉?鍥捐�?鍥句緥锛夊崰浣嶇锛屽綋鏁版嵁灏辩华锛堟垨閲嶆柊瑙﹀彂棣栨鍔犺浇锛夋椂閫氳�?0.45s `EasingCubicOut` Storyboard 娣″嚭
- Replaced `CompositionTarget.Rendering` in `RadialGaugeControl` with a `DispatcherTimer` at ~60fps to reduce global render-event pressure when multiple gauges animate simultaneously; timer is cleaned up on `Unloaded` / �?`RadialGaugeControl` 涓�?`CompositionTarget.Rendering` 鏇挎崲涓?`DispatcherTimer`锛垀60fps锛夛紝闄嶄綆澶氫华琛ㄥ悓鏃跺姩鐢绘椂鐨勫叏灞€娓叉煋浜嬩欢鍘嬪姏锛沗Unloaded` 鏃舵竻鐞嗗畾鏃跺�?
- `TrendChartControl` and `TrendSeries` now cache `LinearGradientBrush` and `Pen` objects per series color instead of recreating them on every render pass / `TrendChartControl` �?`TrendSeries` 鐜板湪鎸夌郴鍒楅鑹茬紦�?`LinearGradientBrush` �?`Pen` 瀵硅薄锛屼笉鍐嶆瘡娆℃覆鏌撻兘閲嶆柊鍒涘�?
- `SensorsControl` now caches `FindName()` results in a `Dictionary<string, FrameworkElement?>` to avoid repeated XAML name-scope tree walks in hot update paths / `SensorsControl` 鐜板湪灏?`FindName()` 缁撴灉缂撳瓨�?`Dictionary<string, FrameworkElement?>` 涓紝閬垮厤鍦ㄧ儹鏇存柊璺緞涓弽澶嶉亶鍘?XAML 鍚嶇О浣滅敤鍩熸爲
- `DiscreteGPUControl` and `AbstractRefreshingControl` now unsubscribe from singleton events in `Unloaded` handlers to prevent memory leaks when controls are created and destroyed by page navigation / `DiscreteGPUControl` �?`AbstractRefreshingControl` 鐜板湪鍦?`Unloaded` 澶勭悊绋嬪簭涓彇娑堝鍗曚緥浜嬩欢鐨勮闃咃紝闃叉椤甸潰瀵艰埅鍒涘缓/閿€姣佹帶浠舵椂鐨勫唴瀛樻硠婕?
- `FanCurveControl.DrawGraph` now caches the `Path` and `Polygon` canvas children and updates their `Data`/`Points` in-place instead of clearing and rebuilding on every slider change; `VerifyValues` uses direct `for` loops instead of LINQ `Take`/`Skip` to avoid enumerator allocation / `FanCurveControl.DrawGraph` 鐜板湪缂撳瓨鐢诲竷瀛愬厓绱?`Path` �?`Polygon`锛屾瘡娆℃粦鍧楀彉鍖栨椂灏卞湴鏇存柊 `Data`/`Points` 鑰岄潪娓呯┖閲嶅缓锛沗VerifyValues` 浣跨敤鐩存帴 `for` 寰幆鏇夸唬 LINQ `Take`/`Skip`锛岄伩鍏嶆灇涓惧櫒鍒嗛厤
- `AutomationPage.GetSupportedAutomationStepsAsync` now runs all 30+ `IsSupportedAsync` calls in parallel via `Task.WhenAll` instead of sequentially, reducing page initialization time / `AutomationPage.GetSupportedAutomationStepsAsync` 鐜板湪閫氳繃 `Task.WhenAll` 骞惰杩愯 30+ �?`IsSupportedAsync` 璋冪敤锛屾浛浠ｅ師鏉ョ殑涓茶鎵ц锛屽噺灏戦〉闈㈠垵濮嬪寲鏃堕棿
- `ColorPickerControl` now reuses a single `SolidColorBrush` instance (updating `.Color`) instead of creating a new brush on every color change, reducing GC pressure during continuous drag / `ColorPickerControl` 鐜板湪澶嶇敤鍗曚�?`SolidColorBrush` 瀹炰緥锛堟洿�?`.Color`锛夛紝涓嶅啀姣忔棰滆壊鍙樺寲閮藉垱寤烘柊鐢诲埛锛屽噺灏戣繛缁嫋鍔ㄦ椂�?GC 鍘嬪�?
- Memory leak fixes: `AbstractToggleFeatureCardControl` and `AbstractComboBoxFeatureCardControl` now unsubscribe from `MessagingCenter` in `Unloaded`; 12 Dashboard controls (`WhiteKeyboardBacklightControl`, `PowerModeControl`, `OverclockDiscreteGPUControl`, `DpiScaleControl`, `RefreshRateControl`, `ResolutionControl`, `HDRControl`, `FnLockControl`, `MicrophoneControl`, `PanelLogoBacklightControl`, `PortsBacklightControl`, `TouchpadLockControl`, `WinKeyControl`) now unsubscribe from `_listener.Changed` in `Unloaded` / 鍐呭瓨娉勬紡淇锛歚AbstractToggleFeatureCardControl` �?`AbstractComboBoxFeatureCardControl` �?`Unloaded` 鏃跺彇娑?`MessagingCenter` 璁㈤槄锛?2 涓华琛ㄧ洏鎺т欢�?`Unloaded` 鏃跺彇娑?`_listener.Changed` 璁㈤�?
- `NativeLayeredWindow.UpdateLayeredWindow` now disposes the GDI `Bitmap` via `using` to prevent unmanaged handle leak / `NativeLayeredWindow.UpdateLayeredWindow` 鐜板湪閫氳繃 `using` 閲婃�?GDI `Bitmap`锛岄槻姝㈤潪鎵樼鍙ユ焺娉勬�?
- `RGBKeyboardBacklightControl` now unsubscribes from `MessagingCenter` and event handlers in `Unloaded` to prevent memory leaks / `RGBKeyboardBacklightControl` 鐜板湪鍦?`Unloaded` 鏃跺彇娑?`MessagingCenter` 鍜屼簨浠跺鐞嗙▼搴忕殑璁㈤槄锛岄槻姝㈠唴瀛樻硠婕?
- `Application_Exit` now uses `async void` with `await` instead of blocking `GetAwaiter().GetResult()` to prevent UI thread deadlock during shutdown; `Dispatcher.Invoke` replaced with `BeginInvoke` / `Application_Exit` 鐜板湪浣跨敤 `async void` + `await` 鏇夸唬闃诲寮忕�?`GetAwaiter().GetResult()`锛岄槻姝㈠叧闂�?UI 绾跨▼姝婚攣锛沗Dispatcher.Invoke` 鏇挎崲涓?`BeginInvoke`
- 27 `Process.Start` calls across the codebase now wrap the returned `Process` in `using` to prevent native handle leaks / 浠ｇ爜搴撲腑 27 �?`Process.Start` 璋冪敤鐜板湪灏嗚繑鍥炵殑 `Process` 鍖呰鍦?`using` 涓紝闃叉闈炴墭绠″彞鏌勬硠婕?
- `LampArrayController` now properly disposes `_renderCts` and `_screenCaptureCts` via `Dispose(bool)` pattern to prevent CancellationTokenSource memory leaks / `LampArrayController` 鐜板湪閫氳繃 `Dispose(bool)` 妯″紡姝ｇ‘閲婃斁 `_renderCts` �?`_screenCaptureCts`锛岄槻姝?CancellationTokenSource 鍐呭瓨娉勬紡
- All 16 bare `SemaphoreSlim.WaitAsync()` calls now pass a `CancellationToken` to prevent indefinite blocking / 鎵€�?16 涓�?`SemaphoreSlim.WaitAsync()` 璋冪敤鐜板湪浼犻�?`CancellationToken`锛岄槻姝㈡棤闄愭湡闃诲
- Added `volatile` to double-checked locking backing fields in `PluginHostContext._resolved` and `HttpClientManager._sharedClient` to prevent torn reads on 32-bit targets / �?`PluginHostContext._resolved` �?`HttpClientManager._sharedClient` 鐨勫弻閲嶆鏌ラ攣瀹氬悗澶囧瓧娈垫坊鍔?`volatile`锛岄槻姝?32 浣嶇洰鏍囦笂鐨勮鍙栨挄�?
- 35 empty `catch` blocks across the codebase now log exceptions at appropriate levels instead of silently swallowing them / 浠ｇ爜搴撲腑 35 涓�?`catch` 鍧楃幇鍦ㄤ互閫傚綋绾у埆璁板綍寮傚父锛屼笉鍐嶉潤榛樺悶�?
- `GodModeSettingsWindow.SetDefaults` converted from `async void` to `async Task` with proper try/catch to prevent unobserved exceptions / `GodModeSettingsWindow.SetDefaults` �?`async void` 杞崲涓?`async Task` 骞舵坊鍔犻€傚綋�?try/catch锛岄槻姝㈡湭瑙傚療寮傚父
- 8 singleton event subscriptions in controls/pages now unsubscribe in `Unloaded` handlers to prevent memory leaks when controls are navigated away: `PluginExtensionsPage`, `SettingsAppearanceControl`, `ResolutionAutomationStepControl`, `RefreshRateAutomationStepControl`, `HDRAutomationStepControl`, `DpiScaleAutomationStepControl`, `DeviceAutomationPipelineTriggerTabItemContent`, `MacroSequenceControl` / 8 涓帶浠?椤甸潰涓殑鍗曚緥浜嬩欢璁㈤槄鐜板湪�?`Unloaded` 澶勭悊绋嬪簭涓彇娑堣闃咃紝闃叉瀵艰埅绂诲紑鏃跺唴瀛樻硠婕忥細`PluginExtensionsPage`銆乣SettingsAppearanceControl`銆乣ResolutionAutomationStepControl`銆乣RefreshRateAutomationStepControl`銆乣HDRAutomationStepControl`銆乣DpiScaleAutomationStepControl`銆乣DeviceAutomationPipelineTriggerTabItemContent`銆乣MacroSequenceControl`
- Hot-path string allocations reduced in `OsdWindowBase` (sensor tick loop, FPS callback) and `SensorsControl` (gauge updates, trend samples) by caching resource suffixes in static readonly fields and replacing interpolated strings with `string.Concat` / 鍑忓�?`OsdWindowBase`锛堜紶鎰熷櫒鍒锋柊寰幆銆丗PS 鍥炶皟锛夊拰 `SensorsControl`锛堜华琛ㄦ洿鏂般€佽秼鍔块噰鏍凤級涓殑鐑矾寰勫瓧绗︿覆鍒嗛厤锛屽皢璧勬簮鍚庣紑缂撳瓨鍒伴潤鎬佸彧璇诲瓧娈靛苟�?`string.Concat` 鏇夸唬鍐呮彃瀛楃涓?
- `EnumToBoolConverter` now caches `Enum.Parse` results in a `ConcurrentDictionary` to avoid per-call allocations; `CollectionSplitConverters` replaced LINQ `Cast().ToList()` with manual enumeration and `Array.Empty<object>()` / `EnumToBoolConverter` 鐜板湪灏?`Enum.Parse` 缁撴灉缂撳瓨�?`ConcurrentDictionary` 涓紝閬垮厤姣忔璋冪敤鍒嗛厤锛沗CollectionSplitConverters` 鐢ㄦ墜鍔ㄦ灇涓惧�?`Array.Empty<object>()` 鏇夸�?LINQ `Cast().ToList()`
- `SpectrumKeyboardBacklightControl`, `PowerModeControl`, `SensorsControl`, `PackageControl` now implement `IDisposable` and dispose their `CancellationTokenSource`, `ThrottleLastDispatcher`, and `Process` fields / `SpectrumKeyboardBacklightControl`銆乣PowerModeControl`銆乣SensorsControl`銆乣PackageControl` 鐜板湪瀹炵�?`IDisposable` 骞堕噴鏀惧叾 `CancellationTokenSource`銆乣ThrottleLastDispatcher` �?`Process` 瀛楁�?
- Dynamic Panel children in `KeyboardBacklightPage`, `OsdSettingsWindow`, `SensorsControl`, `AutomationPipelineControl` now properly cleaned up in `Unloaded` handlers / `KeyboardBacklightPage`銆乣OsdSettingsWindow`銆乣SensorsControl`銆乣AutomationPipelineControl` 涓殑鍔ㄦ€?Panel 瀛愬厓绱犵幇鍦ㄥ�?`Unloaded` 澶勭悊绋嬪簭涓纭竻�?
- 12 `ItemsControl`/`ListView`/`ListBox` elements across the app now have `VirtualizingStackPanel.IsVirtualizing="True"` and `VirtualizationMode="Recycling"` enabled for better scroll performance with large lists / 搴旂敤涓?12 �?`ItemsControl`/`ListView`/`ListBox` 鍏冪礌鐜板湪鍚�?`VirtualizingStackPanel.IsVirtualizing="True"` �?`VirtualizationMode="Recycling"`锛屾彁鍗囧ぇ鍒楄〃婊氬姩鎬ц兘
- Added `.ConfigureAwait(false)` to all `await` calls across Lib (12), WPF Controls (200+), Pages (75), Windows (120), Utils/Extensions/ViewModels (64), CLI/Lib.Automation/Lib.Macro (0 �?already compliant) to reduce thread marshalling overhead / 涓烘墍鏈?`await` 璋冪敤娣诲姞 `.ConfigureAwait(false)`锛圠ib 12 澶勩€乄PF Controls 200+ 澶勩€丳ages 75 澶勩€乄indows 120 澶勩€乁tils/Extensions/ViewModels 64 澶勶級锛屽噺灏戠嚎绋嬬紪鎺掑紑閿�?
- Fixed 25 culture-sensitive string operations across Lib, WPF, and Plugins: added `StringComparison.Ordinal` to `StartsWith`/`EndsWith`/`Contains`/`IndexOf`, changed `ToUpper()`/`ToLower()` to invariant variants to prevent locale-dependent behavior / 淇璺?Lib銆乄PF �?Plugins �?25 澶勬枃鍖栨晱鎰熷瓧绗︿覆鎿嶄綔锛氫负 `StartsWith`/`EndsWith`/`Contains`/`IndexOf` 娣诲�?`StringComparison.Ordinal`锛屽�?`ToUpper()`/`ToLower()` 鏀逛负涓嶅彉閲忓彉浣擄紝闃叉鍖哄煙璁剧疆渚濊禆琛屼负
- Changed 25 `DateTime.Now` to `DateTime.UtcNow` in logging, timestamps, file names, duration calculations, and event metadata across Lib.Plugins, OsdWindowBase, and Tools; kept `DateTime.Now` only for UI display and local-time scheduling logic / �?Lib.Plugins銆丱sdWindowBase �?Tools 涓殑鏃ュ織銆佹椂闂存埑銆佹枃浠跺悕銆佹寔缁椂闂磋绠楀拰浜嬩欢鍏冩暟鎹腑�?25 �?`DateTime.Now` 鏀逛负 `DateTime.UtcNow`锛涗粎鍦?UI 鏄剧ず鍜屾湰鍦版椂闂磋皟搴﹂€昏緫涓繚�?`DateTime.Now`
- `OsdWindowBase`: added `volatile` to `_fpsMonitoringStarted`; brush fields captured in locals before use in `OnFpsDataUpdated` to avoid disposed-object reads from background thread / `OsdWindowBase`锛氫�?`_fpsMonitoringStarted` 娣诲�?`volatile`锛涘�?`OnFpsDataUpdated` 涓皢鐢诲埛瀛楁鎹曡幏鍒板眬閮ㄥ彉閲忥紝閬垮厤鍚庡彴绾跨▼璇诲彇宸查噴鏀惧�?
- `UpdateWindow` now disposes `CancellationTokenSource` after cancellation and on `Closing` to prevent CTS memory leak / `UpdateWindow` 鍦ㄥ彇娑堝悗�?`Closing` 鏃堕噴鏀?`CancellationTokenSource`锛岄槻姝?CTS 鍐呭瓨娉勬紡
- `SensorsControl.FormatCpuPowerBreakdown` now reuses a static `List<string>` via `Clear()` instead of allocating a new list on every call / `SensorsControl.FormatCpuPowerBreakdown` 鏀逛负澶嶇敤闈欐€?`List<string>`锛岄€氳�?`Clear()` 閬垮厤姣忔璋冪敤鍒嗛厤鏂板垪琛?
- `IFeature<T>` interface and all abstract base classes (`AbstractWmiFeature`, `AbstractCapabilityFeature`, `AbstractDriverFeature`, `AbstractUEFIFeature`, `AbstractLenovoLightingFeature`, `AbstractCompositeFeature`) now accept a `CancellationToken` on every async method, and a new `InvalidateResolution()` method lets callers force composite features to re-resolve their delegate implementation on demand / `IFeature<T>` 鎺ュ彛鍙婃墍鏈夋娊璞″熀绫伙紙`AbstractWmiFeature`銆乣AbstractCapabilityFeature`銆乣AbstractDriverFeature`銆乣AbstractUEFIFeature`銆乣AbstractLenovoLightingFeature`銆乣AbstractCompositeFeature`锛夌殑寮傛鏂规硶鐜板湪鎺ュ�?`CancellationToken`锛屾柊澧炵殑 `InvalidateResolution()` 鏂规硶鍏佽璋冪敤鏂规寜闇€寮哄埗澶嶅悎鐗规€ч噸鏂拌В鏋愬叾濮旀墭瀹炵�?
- Plugin lifecycle is now driven by an explicit `PluginLifecycleStateMachine`; the legal transitions between `NotInstalled` �?`Installed` �?`Enabled`/`Disabled` are enforced centrally and illegal attempts (e.g. `NotInstalled` �?`Enabled`, or `Error` �?`Enabled`) are logged and rejected. A richer `LifecycleStateChanged` event exposing old/new `PluginState` is now available alongside the existing boolean `PluginStateChanged` event / 鎻掍欢鐢熷懡鍛ㄦ湡鐜板湪鐢辨樉寮忕殑 `PluginLifecycleStateMachine` 鐘舵€佹満椹卞姩锛宍NotInstalled` �?`Installed` �?`Enabled`/`Disabled` 涔嬮棿鐨勫悎娉曡浆鎹㈣闆嗕腑寮哄埗鎵ц锛岄潪娉曞皾璇曪紙濡?`NotInstalled` �?`Enabled`锛屾�?`Error` �?`Enabled`锛変細琚褰曞苟鎷掔粷銆傞櫎鍘熸湁甯冨皵鍨?`PluginStateChanged` 浜嬩欢澶栵紝杩樻彁渚涙毚闇叉�?�?`PluginState` �?`LifecycleStateChanged` 浜嬩�?
- `SDK_BOUNDARY.md` now documents which types in `UniversalDeviceToolkit.Lib.Plugins` are part of the public plugin SDK and which are host-internal; plugins must not reference host-internal types at compile time / `SDK_BOUNDARY.md` 鐜板凡鏄庣‘璁板綍 `UniversalDeviceToolkit.Lib.Plugins` 涓摢浜涚被鍨嬪睘浜庡叕寮€鎻掍�?SDK锛屽摢浜涙槸瀹夸富鏈哄唴閮ㄧ被鍨嬶紱鎻掍欢鍦ㄧ紪璇戞椂涓嶅緱寮曠敤瀹夸富鏈哄唴閮ㄧ被鍨?

### Fixed / 淇�?

- Fixed Plugin Extensions action buttons rendering as empty squares when only icons were shown; icons now use the WPF-UI `Icon` property with proper sizing / 淇鎻掍欢鎵╁睍鎿嶄綔鎸夐挳鍦ㄤ粎鏄剧ず鍥炬爣鏃舵覆鏌撲负绌虹櫧鏂规鐨勯棶棰橈紝鏀圭敤 WPF-UI `Icon` 灞炴€у苟璋冩暣灏哄
- `MessagingCenter.Publish<T>` no longer lets one bad subscriber's exception prevent other subscribers from receiving the message; failures are now logged at trace level / `MessagingCenter.Publish<T>` 涓嶅啀鍥犲崟涓闃呰€呮姏鍑哄紓甯歌€岄樆鏂叾浠栬闃呰€咃紝澶辫触鏃舵敼涓轰�?trace 绾у埆璁板綍鏃ュ織
- `ExternalDetectionRule` now validates the download host against a Lenovo allowlist, uses a safer command splitter, and refuses to launch missing executables so the external detection path can no longer pull from arbitrary hosts or run absent binaries / `ExternalDetectionRule` 鐜板涓嬭浇涓绘満鍚嶅仛鐧藉悕鍗曟牎楠屻€佹敼鐢ㄦ洿瀹夊叏鐨勫懡浠ゅ垏鍒嗘柟寮忥紝骞跺湪鍙墽琛屾枃浠剁己澶辨椂鎷掔粷鎵ц锛岄伩鍏嶅閮ㄦ娴嬫祦绋嬩粠浠绘剰鏉ユ簮涓嬭浇鎴栬繍琛屼笉瀛樺湪鐨勪簩杩涘�?

### Security / 瀹夊�?

- IPC named pipe now requires per-message HMAC-SHA256 authentication via challenge-response handshake, preventing unauthorized same-user processes from sending commands / IPC 鍛藉悕绠￠亾鐜板湪瑕佹眰閫氳繃璐ㄨ-鍝嶅簲鎻℃墜杩涜姣忔秷�?HMAC-SHA256 韬唤楠岃瘉锛岄槻姝㈡湭缁忔巿鏉冪殑鍚岀敤鎴疯繘绋嬪彂閫佸懡�?
- Plugin EXE entry points now require Authenticode signature validation before launch (debug builds allow unsigned override) / 鎻掍�?EXE 鍏ュ彛鐐圭幇鍦ㄩ渶瑕佺粡�?Authenticode 绛惧悕楠岃瘉鍚庢墠鑳藉惎鍔紙璋冭瘯鐗堟湰鍏佽鏈鍚嶈鐩栵�?
- Added trusted repository owner allowlist to UpdateChecker; custom repo owners from settings are ignored in release builds / UpdateChecker 娣诲姞浜嗗彈淇′换浠撳簱鎵€鏈夎€呯櫧鍚嶅崟锛涘彂甯冪増鏈拷鐣ヨ缃腑鐨勮嚜瀹氫箟浠撳簱
- SHA256 integrity check is now enforced for update packages; validation is no longer skipped when hash is missing / 鏇存柊鍖呯幇鍦ㄥ己鍒惰繘�?SHA256 瀹屾暣鎬ф牎楠岋紝鍝堝笇缂哄け鏃朵笉鍐嶈烦杩囬獙璇?
- Plugin store cache (`plugin-store-cache.json`) now uses HMAC-SHA256 integrity verification to prevent tampering / 鎻掍欢鍟嗗簵缂撳瓨鐜板湪浣跨�?HMAC-SHA256 瀹屾暣鎬ч獙璇侀槻姝㈢鏀?
- Trusted plugin package store (`trusted-plugin-packages.json`) now uses DPAPI encryption + HMAC integrity / 鍙椾俊浠绘彃浠跺寘瀛樺偍鐜板湪浣跨�?DPAPI 鍔犲�?+ HMAC 瀹屾暣鎬т繚鎶?
- TrustedPluginPackageStore HMAC key derivation strengthened: uses Windows SID binary (not MachineName string) with 16-byte random salt and PBKDF2 (100k iterations) / TrustedPluginPackageStore HMAC 瀵嗛挜娲剧敓澧炲己锛氫娇�?Windows SID 浜岃繘鍒讹紙鑰岄�?MachineName 瀛楃涓诧級�?6 瀛楄妭闅忔満鐩愬�?PBKDF2�?0 涓囨杩唬�?
- Download URL allowlist restricts plugin downloads to known trusted hosts (github.com, jsdelivr.net); file:// URLs rejected in production / 涓嬭�?URL 鐧藉悕鍗曚粎鍏佽鍙椾俊浠讳富鏈猴紱鐢熶骇鐜鎷掔�?file:// URL
- UpdateChecker enforces SHA256 validation; missing hashes now throw in release builds / UpdateChecker 寮哄�?SHA256 鏍￠獙锛岀己澶卞搱甯屾椂鍦ㄥ彂甯冪増鏈腑鎶涘嚭寮傚父

### Fixed / 淇�?

- Registry hive string keys no longer contain trailing spaces, fixing lookups for `HKEY_CLASSES_ROOT` and `HKEY_CURRENT_CONFIG` from the WPF host / Registry hive 瀛楃涓查敭涓嶅啀鍖呭惈灏鹃殢绌烘牸锛屼慨�?WPF 瀹夸富涓 `HKEY_CLASSES_ROOT` �?`HKEY_CURRENT_CONFIG` 鐨勬煡璇?
- `SpectrumKeyboardBacklightController.GetDeviceHandleAsync` now retries up to 3 times (was 1) and disposes the previous `_deviceHandle` before reassignment, preventing the leaked handle on re-open / `SpectrumKeyboardBacklightController.GetDeviceHandleAsync` 閲嶈瘯娆℃暟�?1 鎻愬崌鍒?3锛屽苟鍦ㄩ噸鏂板垎閰嶅墠閲婃斁鏃?`_deviceHandle`锛岄伩鍏嶉噸鏂版墦寮€鏃跺彞鏌勬硠婕?
- `Registry.ObserveKey` and `DriverKeyListener` now wrap their `ManualResetEvent` in `using` to ensure native handle disposal on listener shutdown / `Registry.ObserveKey` �?`DriverKeyListener` 鐜板湪灏?`ManualResetEvent` �?`using` 鍖呰９锛岀‘淇濈洃鍚叧闂椂閲婃斁鍘熺敓鍙ユ�?
- `NotifyIcon` now tracks the HICON handed to the shell and calls `DestroyIcon` when the icon is replaced or the tray icon is disposed, fixing a tray-icon HICON leak / `NotifyIcon` 鐜板湪璺熻釜鎻愪氦缁?Shell �?HICON锛屽苟鍦ㄥ浘鏍囨浛鎹㈡垨鎵樼洏鍥炬爣閲婃斁鏃惰皟�?`DestroyIcon`锛屼慨澶嶆墭鐩樺浘鏍?HICON 娉勬�?
- `GPUController` now exposes the process list as `IReadOnlyList<Process>` and rebuilds it on every refresh, eliminating the background-thread race when the UI reads the list / `GPUController` 鐜颁�?`IReadOnlyList<Process>` 鏆撮湶杩涚▼鍒楄〃骞舵瘡娆″埛鏂伴噸寤猴紝娑堥�?UI 璇诲彇鏃朵笌鍚庡彴绾跨▼鐨勭珵鎬?
- Replaced `.ConfigureAwait(true)` with `.ConfigureAwait(false)` in `FpsSensorController`, `WindowsOptimizationPage`, `PluginExtensionsPage`, `StartupDeviceSetupCoordinator`, and `SettingsApplicationBehaviorControl` to avoid deadlocks and respect the project async guideline / �?`FpsSensorController`銆乣WindowsOptimizationPage`銆乣PluginExtensionsPage`銆乣StartupDeviceSetupCoordinator`銆乣SettingsApplicationBehaviorControl` 涓�?`.ConfigureAwait(true)` 鏇挎崲涓?`.ConfigureAwait(false)`锛岄伩鍏嶆閿佸苟閬靛惊椤圭洰寮傛瑙勮�?
- `WindowsOptimizationPage` driver packages now subscribe to `PropertyChanged` via a named method and unsubscribe in the `Unloaded` handler, preventing lambda-based event-handler leaks / `WindowsOptimizationPage` 椹卞姩鍖呮敼鐢ㄥ叿鍚嶆柟娉曡闃?`PropertyChanged` 骞跺�?`Unloaded` 澶勭悊鍣ㄤ腑鍙栨秷璁㈤槄锛岄伩鍏嶅熀�?lambda 鐨勪簨浠跺鐞嗗櫒娉勬紡
- CLI IPC client now uses exponential backoff with jitter (up to 40 attempts, 3s max delay) to handle startup race conditions / CLI IPC 瀹㈡埛绔敼鐢ㄦ寚鏁伴€€閬?+ 鎶栧姩绛栫暐锛堟渶澶?40 娆￠噸璇曪紝3 绉掓渶澶у欢杩燂級澶勭悊鍚姩绔炴€佹潯浠?
- Hardcoded `IsEnabled`/`Visibility` values replaced with data bindings in UpdateWindow, UnsupportedWindow, DeviceInformationWindow, DiscreteGPUControl / UpdateWindow銆乁nsupportedWindow銆丏eviceInformationWindow銆丏iscreteGPUControl 涓‖缂栫爜�?IsEnabled/Visibility 宸叉浛鎹负鏁版嵁缁戝畾
- LargeFilesWindow fully localized with all 15+ hardcoded English strings migrated to resources; AutomationProperties.Name added / LargeFilesWindow 瀹屽叏鏈湴鍖栵紝15+ 涓‖缂栫爜鑻辨枃瀛楃涓插凡杩佺Щ鍒拌祫婧愭枃浠讹紱娣诲姞�?AutomationProperties.Name
- MacroPage and AutomationPage ToggleSwitches now sync state through data binding instead of manual Click handlers / MacroPage �?AutomationPage �?ToggleSwitch 鏀逛负閫氳繃鏁版嵁缁戝畾鍚屾鐘舵�?
- PluginManager.CheckForUpdatesAsync no longer returns empty dictionary; delegates to PluginRepositoryService for real update checking / PluginManager.CheckForUpdatesAsync 涓嶅啀杩斿洖绌哄瓧鍏革紝鏀逛负濮旀墭缁?PluginRepositoryService 杩涜鐪熸鐨勬洿鏂版�?
- Application startup refactored: async void Application_Startup is now a thin dispatcher; startup logic extracted to testable StartupOrchestrator class / 搴旂敤鍚姩閲嶆瀯锛歛sync void Application_Startup 鏀逛负钖勫垎鍙戝櫒锛涘惎鍔ㄩ€昏緫鎻愬彇鍒板彲娴嬭瘯鐨?StartupOrchestrator �?
- Startup orchestrator and single-instance Mutex activation now have dedicated unit tests / 鍚姩缂栨帓鍣ㄥ拰鍗曞疄�?Mutex 婵€娲荤幇鍦ㄦ湁涓撻棬鐨勫崟鍏冩祴璇?
- 170+ hardcoded English exception messages across Lib projects migrated to resource files with ExceptionHelper wrapper / Lib 椤圭洰涓?170+ 涓‖缂栫爜鑻辨枃寮傚父娑堟伅宸茶縼绉诲埌甯?ExceptionHelper 鍖呰鍣ㄧ殑璧勬簮鏂囦欢
- DeviceInformationWindow and DiscreteGPUControl now have proper data binding for Visibility and IsEnabled states / DeviceInformationWindow �?DiscreteGPUControl �?Visibility �?IsEnabled 鐘舵€佺幇鍦ㄦ湁姝ｇ‘鏁版嵁缁戝�?
- UnsupportedWindow countdown button now uses data binding instead of hardcoded IsEnabled / UnsupportedWindow 鍊掕鏃舵寜閽敼鐢ㄦ暟鎹粦瀹氳€岄潪纭紪�?IsEnabled
- Power consumption can now be read reliably; sensor data reads have a 2-second timeout so slow sensors no longer block the UI, and failures fall back to the session cache instead of leaving stale values / 鍔熻€楄鍙栨仮澶嶆甯革紱浼犳劅鍣ㄨ鍙栧姞鍏?2 绉掕秴鏃朵繚鎶わ紝鎱㈤€熶紶鎰熷櫒涓嶅啀闃诲�?UI锛岃鍙栧け璐ユ椂浼氬洖閫€鍒颁細璇濈紦瀛樿€岄潪鏄剧ず杩囨湡鏁版嵁

- Dashboard trend charts no longer restart from scratch when navigating away and back; accumulated history is now restored across page navigation / 鎺у埗鍙拌秼鍔垮浘鍒囨崲椤甸潰鍚庝笉鍐嶉噸鏂颁粠澶存樉绀猴紝绱Н鐨勫巻鍙茶秼鍔挎暟鎹湪椤甸潰瀵艰埅涓緱浠ヤ繚鐣?

- Main window minimum width is wider so the dashboard console no longer compresses sensor cards as tightly at the smallest size / 涓荤獥鍙ｆ渶灏忓搴﹀凡鍔犲锛岄伩鍏嶆帶鍒跺彴鍦ㄦ渶灏忓昂瀵镐笅杩囧害鍘嬬缉浼犳劅鍣ㄥ崱鐗?
- Dashboard sensor cards keep CPU, battery, and GPU on one row at every window size, using a compact small-window mode and a wider chart layout on large screens / 鎺у埗鍙颁紶鎰熷櫒鍗＄墖鍦ㄦ墍鏈夌獥鍙ｅ昂瀵镐笅閮戒繚�?CPU銆佺數姹犮€丟PU 鍚屼竴琛屾樉绀猴紝骞跺湪灏忕獥鍙ｄ娇鐢ㄧ揣鍑戞ā寮忋€佸ぇ灞忎娇鐢ㄦ洿瀹界殑鍥捐〃甯冨眬
- Battery sensor cards now include a live trend chart, and CPU/GPU/battery summary metrics stay on the gauges instead of being duplicated as progress rows / 鐢垫睜浼犳劅鍣ㄥ崱鐗囩幇鍦ㄥ寘鍚疄鏃惰秼鍔垮浘锛孋PU/GPU/鐢垫睜鎽樿鎸囨爣淇濈暀鍦ㄧ幆褰华琛ㄤ笂锛屼笉鍐嶉噸澶嶄负杩涘害鏉¤�?
- Sensor details no longer slow down compact dashboard loading, and unavailable detail rows are hidden instead of filling the console with repeated unavailable values / 浼犳劅鍣ㄨ鎯呬笉鍐嶆嫋鎱㈢揣鍑戞帶鍒跺彴鍔犺浇锛屼笉鍙敤鐨勮鎯呰浼氳闅愯棌锛岄伩鍏嶉噸澶嶆樉绀哄ぇ閲忎笉鍙敤�?
- App typography now adjusts with the active display DPI so dashboard text stays compact on high-scaling screens / 搴旂敤瀛椾綋鐜板湪浼氭牴鎹綋鍓嶆樉绀哄櫒 DPI 璋冩暣锛屽湪楂樼缉鏀惧睆骞曚笂淇濇寔鎺у埗鍙版枃瀛楁洿绱у�?
- Small dashboard windows now open sensor details in a separate dialog on double-click instead of suppressing details / 灏忓昂瀵告帶鍒跺彴鐜板湪鍙屽嚮浼氱敤鐙珛绐楀彛鏄剧ず浼犳劅鍣ㄨ鎯咃紝鑰屼笉鏄洿鎺ョ鐢ㄨ鎯?
- Dashboard trend charts are reset and redrawn each time the dashboard sensors open / 鎺у埗鍙颁紶鎰熷櫒瓒嬪娍鍥剧幇鍦ㄦ瘡娆℃墦寮€閮戒細閲嶇疆骞堕噸鏂扮粯�?
- Dashboard sensor detail hints no longer pop up every time the console is opened / 鎺у埗鍙颁紶鎰熷櫒璇︽儏鎻愮ず涓嶅啀姣忔鎵撳紑鎺у埗鍙版椂寮瑰嚭
- Malformed BIOS package level values are skipped instead of crashing package rules / 鏍煎紡閿欒�?BIOS Level 鍊间細琚烦杩囷紝涓嶅啀瀵艰嚧鍖呰鍒欏穿婧?
- Driver package SHA256 sidecar files in GNU `hash  filename` format are accepted / 椹卞姩鍖呮牎楠屾敮鎸?GNU 椋庢牸鐨?`hash  filename` SHA256  sidecar 鏍煎�?
- Device pack reinstall no longer deletes the existing pack before the replacement move succeeds / 璁惧鍖呴噸瑁呮椂浠呭湪鏇挎崲绉诲姩鎴愬姛鍚庢墠绉婚櫎鏃у寘锛岄伩鍏嶅畨瑁呬腑鏂鑷村凡瀹夎鍖呬涪�?
- One malformed Lenovo Vantage catalog entry no longer prevents listing all packages / 鍗曚釜鏍煎紡閿欒鐨?Lenovo Vantage 鍖呭厓鏁版嵁涓嶅啀瀵艰嚧鏁翠唤鐩綍鍒楄〃澶辫触
- CLI IPC connection retries with backoff while the app is still starting / CLI 鍦ㄥ簲鐢ㄥ惎鍔ㄦ湡闂翠細浠ラ€€閬块噸璇?IPC 杩炴�?

### Improved / 鏀硅繘

- CI PR gate consolidated to `Ci-tests.yml`; coverage artifacts uploaded; High/Critical NuGet vulnerabilities block merges / CI 鍚堝苟鑷?`Ci-tests.yml` 闂ㄧ锛屼笂浼犺鐩栫巼浜х墿锛孒igh/Critical NuGet 婕忔礊灏嗛樆鏂悎骞?
- Main navigation background now matches the outer window surface for a more unified shell / 涓诲鑸儗鏅幇鍦ㄤ笌绐楀彛澶栧洿搴曡壊涓€鑷达紝浣垮簲鐢ㄥ澹宠瑙夋洿缁熶�?

- Aligned English and Chinese README product positioning, compatibility scope, version references (v4.2.1), legacy-identifier table, and contribution policy so "Universal" vs Lenovo-only hardware control is explicit / 缁熶竴涓嫳�?README 鐨勪骇鍝佸畾浣嶃€佸吋瀹硅寖鍥淬€佺増鏈紩鐢紙v4.2.1锛夈€侀仐鐣欐爣璇嗚鏄庝笌璐＄尞鏀跨瓥锛屾槑纭€孶niversal銆嶄笌鑱旀兂涓撶敤纭欢鎺у埗鐨勮竟�?
- Updated winget locale descriptions and draft template to reflect full-control vs basic-mode positioning; removed misleading `vantage` tag / 鏇存�?winget 鎻忚堪涓庤崏绋挎ā鏉夸互鍙嶆槧瀹屾暣鎺у埗涓庡熀纭€妯″紡瀹氫綅锛岀Щ闄ゆ槗璇�?vantage 鏍囩�?
- Marked `Docs/RELEASE_NOTES_4.1.0_DRAFT.md` as historical; current notes live in CHANGELOG / �?4.1.0 鍙戝竷璇存槑鑽夌鏍囪涓哄巻鍙叉枃妗ｏ紝褰撳墠璇存槑浠?CHANGELOG 涓哄�?
- Enabled `RestorePackagesWithLockFile` in `Directory.Build.props` so future restores consume committed `packages.lock.json` files for reproducible builds / �?`Directory.Build.props` 涓惎鐢?`RestorePackagesWithLockFile`锛屼娇鍚庣画杩樺師鍩轰簬宸叉彁浜ょ殑 `packages.lock.json` 瀹炵幇鍙鐜版瀯寤?
- Upgraded `Markdig` 1.2.0 -> 1.3.2 and `Autofac` 9.1.0 -> 9.3.0 in central package management for current fixes and performance improvements / 鍦ㄤ腑澶寘绠＄悊涓皢 `Markdig` �?1.2.0 鍗囪�?1.3.2銆乣Autofac` �?9.1.0 鍗囪�?9.3.0锛岃幏鍙栦笂娓镐慨澶嶄笌鎬ц兘鏀硅繘

## [4.2.1] - 2026-06-07

### Changed / 鍙樻�?

- Redesigned the dashboard home page for large windows: sensor sections now show radial gauges and live CPU/GPU trend charts with a legend, battery capacity is visualized with rings, and the dashboard groups reflow into 1, 2, or 3 columns based on window width so content no longer stretches awkwardly when maximized / 閲嶆柊璁捐澶х獥鍙ｄ笅鐨勪华琛ㄧ洏涓婚〉锛氫紶鎰熷櫒鍖哄煙鏀圭敤鐜舰浠〃鐩樺苟鍔犲叆甯﹀浘渚嬬殑 CPU/GPU 瀹炴椂瓒嬪娍鍥撅紝鐢垫睜瀹归噺浠ョ幆褰㈠浘灞曠ず锛屼华琛ㄧ洏鍒嗙粍浼氭牴鎹獥鍙ｅ搴﹁嚜閫傚簲�?1�? �?3 鍒楋紝鏈€澶у寲鏃跺唴瀹逛笉鍐嶈鎷変几澶辩�?

### Fixed / 淇�?

- Fixed a startup crash when a damaged dashboard settings file contains empty sensor/dashboard groups / 淇浠〃鐩樿缃枃浠舵崯鍧忓苟鍖呭惈绌轰紶鎰熷櫒/浠〃鐩樺垎缁勬椂鐨勫惎鍔ㄥ穿婧?
- Fixed dashboard and console sensor loading so partial CPU/GPU readings stay visible, intermittent missing metrics no longer flash, and subsequent opens reuse cached data in the same app session / 淇浠〃鐩樺拰鎺у埗鍙颁紶鎰熷櫒鍔犺浇锛岄儴�?CPU/GPU 璇绘暟浼氫繚鎸佹樉绀恒€佸伓鍙戠己澶辩殑鎸囨爣涓嶅啀闂儊锛屽苟涓斿悓涓€娆″簲鐢ㄨ繍琛屼腑鐨勫悗缁墦寮€浼氬鐢ㄧ紦瀛樻暟鎹?
- Fixed GPU power readings flickering to unavailable and restored battery current and average temperature display / 淇�?GPU 鍔熻€楄鏁伴棯鐑佷负涓嶅彲鐢ㄧ殑闂锛屽苟鎭㈠鐢垫睜褰撳墠娓╁害鍜屽钩鍧囨俯搴︽樉绀?

## [4.2.0] - 2026-06-04

### Highlights / 閲嶇�?

- Stable release for in-app updates from v4.1.0. Existing installs can check for updates normally and install the Full or Online setup package from GitHub Releases.
- Added a real-time on-screen display (OSD) overlay for hardware metrics, ported from upstream Lenovo Legion Toolkit behavior.
- Rolls up post-4.1.0 fixes for God Mode preset refresh, installed optimization-only plugins, dashboard sensor loading, expanded sensor aliases, shipping-test isolation, real hardware validation guards, and broader 2020+ basic-mode device matching.

### Added / 鏂板�?

- Added on-screen display (OSD) overlay with Panel and Bar styles, configurable metrics, and settings under Application behavior when hardware sensors are enabled / 鏂板灞忓箷鏄剧ず锛圤SD锛夋诞灞傦紝鏀寔 Panel �?Bar 涓ょ鏍峰紡銆佸彲閰嶇疆鎸囨爣锛屽苟鍦ㄥ惎鐢ㄧ‖浠朵紶鎰熷櫒鏃朵簬搴旂敤琛屼负璁剧疆涓彁渚涢厤缃叆�?

### Fixed / 淇�?

- Fixed the dashboard power mode settings gear staying hidden on some Legion machines (including Y9000P 2025) when custom mode is available from hardware / 淇閮ㄥ垎鎷晳鑰呮満鍨嬶紙鍚?Y9000P 2025锛夊湪纭欢宸叉敮鎸佽嚜瀹氫箟妯″紡鏃讹紝浠〃鐩樻€ц兘妯″紡榻胯疆浠嶆棤娉曟墦寮€璁剧疆鐨勯棶棰?

- Fixed optimization-only plugins after install so manifest and convention-provided System Optimization child actions are visible and usable even when the plugin has no standalone page.
- Fixed dashboard CPU/GPU skeleton timing so loading remains visible until renderable live sensor data is available, and kept battery detailed average temperature out of the dashboard.
- Expanded sensor collection by including nested hardware sensors and additional CPU/GPU/platform aliases used by third-party tools.
- Fixed basic-mode device matching for broader 2020+ brand coverage, including regional brands, family-only catalog packs, and gaming subbrand DMI vendors such as ROG, Alienware, OMEN, Predator, AORUS, and ERAZER.
- Tightened shipping payload guards so test, smoke, validation, and tool projects cannot be referenced by or shipped with the main app.
- Added real hardware validation guard coverage for measurable power-mode readback instead of relying only on simulated state changes.
- Multi-platform work remains diagnostics groundwork in 4.x; formal macOS/Linux release assets are deferred to 5.x.

## [4.1.0] - 2026-05-31

### Highlights / 閲嶇�?

- Fixed God Mode preset management so create, rename, delete, and preset switching refresh both the visible picker and the stored state correctly.
- Fixed optimization-only plugins so installed local plugins can contribute System Optimization child actions even when they do not expose a standalone page.
- Expanded sensor coverage and fallback handling with better VRAM, GPU hot-spot, memory temperature, SSD temperature, voltage, and shared-memory GPU readings.
- Fixed dashboard loading timing so skeletons remain visible until the first real content refresh is ready, and removed the battery detailed average temperature field.
- Continued separating hardware-validation and smoke-only behavior out of the shipping app and into standalone tools.

### Improved / 鏀硅繘

- Fixed God Mode preset create, rename, delete, and switching behavior so the UI and persisted state stay in sync.
- Fixed Plugin Extensions optimization-only plugins so their System Optimization child actions load correctly from installed local plugin directories.
- Expanded dashboard and fallback sensor coverage with additional VRAM, memory, SSD, voltage, hot-spot, and memory-temperature readings on more machines.
- Fixed dashboard skeleton timing so loading indicators stay visible until the first real content refresh has completed, and removed the battery detailed average temperature field.
- Continued moving hardware validation and other smoke-only behavior into standalone tools instead of shipping-app entry points.
- Prepared 4.1.0 release metadata, package-manager manifest generation, and local validation so winget/Scoop manifests can be finalized from the tagged release assets.

## [4.0.0] - 2026-05-29

### Highlights / 閲嶇�?

- Universal Device Toolkit is now the stable public name. Existing Lenovo Legion Toolkit installations can upgrade in place while settings, plugins, updater paths, winget, and Scoop compatibility are preserved.
- Added Full and Online release packages. Full includes bundled languages and device support data; Online starts smaller and installs language/device resources from the GitHub Pages catalog through the app flow.
- Plugin Extensions now use plugin-owned metadata and translations for names, descriptions, details, usage guides, and optimization entries.

### Added / 鏂板�?

- Added a first-run language and device setup flow, including online language/device resource installation and progress feedback.
- Added broader basic-mode support for more Lenovo and non-Lenovo PCs, with device matching for common ASUS, Dell, HP, Acer, Xiaomi, Huawei, MECHREVO, Clevo/Tongfang, and generic desktop/laptop profiles.
- Added generic CPU/GPU telemetry fallbacks for basic-mode systems when Lenovo-specific sensor paths are not available.

### Fixed / 淇�?

- Fixed language pack installation so newly selected languages are available after first launch and from Settings.
- Fixed online plugin installation so installed plugins reload correctly, show their available actions, and no longer open to a misleading "No UI" message when a settings page or optimization entry exists.
- Fixed settings-only plugins appearing as empty System Optimization categories with no actions to select.
- Fixed plugin download progress so installs continue across page navigation and queued installs show a clear waiting state.
- Fixed God Mode preset rename, create, delete, and switching persistence.
- Fixed the Device Information window leaving a large blank area when warranty details are unavailable.
- Fixed Dashboard sensor details so double-click expand/collapse works reliably across child controls.
- Fixed startup stability issues around Lenovo WMI feature reads and update banner event binding.

### Improved / 鏀硅繘

- Improved Plugin Extensions with collapsible details, usage guides, better spacing, and online/store metadata that stays separate from main app translations.
- Improved Dashboard sensor cards so available CPU, battery, and GPU readings stay visible instead of disappearing when one backend is unsupported.
- Improved the main left navigation with a smoother collapsed/expanded transition.
- Improved performance mode selection by keeping unsupported modes out of the picker and falling back safely when hardware reports an unavailable state.
- Improved package naming, release notes structure, SHA256 output, GitHub Pages resource catalog descriptions, and legacy installer alias handling for the rename transition.

## [3.8.1] - 2026-05-23

### Added

- Added a neutral catalog-backed device support provider while keeping the legacy Lenovo provider facade for compatibility.
- Expanded built-in and generated device packs for ThinkPad, ThinkCentre, ThinkStation, IdeaCentre, Legion desktop, XiaoXin, Y-series legacy, V-series, Slim, Motorola, ASUS, Dell, HP, Acer, MSI, Microsoft Surface, and generic PC basic mode.
- Added vendor alias matching and additional basic-mode packs for GIGABYTE/AORUS, Razer, Samsung, HUAWEI, Xiaomi/Redmi, HONOR, LG, Framework, Panasonic, Dynabook/Toshiba, Fujitsu, VAIO, MEDION, XMG/SCHENKER, Clevo/Tongfang, and related barebone vendors.

### Changed

- Startup device setup now evaluates recommendations through the injected device-support provider instead of hard-coding the Lenovo singleton.
- Non-Lenovo and unsupported Lenovo systems now keep hardware-specific controls hidden while basic workflows such as plugins, system optimization, language, theme, updates, and logs remain available.
- Device-pack vendor matching now normalizes common BIOS/DMI vendor formatting differences, including punctuation, spacing, casing, diacritics, and common company suffix variants such as Inc./Incorporated, Corp./Corporation, and Ltd./Limited.

## [3.8.0] - 2026-05-20

### Added / 鏂板�?

- Added the Universal Device Toolkit identity layer with new public display names, new update/resource repository defaults, and legacy Lenovo Legion Toolkit names kept for upgrade compatibility.
- Added Lenovo-first device support data for Legion, LOQ, IdeaPad, ThinkBook, YOGA, Lenovo Slim, legacy Lenovo gaming families, and Motorola Lenovo devices, plus a basic mode for unsupported PCs.
- Added a data-only online device-pack manager and GitHub Pages device-pack catalog generation. Device packs are JSON manifests only and reject executable/script content.

### Changed / 鍙樻�?

- Release packaging now produces Full and Online installers/portable ZIPs under UniversalDeviceToolkit names, plus the LenovoLegionToolkit setup alias for the first bridge release.
- Language and device resources now publish through GitHub Pages catalogs instead of separate language-pack GitHub Release assets; Online packages include the base English resources.
- Runtime paths migrate from `%LOCALAPPDATA%\LenovoLegionToolkit` to `%LOCALAPPDATA%\UniversalDeviceToolkit` without deleting the legacy directory.
- Autorun tasks and single-instance guards now use the new name while also handling legacy LenovoLegionToolkit identifiers.

### Improved / 鏀硅繘

- Documentation, release notes, promotion copy, winget drafts, and Scoop guidance now lead with Universal Device Toolkit and plugin-extension positioning while preserving old package identifiers during the transition.
- Update selection now prefers UniversalDeviceToolkit Full assets and uses the new repository defaults, with the LenovoLegionToolkit alias retained for older update/package-manager paths.
- Consolidated release verification around `UniversalDeviceToolkit_vX.Y.Z_SHA256.txt`.

## [3.7.1] - 2026-05-20

### Added / 鏂板�?

- Added app theme style presets with persisted settings: Current Style, Official Cool, Midnight Neon, and Forest Tech.
- Added optional online language-pack install/uninstall support and release packaging for Full, Online, and legacy language-pack assets.

### Fixed / 淇�?

- Hide the advanced Keyboard Backlight navigation and settings entry on devices that do not support Spectrum/RGB keyboard lighting, preventing empty unsupported pages from appearing in normal use.
- Hardened enum display localization so newly added resource keys resolve correctly even before generated resource designer files are refreshed.
- Improved dispatcher, notification, and registry observer async handling to reduce unobserved task failures during startup and UI refresh.

### Improved / 鏀硅繘

- Expanded visual regression smoke options for theme-style screenshots, settings-only captures, and unsupported-hardware navigation checks.
- Updated release automation to build Full and Online installers, portable ZIPs, legacy language-pack ZIPs, and consolidated SHA256 files.
- Cleaned Plugin Extensions dead import code and tightened UI action dispatch paths across dashboard, automation, keyboard, and display controls.

## [3.7.0] - 2026-05-19

### Fixed / 淇�?

- 淇浼犳劅鍣ㄦ暟鎹摼璺細鏍℃�?CPU 鍔熻€楁煡璇笌鍗曚綅鎹㈢畻锛屽苟鍦?Lenovo 娓╁害浼犳劅鍣ㄤ笉鍙敤鏃跺洖閫�?ACPI thermal zone锛屾彁鍗?CPU 娓╁害涓庡姛鑰楁樉绀虹ǔ瀹氭�?/ Fixed sensor data handling by correcting CPU wattage queries and unit normalization and adding ACPI thermal-zone fallback when Lenovo temperature sensors are unavailable.
- 淇鎻掍欢 ZIP 瀹夎涓殑 Zip Slip 璺緞閬嶅巻椋庨櫓锛岄樆姝㈡伓鎰忔彃浠跺寘鍐欏叆鐩爣鐩綍涔嬪�?/ Fixed a Zip Slip path traversal vulnerability during plugin ZIP extraction.
- 淇�?WPF-UI 4 杩佺Щ鍚庣殑澶氬鐣岄潰鍥炲綊锛屽寘鎷爣棰樻爮鎸夐挳銆佸惎鍔ㄨ瑷€閫夋嫨绐楀彛閫忔槑鑳屾櫙銆丄bout 灏忕獥鍙ｆ粴鍔ㄣ€佹彃浠舵墿灞曠粺璁″崱涓庢繁鑹蹭富棰樻枃瀛楀姣?/ Fixed multiple WPF-UI 4 migration regressions across title-bar buttons, the startup language selector, About scrolling, Plugin Extensions summary cards, and dark-theme text contrast.
- 淇鑻ュ共鏈湴鍖栫粏鑺傦紝鍖呮嫭鎸▉璇壀璐存澘鍗犱綅绗﹀拰涓枃浼犳劅鍣ㄩ鐜囧崟浣?`GHz` / Fixed localization issues including the Norwegian clipboard placeholder and the Chinese `GHz` unit label.

### Improved / 鏀硅繘

- 琛ュ�?README 涓嬭浇娓犻亾銆佺ぞ鍖虹淮鎶ゅ�?winget 瀹夎璇存槑锛屽苟鏂板涓枃瀹ｅ彂涓庝笂鏋舵潗鏂欙紝鎻愬崌鍙戝竷浼犳挱涓庡畨瑁呮壙鎺?/ Expanded README download, community-maintenance, and winget guidance and added Chinese launch materials for release promotion.
- 鍗囩骇鎺у埗鍙伴〉棣栨鍔犺浇浣撻獙锛氭帶鍒跺彴棣栭〉鏀逛负鏇磋创杩戞渶缁堝竷灞€鐨勯鏋舵壂鍏夊姩鐢伙紝CLI 绛夊緟涓荤▼搴忓搷搴旀椂涔熸柊澧炰笉姹℃煋鑴氭湰杈撳嚭鐨勫姞杞藉姩�?/ Improved first-load UX with a dashboard skeleton shimmer and a non-polluting CLI loading animation.
- 搴旂敤璁剧疆銆佽嚜鍔ㄥ寲銆丆LI IPC �?Spectrum 閰嶇疆缁熶竴杩佺Щ�?`System.Text.Json`锛屽悓鏃朵繚鎸佹棦鏈夐厤缃枃浠跺吋�?/ Migrated app settings, automation, CLI IPC, and Spectrum profile serialization to `System.Text.Json` while preserving existing file compatibility.
- 琛ラ�?WPF 澶氳瑷€璧勬簮閿苟鏂板 `resx` 瀹¤鑴氭湰锛屾彁鍗囩炕璇戝畬鏁存€у拰鍙戝竷鍓嶆湰鍦板寲鑷鑳藉�?/ Backfilled WPF resource keys and added a `resx` audit script to improve localization completeness and release readiness checks.
- 鎵╁�?2025 �?Lenovo Legion �?LOQ Gen 10 鏈哄瀷璇嗗埆锛岃ˉ�?`15AKP`銆乣15IRX`銆乣16ADR`銆乣16AFR`銆乣17IRX`銆乣18IAX` 鍓嶇紑鏀寔 / Expanded 2025 Lenovo Legion and LOQ Gen 10 model detection.
- 鏀硅繘 CLI 涓庢彃浠跺吋瀹规€э細CLI 鍒囨崲鍒扮ǔ�?`System.CommandLine` API锛屾彃浠朵緷璧栧尯闂存牎楠屽拰鏈煡鐗堟湰灞曠ず鏇村噯纭?/ Improved CLI and plugin compatibility by moving to the stable `System.CommandLine` API and tightening plugin dependency version handling.
- 寮哄寲鍙戝竷閾捐矾涓庝粨搴撶淮鎶わ細瀹夎鍣ㄦ敼涓烘娴?.NET 10 Desktop Runtime锛屽彂甯冨墠鑷姩璺戞祴璇曪紝骞跺悓姝ユ暣鐞嗕緷璧栧拰浠撳簱妯℃�?/ Hardened release infrastructure with .NET 10 Desktop Runtime detection, pre-package tests, dependency updates, and repository maintenance cleanup.

## [3.6.16] - 2026-05-18

### Fixed / 淇�?

- 鎭㈠�?CPU / Battery / GPU 鍗曞崱鐗囦华琛ㄦ澘甯冨眬锛岄粯璁ゆ姌鍙犺缁嗕紶鎰熷櫒琛岋紝骞惰ˉ涓婂弻鍑诲睍寮€鎻愮�?/ Restored the single-card CPU / Battery / GPU dashboard layout with detailed rows collapsed by default and a double-click expansion hint.
- 寮哄寲浼犳劅鍣ㄥ埛鏂颁笌鍥為€€閫昏緫锛屽噺�?Lenovo WMI 鐬椂澶辫触瀵艰嚧鏁村紶鍗＄墖娑堝け鐨勯棶棰?/ Hardened sensor refresh and fallback behavior so transient Lenovo WMI failures are less likely to hide the whole dashboard card.
- 鍦ㄨ繍琛屾椂璇诲彇澶辫触鏃朵繚鐣欏姛鑰楁ā寮忔帶浠跺彲瑙侊紝鍥為€€鍒版渶杩戜竴娆″凡鐭ョ姸鎬佹垨鍧囪　妯″紡锛岃€屼笉鏄洿鎺ュ鎺?/ Kept the power mode selector visible by falling back to the last known or balanced mode when runtime reads fail.
- 淇鍚姩闃舵鏈瀵熶换鍔″紓甯稿拰鎻掍欢瀹夎�?鍔犺浇鍏煎鎬ч棶棰橈紝鍑忓皯鍦ㄧ嚎鐑熸祴鏃剁殑宕╂簝涓庤鎶?/ Fixed startup-time unobserved task failures and tightened plugin install/load compatibility for online smoke coverage.

### Improved / 鏀硅繘

- 鏀剁揣 Plugin Extensions 椤甸潰甯冨眬锛岀Щ闄ゅ浣欑┖鐧介€夋嫨鎬侊紝榛樿閫変腑棣栦釜鎻掍欢锛屽苟琛ュ厖鏇存竻鏅扮殑璇存槑鍜屼娇鐢ㄥ紩�?/ Tightened the Plugin Extensions page layout, removed the redundant empty state, auto-selected the first plugin, and added clearer descriptions and usage guidance.
- 鎵╁睍鍦ㄧ嚎鎻掍欢鐑熸祴瑕嗙洊锛岃ˉ榻愬畨瑁呫€侀厤缃?鎵撳紑涓庡嵏杞介摼璺紝鎻愰珮榛樿鎻掍欢闆嗙殑楠岃瘉鍙潬�?/ Expanded online plugin smoke coverage across install, configure/open, and uninstall flows.
- 鏇存�?3.6.16 鍙戝竷涓庡垎鍙戞枃妗ｏ紝琛ラ綈涓枃瀹ｅ彂鏂囨浠ュ�?winget / Scoop 缁存姢娴佺▼璇存�?/ Updated 3.6.16 release and distribution docs, including Chinese promotion copy and winget/Scoop maintainer workflows.

## [3.6.15] - 2026-04-29

### Fixed / 淇�?

- 淇璁剧疆椤点€侀€€鍑烘祦绋嬨€侀┍鍔ㄤ笅杞戒笌绯荤粺浼樺寲涓殑澶氬绋冲畾鎬ч棶棰橈紝鍖呮嫭缂哄け閰嶇疆鏂囦欢鏃ュ織鍣煶銆�?-disable-update-checker` 绌虹櫧椤点€丷GB 鎵€鏈夋潈璺宠繃璇姤銆佽法绾跨▼鎻愮ず寮傚父锛屼互�?`del` / `rd` 娓呯悊鍛戒护鎵ц澶辫�?/ Fixed stability issues across settings, shutdown, Driver Download, and System Optimization, including noisy missing-config logs, the blank `--disable-update-checker` page, RGB ownership false alarms, cross-thread snackbar errors, and failing `del` / `rd` cleanup actions.
- 淇鎻掍欢鍔犺浇涓庨殧绂荤儫娴嬮摼璺腑鐨勪竴缁勯棶棰橈紝鍖呮�?sidecar DLL 瑙ｆ瀽銆佽繃鏈熷井杞鍚嶅吋瀹广€侀〉闈㈤噸寤哄惊鐜€佸崟瀹炰緥閿佸啿绐併€乣ReleaseMutex` 寮傚父锛屼互鍙婃彃浠跺競鍦哄崱鐗囧埛鏂颁�?`Open` 鍏ュ彛璇垽 / Fixed plugin-loading and isolated-smoke issues including sidecar DLL resolution, expired Microsoft signature handling, page rebuild loops, single-instance conflicts, `ReleaseMutex` errors, marketplace card refresh, and `Open` entry-point misclassification.
- 淇搴旂敤�?IPC 鎴浘璺緞閲嶇�?WPF 娓叉煋妯″紡鐨勯棶棰橈紝闄嶄綆鍏煎妯″紡鍜岃蒋浠舵覆鏌撳満鏅啀娆″嚭鐜扮┖鐧界獥鍙ｇ殑姒傜巼 / Fixed the in-app IPC screenshot path resetting the WPF render mode and reintroducing blank-window issues in compatibility or software-rendering sessions.
- 鏀惧绯荤粺浼樺寲鍛戒护鏍￠獙瑙勫垯锛屽厑璁稿彈鎺х�?stdout / stderr 閲嶅畾鍚戯紝閬垮�?`2>&1` 绛夊悎娉曠墖娈佃璇垽涓烘敞鍏ラ�?/ Relaxed System Optimization command validation so controlled stdout/stderr redirection such as `2>&1` is no longer misclassified as an injection risk.

### Improved / 鏀硅繘

- CLI 鏂板�?`status` 鍛戒护锛屽彲蹇€熸煡鐪嬩富绋嬪簭杩炴帴鐘舵€併€佹洿鏂版鏌ョ鐢ㄧ姸鎬佸拰褰撳墠鏇存柊浠撳簱锛屼究浜庤瘖鏂缃〉闂�?/ Added a `status` CLI command for checking host connectivity, update-check disablement, and the active update repository.
- 椹卞姩涓嬭浇椤甸噸鍋氫负鏇村畬鏁淬€佹洿绱у噾鐨勯槦鍒楀紡浣撻獙锛岃ˉ榻愰槦鍒楃鐞嗐€佺┖鐘舵€併€佹棤缁撴灉銆侀殣钘忛」鎭㈠鍜屽畬鎴愮姸鎬佸睍绀?/ Reworked Driver Download into a more complete and compact queue-based workflow with improved empty, no-result, restore, and completed states.
- 鎻掍欢鎵╁睍椤垫敼涓哄乏渚у垪琛ㄥ姞鍙充晶璇︽儏鐨勭鐞嗗櫒寮忓竷灞€锛岄鏋跺睆涓庢渶缁堝竷灞€瀵归綈锛屽崱鐗囨搷浣溿€佸浘鏍囦笌璇存槑鏇存竻鏅?/ Reworked Plugin Extensions into a manager-style list/detail layout with aligned skeleton states, clearer actions, icons, and descriptions.
- `custom-mouse` �?`shell-integration` 缁熶竴鏀舵暃鍒伴厤缃獥鍙ｅ拰绯荤粺浼樺寲鍒嗙被鍏ュ彛锛岄檷浣庝晶鏍忓垎鏁ｅ�?/ Consolidated `custom-mouse` and `shell-integration` into the settings-window plus System Optimization entry model.
- 绯荤粺浼樺寲椤靛皢鍙充笂瑙掍笌鎵归噺鎿嶄綔杩佸叆宸ュ叿鏍忥紝骞惰ˉ鍏呯ǔ�?AutomationId锛屾彁鍗?UI 鍐掔儫鍜岃緟鍔╁姛鑳藉畾�?/ Moved System Optimization top-right and bulk actions into the toolbar and added stable AutomationIds for UI smoke and accessibility.
- `MainAppPluginUi.Smoke` 鍋氫簡涓€杞郴缁熸€у寮猴紝鍖呮嫭鍔ㄧ敾绛夊緟銆佸脊绐楄瘑鍒笌娑堥櫎銆侀〉闈㈡埅鍥剧储寮曘€�?-watch` / `--theme` / `--screenshots` 绛夊弬鏁帮紝浠ュ強涓荤獥�?璁剧疆绐楀�?IPC 瀵煎嚭鑳藉姏 / Expanded `MainAppPluginUi.Smoke` with animation-aware waits, popup handling, screenshot indexing, watch/theme/screenshot options, and IPC-based window exports.
- 寮哄寲鐪熷疄鎻掍欢瀹夎涓庡湪绾垮競鍦洪獙璇侀摼璺紝鏀硅繘甯傚満鍒锋柊銆佸畨瑁呮垚鍔熷垽瀹氥€佷笅杞介噸璇曘€佷覆琛屽畨瑁呫€侀〉闈㈠氨缁垽鏂拰 `curl.exe` 涓嬭浇鍏滃簳 / Hardened real plugin-install and marketplace validation with better refresh handling, install-success detection, download retries, serial installs, readiness checks, and a `curl.exe` fallback.
- 鎻掍欢璁剧疆瀹夸富澹冲拰鏍峰紡璁剧疆椤靛仛浜嗙函 UI 鎵撶（锛屾斁澶х獥鍙ｃ€佸噺灏戝弻灞傛粴鍔紝骞惰 `shell-integration` / `custom-mouse` 鐨勮缃綋楠屾洿鎺ヨ繎瀹夸富搴旂敤 / Polished the plugin settings host shell and style settings pages with larger windows, less nested scrolling, and host-aligned presentation.
- 鏂板�?`Main App UI Smoke` workflow �?PowerShell runner 鍖呰鑴氭湰锛屽彲鍦ㄧ嫭绔嬩氦浜掑紡 Windows runner 涓婃墽琛岀湡�?UI smoke 骞惰嚜鍔ㄥ洖浼犳埅鍥句笌鏃ュ�?/ Added a dedicated `Main App UI Smoke` workflow and PowerShell runner wrapper for real UI smoke on an interactive Windows runner.

## [3.6.14] - 2026-04-19

### Added / 鏂板�?

- 鏂板璁捐浠ょ墝浣撶郴鍜屾爣鍑嗘寜�?瀛椾綋鏍峰紡锛岀粺涓€闂磋窛銆佸渾瑙掋€佸浘鏍囥€佹寜閽昂瀵稿拰瀛楀彿锛屼负鍚庣�?UI 鏀舵暃鎵撳熀纭€ / Added design tokens plus standard button and typography styles to unify spacing, corners, icon sizes, button sizes, and text scales.

### Improved / 鏀硅繘

- �?Plugin Extensions銆乄indows Optimization銆丆ardHeader銆丄bout銆丮acro銆丼ensors 绛夐〉闈㈢殑灞€閮ㄦ牱寮忔敹鏁涘埌鍏ㄥ眬璁捐浠ょ墝�?Typography锛屾彁鍗囩晫闈竴鑷存€?/ Consolidated local page styles onto shared design tokens and typography across Plugin Extensions, Windows Optimization, CardHeader, About, Macro, Sensors, and related surfaces.
- 鎻掍�?UI 鐑熸祴鍒囨崲鍒版洿鐪熷疄鐨勯殧绂绘矙绠辨祦绋嬶紝鍚屾椂鏀寔鐪熷疄鍦ㄧ嚎瀹夎鍜屾湰�?ZIP 瀵煎叆锛屽苟鍏佽閫氳繃 `LLT_PLUGIN_SIGNATURE_MODE` 鍒囨崲绛惧悕鏍￠獙绛栫暐 / Moved plugin UI smoke to a more realistic isolated-sandbox flow with real online installs, local ZIP imports, and an explicit `LLT_PLUGIN_SIGNATURE_MODE` override.
- 鏀硅繘鎻掍欢甯傚満鐨勫湪绾垮厓鏁版嵁涓庡畨瑁呭寘涓嬭浇鎶楁姈鍔ㄨ兘鍔涳紝鍔犲叆澶氭簮闀滃儚銆侀噸璇曞拰缂撳瓨鍥為€€锛屽噺�?GitHub 杩炴帴閲嶇疆甯︽潵鐨勭┖甯傚満椤靛拰瀹夎澶辫触 / Improved plugin marketplace resilience with mirrored sources, retries, and cached fallback for metadata and package downloads.
- 鍗囩�?GitHub Actions 渚濊禆骞舵竻鐞嗘祴璇曢」鐩腑�?nullable / xUnit analyzer 鍛婅锛岄檷�?CI 鍣０鍜屽悗�?runner 鍗囩骇椋庨櫓 / Upgraded GitHub Actions dependencies and cleaned nullable/xUnit analyzer warnings to reduce CI noise and future runner-upgrade risk.

### Fixed / 淇�?

- 淇椹卞姩鐗规€у啓鍏ユ牎楠屻€丵uick Action 寰幆寮曠敤銆佽繘绋嬬洃鍚櫒缂撳瓨娓呯悊绛夋牳蹇冪ǔ瀹氭€ч棶棰橈紝鍑忓皯璇姤鎴愬姛銆佸崱姝诲拰楂樹簨浠跺帇鍔涗笅鐨勫紓�?/ Fixed core stability issues around driver-feature verification, cyclic Quick Actions, and process-listener cache cleanup.
- 淇�?IPC 鍛藉悕绠￠亾 ACL銆佽缃�?toggle 姘镐箙绂佺敤銆佸叧闂椂鏈€灏忓寲寮傛淇濆瓨绔炰簤锛屼互鍙婂畨瑁呭櫒蹇界暐闈為浂閫€鍑虹爜绛夐棶棰?/ Fixed IPC ACL issues, permanently disabled toggles after errors, close-to-tray save races, and installers falsely reporting success on non-zero exit codes.
- 淇鎻掍欢鍔犺浇鍣ㄧ己�?sidecar 渚濊禆瑙ｆ瀽銆佹彃浠跺競鍦哄け璐ユ洿鏂版畫鐣欏崐澶嶅埗鐩綍銆佷互鍙婇噸鎵珵鎬佸鑷寸殑鐘舵€侀敊涔?/ Fixed plugin-loader sidecar dependency resolution plus marketplace update/install races that could leave half-copied directories or stale UI state.
- 淇�?WMI listener 骞跺彂閲嶅彔銆佽嚜鍔ㄥ寲鈥滅珛鍗宠繍琛屸�?鐙樉鍋滅敤/鍏抽棴鏄剧ず鍣ㄦ寜閽崱姝伙紝浠ュ強鎻掍欢鍟嗗簵璇锋眰澶辫触琚潤榛樿涓虹┖鍒楄〃绛夐棶�?/ Fixed overlapping WMI listener execution, stuck quick-action buttons, and plugin-store failures being silently treated as an empty list.

## [3.6.13] - 2026-04-18

### Added / 鏂板�?

- 涓烘彃浠跺姞杞芥柊澧炲彲閰嶇疆鐨勭鍚嶆牎楠屾ā寮忥紝鍦ㄥ紑鍙戜笌鐢熶骇鍦烘櫙涓嬮兘鑳芥洿鏄庣‘鍦版嫤鎴湭绛惧悕鎻掍�?/ Added configurable signature validation modes for plugin loading so unsigned plugins are blocked more explicitly in both development and production scenarios

### Fixed / 淇�?

- 鎭㈠�?Panel Logo 鐏晥鍔熻兘鍙婂叾瀹夸�?CLI 娉ㄥ唽閾捐矾锛岄伩鍏嶅彈鏀寔璁惧涓婄殑鐩稿叧鎺у埗椤圭己澶辨垨寮曞叆鏋勫缓鍥炲綊 / Restored Panel Logo lighting features and their host/CLI registrations so supported devices no longer lose the controls or hit the related build regression
- 涓轰笅杞界殑鏇存柊鍖呭�?SHA256 瀹屾暣鎬ф牎楠岋紝閬垮厤绡℃敼鍖呭湪瀹夎鍓嶉€氳繃鏍￠�?/ Added SHA256 integrity verification for downloaded update packages so tampered payloads are rejected before installation

### Improved / 鏀硅繘

- 鍙戝竷涓嬭浇鐜板湪鎻愪緵甯︾増鏈彿鐨勫畨瑁呭寘銆佷究�?ZIP �?SHA256 娓呭崟锛屼究浜庢牎楠屻€佸綊妗ｄ笌闂鎺掓煡 / Release downloads now ship with versioned setup, portable ZIP, and SHA256 manifest assets for easier verification, archiving, and support

## [3.6.12] - 2026-03-28

### Fixed / 淇�?

- **Plugin Install State Recovery / 鎻掍欢瀹夎鐘舵€佹仮�?*: Cleared pending-deletion markers during reinstall/update, rejected plugin packages whose normalized `plugin.json` ID does not match the requested store manifest, and blocked incompatible host-version installs before download state is persisted so plugin updates no longer disappear after app exit or land in a false-installed state / 鍦ㄩ噸瑁呬笌鏇存柊鏃舵竻鐞嗗緟鍒犻櫎鏍囪锛屾嫆缁濊鑼冨寲�?`plugin.json` ID 涓庡晢搴楁竻鍗曚笉涓€鑷寸殑鎻掍欢鍖咃紝骞跺湪涓嬭浇鍓嶆嫤鎴涓荤増鏈笉鍏煎鐨勫畨瑁咃紝閬垮厤鎻掍欢鏇存柊鍚庡湪閫€鍑烘椂琚垹鎺夛紝鎴栬繘鍏モ€滄樉绀哄凡瀹夎浣嗗疄闄呬笉鍙敤鈥濈殑閿欒鐘舵�?
- **Plugin Update Accuracy / 鎻掍欢鏇存柊鍑嗙‘鎬?*: Limited marketplace update checks to truly installed plugin IDs and aligned quick-open executable discovery with real plugin directories and metadata-backed locations so update badges and `Open` actions no longer drift when only discoverable/local plugins are present / 灏嗘彃浠跺競鍦烘洿鏂版鏌ユ敹鏁涘埌鐪熸宸插畨瑁呯殑鎻掍欢 ID锛屽苟璁╁揩閫熸墦寮€鍔ㄤ綔鎸夌湡瀹炴彃浠剁洰褰曚笌鍏冩暟鎹畾浣嶅彲鎵ц鏂囦欢锛岄伩鍏嶄粎瀛樺湪鍙彂�?鏈湴鎻掍欢鏃舵洿鏂版彁绀哄拰 `Open` 琛屼负璇姤

## [3.6.11] - 2026-03-27

### Fixed / 淇�?

- **WPF Rendering Compatibility / WPF 娓叉煋鍏煎�?*: Centralized the software-rendering fallback in `RenderingCompatibilityHelper`, applied an opaque window background fallback in `BaseWindow`, and routed app startup render-mode selection through the helper so Remote Desktop / forced-software-rendering sessions no longer show blank Mica/Acrylic windows. Evidence: `C:\Users\96152\.openclaw\workspace\opencode_automation\report\LenovoLegionToolkit\build-rendering-compat.log`, `C:\Users\96152\.openclaw\workspace\opencode_automation\report\LenovoLegionToolkit\rendering-compat.diff` / 灏嗚蒋浠舵覆鏌撳厹搴曢€昏緫闆嗕腑鍒?`RenderingCompatibilityHelper`锛屽�?`BaseWindow` 涓ˉ鍏呬笉閫忔槑鑳屾櫙鍏滃簳锛屽苟璁╁惎鍔ㄩ樁娈电殑娓叉煋妯″紡缁熶竴璧?helper锛岄伩鍏嶈繙绋嬫闈㈡垨寮哄埗杞欢娓叉煋鍦烘櫙�?Mica/Acrylic 绐楀彛绌虹櫧銆傝瘉鎹細`C:\Users\96152\.openclaw\workspace\opencode_automation\report\LenovoLegionToolkit\build-rendering-compat.log`銆乣C:\Users\96152\.openclaw\workspace\opencode_automation\report\LenovoLegionToolkit\rendering-compat.diff`
- **Plugin UI Localization / 鎻掍欢鐣岄潰鏈湴鍖?*: Replaced plugin marketplace summary/capability/author strings with localized resources and added a localized optimization failure format so simplified Chinese UI no longer shows English labels such as `Total Plugins`, `Quick Open`, or `Failed to apply ...` in the plugin workflow / 灏嗘彃浠跺競鍦虹殑鎽樿銆佽兘鍔涙爣绛俱€佷綔鑰呭墠缂€鏀逛负璧勬簮鍖栨湰鍦板寲鏂囨湰锛屽苟琛ュ厖绯荤粺浼樺寲澶辫触娑堟伅妯℃澘锛屼娇绠€浣撲腑鏂囨彃浠舵祦绋嬩腑涓嶅啀鏄剧�?`Total Plugins`銆乣Quick Open`銆乣Failed to apply ...` 绛夎嫳鏂囨爣�?
- **Menu Style Editor Localization / 鑿滃崟鏍峰紡缂栬緫鍣ㄦ湰鍦板�?*: Replaced hard-coded Chinese strings in `MenuStyleSettingsWindow` with resource lookups, localized apply/open error prompts, and aligned the editor with the actual Shell config availability so Chinese and cold-locale runs no longer mix in untranslated text or expose missing-file actions / �?`MenuStyleSettingsWindow` 涓殑纭紪鐮佷腑鏂囨浛鎹负璧勬簮鏌ユ壘锛岃ˉ榻愬簲鐢ㄤ笌鎵撳紑澶辫触鎻愮ず鐨勬湰鍦板寲锛屽苟鎸夊疄�?Shell 閰嶇疆鏂囦欢鍙敤鎬ф帶鍒剁紪杈戝櫒鐘舵€侊紝閬垮厤涓枃鍜屽喎闂ㄨ瑷€杩愯鏃舵贩鍏ユ湭缈昏瘧鏂囨湰鎴栨毚闇叉棤鏁堟枃浠舵搷浣?
- **Plugin Host Localization Refresh / 鎻掍欢瀹夸富鏈湴鍖栧埛�?*: Added a shared plugin-resource culture change event, taught `PluginPageWrapper` and `PluginSettingsWindow` to rebuild plugin UI after culture changes, and localized previously hard-coded plugin host empty states, delete confirmations, snackbars, and ZIP dialog filters so plugin pages/settings inherit language changes more reliably instead of keeping stale text or popping untranslated helper UI / 鏂板鍏变韩鐨勬彃浠惰祫婧愭枃鍖栧彉鏇翠簨浠讹紝�?`PluginPageWrapper` �?`PluginSettingsWindow` 鍦ㄨ瑷€鍙樺寲鍚庨噸寤烘彃浠剁晫闈紝骞舵妸鍘熸湰纭紪鐮佺殑鎻掍欢瀹夸富绌虹姸鎬併€佸垹闄ょ‘璁ゃ€佹彁绀烘潯�?ZIP 鏂囦欢瀵硅瘽妗嗚繃婊ゆ枃鏈祫婧愬寲锛屽噺灏戞彃浠堕�?璁剧疆椤典繚鐣欐棫璇█鎴栧脊鍑烘湭缈昏瘧鐨勮緟鍔╃晫闈?
- **Plugin Marketplace Hidden Copy / 鎻掍欢甯傚満闅愯棌鏂囨**: Routed the remaining install/uninstall/bulk-import fallback snackbars in `PluginExtensionsPage` through resource keys so the plugin marketplace no longer mixes English helper text like install-failed details, dependency uninstall warnings, or `Unknown` import-source placeholders into Chinese mode / �?`PluginExtensionsPage` 涓畫鐣欑殑瀹夎銆佸嵏杞姐€佹壒閲忓鍏ュ厹搴曟彁绀虹粺涓€鏀硅蛋璧勬簮閿紝閬垮厤鎻掍欢甯傚満鍦ㄤ腑鏂囨ā寮忎笅缁х画娣峰叆瀹夎澶辫触璇︽儏銆佷緷璧栧嵏杞借鍛婃�?`Unknown` 杩欑被鑻辨枃鍗犱綅鎻愮ず
- **Hidden Settings Dialog Copy / 闅愯棌璁剧疆寮规鏂囨**: Routed package-download example placeholders in `PackagesPage` and the mirrored `WindowsOptimizationPage` surface through shared resource keys, and localized the Compatibility Check window's manual 鈥淥pen Log�?failure message so hidden host dialogs no longer mix hard-coded English/Chinese helper text / �?`PackagesPage` 鍙婇暅鍍忓埌 `WindowsOptimizationPage` 鐨勫寘涓嬭浇绀轰緥鍗犱綅鏂囨鏀硅蛋鍏变韩璧勬簮閿紝骞舵妸鍏煎鎬ф鏌ョ獥鍙ｉ噷鎵嬪姩鈥滄墦寮€鏃ュ織鈥濆け璐ユ彁绀鸿祫婧愬寲锛岄伩鍏嶅涓婚殣钘忓脊妗嗙户缁贩鍏ョ‖缂栫爜涓嫳鏂囪緟鍔╂枃鏈?
- **Explorer Restart Reliability / Explorer 閲嶅惎鍙潬�?*: Replaced duplicated Explorer restart snippets with a shared helper that waits for Explorer to fully exit, relaunches it via the shell with a `cmd /c start` fallback, uses the Windows `explorer.exe` full path, and verifies the process actually returns so optimization and menu-style apply flows no longer leave the desktop shell closed / 鐢ㄥ叡浜?helper 鏇挎崲閲嶅�?Explorer 閲嶅惎鐗囨锛氱瓑寰?Explorer 瀹屽叏閫€鍑恒€侀€氳�?shell 鏂瑰紡鎷夎捣骞舵彁渚?`cmd /c start` 鍏滃簳銆佷娇�?Windows �?`explorer.exe` 鐨勫叏璺緞銆佸苟鏍￠獙杩涚▼纭疄鎭㈠锛岄伩鍏嶇郴缁熶紭鍖栧拰鑿滃崟鏍峰紡搴旂敤娴佺▼鎶婃闈㈠３鏉€鎺夊悗涓嶈嚜鍔ㄥ洖�?
- **Windows Optimization Locale Drift / 绯荤粺浼樺寲璇█婕傜�?*: Forced `WindowsOptimizationViewModel` category/action resource lookups to use the app's active `Resource.Culture` explicitly, fixing the case where cold locales like `uz-latn-uz` still rendered Shell Integration and optimization-category cards in Chinese because those strings were resolved against the wrong culture during initialization / �?`WindowsOptimizationViewModel` 涓垎绫讳笌鍔ㄤ綔鏂囨鐨勮祫婧愯鍙栨樉寮忎娇鐢ㄥ綋鍓嶅簲鐢ㄧ�?`Resource.Culture`锛屼慨澶?`uz-latn-uz` 杩欑被鍐烽棬璇█涓?Shell Integration 涓庣郴缁熶紭鍖栧垎绫诲崱鐗囦粛鍥炶惤鎴愪腑鏂囩殑闂�?
- **Plugin Smoke Cold-Locale Coverage / 鎻掍欢鍐掔儫鍐烽棬璇█瑕嗙�?*: Taught `MainAppPluginUi.Smoke` to temporarily pre-seed requested plugins into `InstalledExtensions`, restore settings afterward, and fall back to optimization/sidebar verification when marketplace-only settings windows are unavailable, which unblocked `uz-latn-uz` screenshot validation for `custom-mouse` and `network-acceleration` / �?`MainAppPluginUi.Smoke` 鍦ㄨ繍琛屽墠涓存椂鎶婄洰鏍囨彃浠跺啓�?`InstalledExtensions` 骞跺湪缁撴潫鍚庢仮澶嶈缃紝鍚屾椂鍦ㄥ競鍦洪〉璁剧疆绐楀彛涓嶅彲鐢ㄦ椂鍥為€€鍒扮郴缁熶紭鍖栭〉鎴栦晶杈规爮楠岃瘉锛屼粠鑰屾墦�?`custom-mouse` �?`network-acceleration` �?`uz-latn-uz` 涓嬬殑鎴浘楠岃�?
- **English Fallback Chain / 鑻辨枃鍏滃簳�?*: Added a shared `LocalizationHelper.GetStringOrEnglish(...)` path for runtime resource lookups, forcing dynamic UI text to resolve through current language -> parent culture -> English instead of drifting into unrelated cultures such as Chinese; wired it into Windows Optimization, Plugin Extensions, MainWindow, cleanup helpers, and other runtime-only windows so missing keys now fall back to English consistently / 鏂板鍏变韩�?`LocalizationHelper.GetStringOrEnglish(...)` 杩愯鏃惰祫婧愭煡鎵鹃摼锛岃鍔ㄦ�?UI 鏂囨缁熶竴鎸夆€滃綋鍓嶈瑷€ -> 鐖舵枃鍖?-> 鑻辨枃鈥濊В鏋愶紝涓嶅啀婕傚埌涓枃绛夋棤鍏虫枃鍖栵紱骞跺凡鎺ュ叆绯荤粺浼樺寲銆佹彃浠跺競鍦恒€丮ainWindow銆佹竻鐞嗘彁绀哄拰鍏朵粬杩愯鏃剁獥鍙ｏ紝浣跨己閿椂缁熶竴鍥為€€鑻辨枃
- **Unsupported Window Hyperlink Crash / 涓嶅彈鏀寔纭欢绐楀彛瓒呴摼鎺ュ穿婧?*: Split GitHub link constants into XAML-safe string URLs and kept Uri variants for code paths, then switched `UnsupportedWindow` and `AboutPage` hyperlink bindings to the string values so WPF-UI no longer throws `NavigateUri` parse exceptions during compatibility-check startup / �?GitHub 閾炬帴甯搁噺鎷嗗垎涓洪€傚悎 XAML 鐨勫瓧绗︿覆 URL锛屽悓鏃朵繚鐣欎唬鐮佽矾寰勪娇鐢ㄧ殑 `Uri` 鐗堟湰锛屽苟�?`UnsupportedWindow` �?`AboutPage` 鐨勮秴閾炬帴缁戝畾鍒囨崲鍒板瓧绗︿覆鍊硷紝淇鍏煎鎬ф鏌ュ惎鍔ㄦ�?WPF-UI �?`NavigateUri` 鐨勮В鏋愬紓甯?

### Improved / 鏀硅繘

- **ViVeTool Plugin Smoke / ViVeTool 鎻掍欢鍐掔儫**: Normalized runtime fixture IDs for `vive-tool`, switched settings-window discovery to descendant modal scanning, used the Configure-button route when marketplace double-click is not applicable, and added in-process screenshot capture fallback so the host smoke run now reaches the `ViVeTool 璁剧疆` window reliably / 瑙勮�?`vive-tool` �?runtime fixture ID 鏄犲皠锛屽皢璁剧疆绐楁帰娴嬫敼涓?descendant 妯℃€佺獥鍙ｆ壂鎻忥紝鍦ㄥ競鍦哄弻鍑讳笉閫傜敤鏃跺洖閫€�?Configure 鎸夐挳璺緞锛屽苟琛ュ厖杩涚▼鍐呮埅鍥惧厹搴曪紝浣垮涓诲啋鐑熺幇鍦ㄥ彲浠ョǔ瀹氳繘鍏?`ViVeTool 璁剧疆` 绐楀�?
- **Plugin UI Smoke / 鎻掍欢鐣岄潰鍐掔�?*: Updated `MainAppPluginUi.Smoke` to launch the app with `--skip-compat-check`, tolerate missing refresh buttons, and capture optimization-route settings windows so Shell Integration localization and availability fixes can be verified on unsupported hardware / 鏇存�?`MainAppPluginUi.Smoke`锛氬惎鍔ㄤ富绋嬪簭鏃惰嚜鍔ㄩ檮鍔?`--skip-compat-check`銆佸吋瀹圭己澶辩殑鍒锋柊鎸夐挳锛屽苟鏀寔鎴彇浼樺寲璺敱涓嬬殑璁剧疆绐楀彛锛屼究浜庡湪涓嶅彈鏀寔纭欢涓婇獙�?Shell Integration 鐨勬湰鍦板寲涓庡彲鐢ㄦ€т慨澶?
- **Plugin UI Smoke Evidence / 鎻掍欢鐣岄潰鍐掔儫璇佹嵁**: Completed a real end-to-end smoke pass for `shell-integration` through the optimization route, including action toggles and screenshot capture, while recording reproducible failure evidence for `custom-mouse` category discovery and runtime fixture file-lock cleanup during broader plugin-set runs / 宸插熀浜庣湡瀹炶繍琛屽畬�?`shell-integration` 鐨勭郴缁熶紭鍖栬矾鐢辩鍒扮鍐掔儫楠岃瘉锛屽寘鍚姩浣滃垏鎹笌鎴浘鐣欒瘉锛屽悓鏃惰褰曚簡 `custom-mouse` 鍒嗙被瀹氫綅澶辫触浠ュ強鏇村ぇ鎻掍欢闆嗗悎杩愯鏃?fixture 娓呯悊鏂囦欢閿佸畾鐨勫彲澶嶇幇澶辫触璇佹�?
- **Plugin UI Smoke Stability / 鎻掍欢鐣岄潰鍐掔儫绋冲畾�?*: Hardened `MainAppPluginUi.Smoke` against stale UI Automation window handles during plugin settings validation so Shell and Network Acceleration screenshot runs no longer fail on transient `ElementNotAvailableException` cleanup/rebind paths / 鍔犲�?`MainAppPluginUi.Smoke` 鍦ㄦ彃浠惰缃〉楠岃瘉涓�?UI Automation 鍙ユ焺瀹归敊锛岄伩�?Shell �?Network Acceleration 鎴浘楠岃瘉鍥犵灛鏃?`ElementNotAvailableException` 鐨勬竻鐞嗘垨閲嶇粦瀹氳矾寰勮€屽け�?
- **Shell Integration Smoke Coverage / Shell Integration 鍐掔儫瑕嗙洊**: Adjusted `MainAppPluginUi.Smoke` to validate the real optimization-route entry for `shell-integration`, capture the main-window optimization screenshot, and avoid redundant return-to-market cleanup when the plugin was already installed / 璋冩�?`MainAppPluginUi.Smoke`锛屾敼涓洪獙�?`shell-integration` 瀹為檯璧板埌鐨勭郴缁熶紭鍖栧叆鍙ｃ€佹埅鍙栦富绐楀彛浼樺寲椤垫埅鍥撅紝骞跺湪鎻掍欢鏈潵灏卞凡瀹夎鏃惰烦杩囧浣欑殑鍥炴彃浠跺競鍦烘竻鐞嗘祦�?

## [3.6.6] - 2026-03-15

### Fixed / 淇�?

- **Plugin Store Source / 鎻掍欢鍟嗗簵�?*: Removed the failing `main/store.json` probe from the plugin store fetch path and now fetch the published `master/store.json` directly, avoiding the initial 404/timeout hop before plugin downloads / 浠庢彃浠跺晢搴楁媺鍙栬矾寰勪腑绉婚櫎澶辨晥鐨?`main/store.json` 鎺㈡祴锛屾敼涓虹洿鎺ヨ幏鍙栧凡鍙戝竷�?`master/store.json`锛岄伩鍏嶆彃浠朵笅杞藉墠鍏堢粡鍘嗕竴�?404/瓒呮椂璺宠浆
- **Plugin Marketplace / 鎻掍欢甯傚満**: Verified end-to-end download, install, and load of `shell-integration v1.0.4` through the main app plugin system after the store-source correction / 鍦ㄤ慨姝ｅ晢搴楁簮涔嬪悗锛屽凡閫氳繃涓荤▼搴忔彃浠剁郴缁熺鍒扮楠岃瘉 `shell-integration v1.0.4` 鐨勪笅杞姐€佸畨瑁呬笌鍔犺浇娴佺�?

### Improved / 鏀硅繘

- **Plugin Documentation / 鎻掍欢鏂囨�?*: Added README links to the official `UniversalDeviceToolkit-Plugins` repository so plugin source, manifests, and release metadata are discoverable from the main project / �?README 涓ˉ鍏呭畼鏂?`UniversalDeviceToolkit-Plugins` 浠撳簱閾炬帴锛屼究浜庝粠涓婚」鐩洿鎺ユ煡鎵炬彃浠舵簮鐮併€佹竻鍗曚笌鍙戝竷鍏冩暟鎹?

### Fixed / 淇�?

- **Remote Desktop Rendering / 杩滅▼妗岄潰娓叉�?*: Added a software-rendering fallback toggle for RDP/headless sessions to avoid blank UI when no physical display is active / 涓鸿繙绋嬫闈㈡垨鏃犳樉绀哄櫒鍦烘櫙鏂板鈥滆蒋浠舵覆鏌撯€濆紑鍏充笌鍏滃簳绛栫暐锛岄伩鍏嶇晫闈㈢┖鐧?
- **Background Command Execution / 鍚庡彴鍛戒护鎵ц**: Fixed `CMD.RunAsync` fire-and-forget mode to stop redirecting stdout/stderr when `waitForExit` is `false`, preventing background processes with large output from hanging before completion / 淇�?`CMD.RunAsync` �?`waitForExit=false` 鏃剁殑鍚庡彴鎵ц琛屼负锛氫笉鍐嶉噸瀹氬悜浣嗕笉娑堣垂鏍囧噯杈撳�?閿欒锛岄伩鍏嶅ぇ杈撳嚭鍚庡彴杩涚▼鍦ㄥ畬鎴愬墠琚紦鍐插尯闃诲�?
- **Command Injection Guard / 鍛戒护娉ㄥ叆闃叉�?*: Fixed `CMD.ContainsDangerousInput` to scan all `&` occurrences so mixed input like safe redirection followed by command chaining can no longer bypass validation / 淇�?`CMD.ContainsDangerousInput` �?`&` 鐨勫叏閲忔壂鎻忛€昏緫锛岄伩鍏嶁€滃厛瀹夊叏閲嶅畾鍚戝悗鍛戒护鎷兼帴鈥濈殑娣峰悎杈撳叆缁曡繃鏍￠獙
- **CMD Argument Validation / CMD 鍙傛暟鏍￠獙**: Fixed `CMD.ContainsDangerousInput` false positives by allowing escaped ampersands (`^&`) and valid implicit redirection (`>&1`, `>&2`) while still blocking no-space command chaining (e.g. `echo a&whoami`) / 淇�?`CMD.ContainsDangerousInput` 璇垽锛氬湪淇濇寔鎷︽埅鏃犵┖鏍煎懡浠ゆ嫾鎺ワ紙�?`echo a&whoami`锛夌殑鍚屾椂锛屽厑璁稿悎娉曠殑杞箟涓庨噸瀹氬悜鍐欐硶锛坄^&`銆乣>&1`銆乣>&2`�?
- **Command Execution / 鍛戒护鎵ц�?*: Fixed `CMD.RunAsync` output-buffer deadlock by draining standard output/error streams while waiting for process exit (prevents hangs on large output commands such as directory listing) / 淇�?`CMD.RunAsync` 杈撳嚭缂撳啿鍖烘閿侀棶棰橈細鍦ㄧ瓑寰呰繘绋嬮€€鍑烘椂骞惰璇诲彇鏍囧噯杈撳�?閿欒娴侊紙閬垮厤鐩綍閬嶅巻绛夊ぇ杈撳嚭鍛戒护鍗℃锛?
- **Retry Logic / 閲嶈瘯閫昏緫**: Fixed `RetryHelper` to correctly stop and throw `MaximumRetriesReachedException` after reaching retry limit instead of looping indefinitely / 淇�?`RetryHelper` 鍦ㄨ揪鍒伴噸璇曚笂闄愬悗鐨勮涓猴細鐜板湪浼氭纭仠姝㈠苟鎶涘�?`MaximumRetriesReachedException`锛屼笉鍐嶆棤闄愬惊鐜?
- **Power Mode Error Message / 鐢垫簮妯″紡閿欒娑堟伅**: Fixed `PowerModeUnavailableWithoutACException` message to include the blocked power mode for clearer diagnostics / 淇�?`PowerModeUnavailableWithoutACException` 鐨勬秷鎭唴瀹癸紝鍖呭惈琚樆姝㈢殑鐢垫簮妯″紡锛屼究浜庨棶棰樿瘖鏂?
- **Status Tray Popup / 鎵樼洏鐘舵€佸脊�?*: Hide battery discharge/min/max rate rows when running in compatibility mode to avoid showing meaningless `0.00 W` values on unsupported machines / 鍦ㄥ吋瀹规ā寮忎笅闅愯棌鎵樼洏鐘舵€佸脊绐椾腑鐨勭數姹犲厖鏀剧數鍔熺巼銆佹渶灏忓€笺€佹渶澶у€艰锛岄伩鍏嶅湪涓嶅彈鏀寔璁惧涓婃樉绀烘棤鎰忎箟鐨?`0.00 W`
- **Localization / 鏈湴鍖?*: Fixed plugin-open error localization by removing an invalid `{0}` placeholder from the title resource and unifying plugin open failure message formatting to `PluginExtensionsPage_OpenFailedMessage` / 淇鎻掍欢鎵撳紑澶辫触鎻愮ず鐨勬湰鍦板寲闂锛氱Щ闄ゆ爣棰樿祫婧愪腑鐨勬棤鏁?`{0}` 鍗犱綅绗︼紝骞剁粺涓€浣跨敤 `PluginExtensionsPage_OpenFailedMessage` 浣滀负閿欒娑堟伅妯℃澘
- **Localization / 鏈湴鍖?*: Fixed missing `SettingsPage_Autorun_Message` in base and zh-Hans resources to ensure settings subtitle renders correctly in default and simplified Chinese UI / 淇鍩哄噯涓庣畝浣撲腑鏂囪祫婧愪腑缂哄け鐨?`SettingsPage_Autorun_Message`锛岀‘淇濊缃〉鍓爣棰樺湪榛樿璇█涓庣畝浣撲腑鏂囩晫闈笅姝ｇ‘鏄剧ず
- **Localization / 鏈湴鍖?*: Added missing base resource entries for network optimization action keys used by `WindowsOptimizationCategoryProvider` to ensure fallback localization works outside zh-Hans / 琛ラ�?`WindowsOptimizationCategoryProvider` 浣跨敤鐨勭綉缁滀紭鍖栨搷浣滈敭鐨勫熀鍑嗚祫婧愭潯鐩紝纭繚闈炵畝浣撲腑鏂囩幆澧冧笅鏈湴鍖栧洖閫€姝ｅ�?
- **Localization / 鏈湴鍖?*: Removed stale locale-only resource keys in zh-Hans/zh-Hant/ar that had no code references, and aligned locale files with base keys to reduce translation drift / 娓呯�?zh-Hans/zh-Hant/ar 涓棤浠ｇ爜寮曠敤鐨勯檲鏃ф湰鍦板寲閿紝骞跺皢澶氳瑷€璧勬簮涓庡熀鍑嗛敭瀵归綈锛岄檷浣庣炕璇戞紓�?
- **Localization / 鏈湴鍖?*: Restored base fallback entries for `WindowsOptimizationPage_Extensions_ComingSoon_`* and `PluginExtensionsPage_OpenPluginFailed` to keep `Resource.resx` aligned with generated designer metadata and avoid null fallback strings if reintroduced / 鎭㈠�?`WindowsOptimizationPage_Extensions_ComingSoon_*` �?`PluginExtensionsPage_OpenPluginFailed` 鐨勫熀鍑嗗洖閫€鏉＄洰锛屼�?`Resource.resx` 涓庣敓鎴愮殑璁捐鍣ㄥ厓鏁版嵁淇濇寔涓€鑷达紝閬垮厤鍚庣画閲嶆柊鍚敤鏃跺嚭鐜扮┖鍥為€€鏂囨湰
- **Localization / 鏈湴鍖?*: Improved Chinese translation quality by synchronizing untranslated `zh-Hant` entries from `zh-Hans` with Simplified-to-Traditional conversion and manually localizing high-visibility plugin/menu-style UI strings in both `zh-Hans` and `zh-Hant` / 鎻愬崌涓枃缈昏瘧璐ㄩ噺锛氬�?`zh-Hant` 涓湭缈昏瘧鏉＄洰鍩轰簬 `zh-Hans` 鍚屾骞舵墽琛岀畝杞箒锛屽悓鏃跺 `zh-Hans` �?`zh-Hant` 鐨勯珮鍙鎻掍�?鑿滃崟鏍峰紡鐣岄潰鏂囨杩涜浜哄伐鏈湴鍖栦慨�?
- **Localization / 鏈湴鍖?*: Performed a full 20+ locale semantic translation pass for newly added English UI strings across WPF/Lib/Automation/Macro resources (Bing-backed batching + placeholder-safe restoration), updating 16k+ entries and preserving resource structure integrity (`missing=0`, `extra=0`, `placeholder_mismatch=0`) / �?WPF/Lib/Automation/Macro 璧勬簮鎵ц�?20+ 璇█鍏ㄩ噺璇箟缈昏瘧琛ラ綈锛堝熀�?Bing 鐨勫垎鎵圭炕璇戜笌鍗犱綅绗﹀畨鍏ㄥ洖濉級锛屼慨�?1.6 �? 鏉℃柊澧炶嫳鏂囩晫闈㈡枃妗堬紝骞朵繚鎸佽祫婧愮粨鏋勫畬鏁存€э紙`missing=0`銆乣extra=0`銆乣placeholder_mismatch=0`�?
- **Localization**: Added a follow-up 20+ locale semantic completion pass to translate additional English-identical leftovers (`+63` entries across `25` locale files) while keeping structural audit clean (`missing=0`, `extra=0`, `placeholder_mismatch=0`).
- **Localization**: Continued multi-round semantic localization refinement across 20+ locales with interruption-safe per-locale runs, reducing English-identical residual entries from `1047` to `486` while preserving structural consistency (`missing=0`, `extra=0`, `placeholder_mismatch=0`).
- **Localization**: Performed a second continuation wave with locale-specific provider routing and Portuguese mapping compatibility (`pt`), reducing residual English-identical entries from `486` to `291` while keeping structural audits fully clean (`missing=0`, `extra=0`, `placeholder_mismatch=0`).

### Improved / 鏀硅繘

- **Test Stability / 娴嬭瘯绋冲畾�?*: Replaced the `CMD.RunAsync` cancellation test command from `timeout` to a deterministic `ping`-based long-running command to avoid environment-dependent false negatives in headless runs / �?`CMD.RunAsync` 鍙栨秷娴嬭瘯涓�?`timeout` 鍛戒护鏇挎崲涓烘洿绋冲畾�?`ping` 闀胯€楁椂鍛戒护锛岄伩鍏嶆棤鐣岄潰鐜涓嬬殑鐜鐩稿叧璇姤澶辫触
- **Smoke Evidence / 鍐掔儫璇佹嵁**: Captured latest WPF smoke log at `attachments/lenovo-legion-toolkit/wpf-smoke-latest.log` for verification traceability / 璁板綍鏈€鏂?WPF 鍐掔儫鏃ュ織锛坄attachments/lenovo-legion-toolkit/wpf-smoke-latest.log`锛夌敤浜庨獙璇佺暀�?
- **Localization Workflow / 鏈湴鍖栨祦�?*: Replaced legacy single-file Crowdin mapping with a repository-wide `crowdin.yml` that covers WPF/Lib/Automation/Macro resource modules and locale naming mappings (`zh-hans`, `zh-hant`, `pt-br`, `nl-nl`, `uz-latn-uz`) / 灏嗘棫鐨勫崟鏂囦�?Crowdin 鏄犲皠鍗囩骇涓轰粨搴撶骇 `crowdin.yml`锛岃鐩?WPF/Lib/Automation/Macro 鍥涗釜璧勬簮妯″潡锛屽苟琛ラ綈 `zh-hans`銆乣zh-hant`銆乣pt-br`銆乣nl-nl`銆乣uz-latn-uz` 绛夎瑷€鍛藉悕鏄犲�?
- **Documentation / 鏂囨�?*: Updated README and Docs set to align with current repository links, workflow files, release examples, and translation synchronization commands / 鏇存�?README �?Docs 鏂囨。闆嗭紝浣垮叾涓庡綋鍓嶄粨搴撻摼鎺ャ€佸伐浣滄祦鏂囦欢銆佸彂甯冪ず渚嬪強缈昏瘧鍚屾鍛戒护淇濇寔涓€�?
- **Documentation / 鏂囨�?*: Added a WPF smoke build shortcut to deployment docs to highlight `scripts/smoke-build.ps1` / 鍦ㄩ儴缃叉枃妗ｄ腑琛ュ厖 WPF 鍐掔儫鏋勫缓蹇嵎鍛戒护锛岃鏄?`scripts/smoke-build.ps1` 鐨勪娇鐢ㄦ柟�?
- **Documentation / 鏂囨�?*: Documented how to capture smoke build output logs with `Tee-Object` for sharing / 璇存槑濡備綍浣跨�?`Tee-Object` 鎹曡�?WPF 鍐掔儫鏋勫缓杈撳嚭鏃ュ織锛屼究浜庡垎�?
- **Plugin UI Smoke / 鎻掍欢鐣岄潰鍐掔�?*: Stabilized `MainAppPluginUi.Smoke` settings-window automation by switching to descendant modal-window discovery, filtering stale window handles, adding deterministic close-wait logic, and using a configure-button fallback when double-click is flaky; verified end-to-end network plugin settings + feature interactions / 绋冲�?`MainAppPluginUi.Smoke` 鐨勮缃獥鍙ｈ嚜鍔ㄥ寲锛氭敼涓?descendant 妯℃€佺獥鍙ｆ帰娴嬨€佽繃婊ら檲鏃х獥鍙ｅ彞鏌勩€佸姞鍏ョ‘瀹氭€х殑鍏抽棴绛夊緟閫昏緫锛屽苟鍦ㄥ弻鍑讳笉绋冲畾鏃跺洖閫€鍒伴厤缃寜閽紱宸查獙璇佺綉缁滄彃浠惰缃〉涓庡姛鑳介〉绔埌绔氦浜?
- **Plugin Open Routing / 鎻掍欢鎵撳紑璺�?*: Extended plugin marketplace `Open` behavior to include optimization-category plugins, and added category-focused navigation into Windows Optimization for `shell-integration` and `custom-mouse` / 鎵╁睍鎻掍欢甯傚満 `Open` 琛屼负浠ユ敮鎸佺郴缁熶紭鍖栧垎绫绘彃浠讹紝骞朵负 `shell-integration` �?`custom-mouse` 澧炲姞璺宠浆绯荤粺浼樺寲骞跺畾浣嶅垎绫荤殑鑳藉姏

## [3.6.4] - 2026-02-26

### Improved / 鏀硅繘

- **Plugin Marketplace Validation / 鎻掍欢甯傚満楠岃�?*: Extended desktop smoke validation for plugin marketplace interactions (open plugin page, install/uninstall, double-click configuration window) and verified the end-to-end flow against latest plugin runtime fixes / 鎵╁睍鎻掍欢甯傚満妗岄潰鍐掔儫楠岃瘉锛堟墦寮€鎻掍欢椤甸潰銆佸畨瑁?鍗歌浇銆佸弻鍑婚厤缃獥鍙ｏ級锛屽苟鍩轰簬鏈€鏂版彃浠惰繍琛屾椂淇瀹屾垚绔埌绔祦绋嬫牎�?

## [3.6.3] - 2026-02-26

### Improved / 鏀硅繘

- **Plugin Tooling / 鎻掍欢宸ュ叿�?*: Added a standalone plugin completion UI tool in the sibling `UniversalDeviceToolkit-Plugins` repository (`Tools/PluginCompletionUiTool`) for independent visual validation without launching the main app / 鍦ㄥ厔寮熶粨�?`UniversalDeviceToolkit-Plugins` 涓柊澧炵嫭绔嬬殑鎻掍欢瀹屾垚搴﹀彲瑙嗗寲鏍￠獙宸ュ叿锛坄Tools/PluginCompletionUiTool`锛夛紝鏃犻渶鍚姩涓荤▼搴忓嵆鍙繘琛屽彲瑙嗗寲楠岃�?

## [3.6.2] - 2026-02-26

### Fixed / 淇�?

- **Plugin Navigation / 鎻掍欢瀵艰�?*: Fixed sidebar plugin navigation to include only installed plugins that provide `IPluginPage`, preventing empty plugin pages / 淇鎻掍欢渚ц竟鏍忓鑸€昏緫锛氫粎鏄剧ず宸插畨瑁呬笖鎻愪�?`IPluginPage` 鐨勬彃浠讹紝閬垮厤绌虹櫧椤甸�?
- **Plugin Actions / 鎻掍欢鎿嶄綔**: Fixed plugin card action visibility and capability probing by separating feature-page and settings-page detection / 淇鎻掍欢鍗＄墖鎿嶄綔鍙鎬т笌鑳藉姏鎺㈡祴閫昏緫锛屾媶鍒嗏€滃姛鑳介〉鈥濆拰鈥滆缃〉鈥濆垽�?
- **Plugin Settings Host / 鎻掍欢璁剧疆瀹夸�?*: Fixed `PluginSettingsWindow` to support `IPluginPage` settings providers in addition to raw `Page` objects / 淇�?`PluginSettingsWindow` �?`IPluginPage` 璁剧疆鎻愪緵鍣ㄧ殑鏀寔锛堝吋瀹瑰師鏈?`Page` 瀵硅薄锛?
- **Plugin Implementations / 鎻掍欢瀹炵�?*: Fixed official plugin runtime behavior by adding missing UI/settings/optimization capabilities for `custom-mouse`, `network-acceleration`, and `shell-integration` / 淇瀹樻柟鎻掍欢杩愯鏃惰涓猴細涓?`custom-mouse`銆乣network-acceleration`銆乣shell-integration` 琛ラ綈缂哄け�?UI/璁剧�?绯荤粺浼樺寲鎵╁睍鑳藉姏

### Improved / 鏀硅繘

- **Windows Optimization Extensions / 绯荤粺浼樺寲鎵╁�?*: Improved integration flow by surfacing `shell-integration` as a plugin-provided optimization category with executable actions / 鏀硅繘绯荤粺浼樺寲闆嗘垚娴佺▼锛歚shell-integration` 浠ユ彃浠舵墿灞曞垎绫诲舰寮忔彁渚涘彲鎵ц鎿嶄�?

## [3.6.1] - 2026-02-25

### Added / 鏂板�?

- **Dashboard / 鎺у埗�?*: Added Dashboard navigation item preservation in compatibility mode (--skip-compat-check), allowing users to access CPU/GPU/Battery monitoring on unsupported machines / 鍦ㄥ吋瀹规ā寮忥�?-skip-compat-check锛変笅淇濈暀 Dashboard 瀵艰埅椤癸紝鍏佽鐢ㄦ埛鍦ㄤ笉鏀寔鐨勬満鍣ㄤ笂璁块棶 CPU/GPU/鐢垫睜鐩戞帶
- **Plugin Management / 鎻掍欢绠＄悊**: Added one-click bulk install button to install all currently available online plugins / 鏂板鎻掍欢涓€閿畨瑁呮寜閽紝鍙竴娆″畨瑁呭綋鍓嶅湪绾垮彲鐢ㄧ殑鍏ㄩ儴鎻掍欢

### Fixed / 淇�?

- **Plugin Store / 鎻掍欢鍟嗗簵**: Fixed plugin store URLs and file sizes (Crs10259 �?SSC-STUDIO, correct file sizes) / 淇鎻掍欢鍟嗗�?URL 鍜屾枃浠跺ぇ灏忥紙Crs10259 �?SSC-STUDIO锛屾纭殑鏂囦欢澶у皬锛?
- **Localization / 鏈湴鍖?*: Fixed hardcoded "Recommended" text in Windows Optimization view to use localized resource / 淇�?Windows 浼樺寲瑙嗗浘涓‖缂栫爜�?"Recommended" 鏂囨湰锛屾敼涓轰娇鐢ㄦ湰鍦板寲璧勬簮
- **Plugin Configuration / 鎻掍欢閰嶇疆**: Fixed plugin configuration button visibility for plugins exposing `GetSettingsPage` / 淇鎻掍欢閰嶇疆鎸夐挳鍙鎬э紝鏀寔瀹炵�?`GetSettingsPage` 鐨勬彃浠?
- **Plugin Configuration / 鎻掍欢閰嶇疆**: Added double-click behavior on plugin list items to open plugin settings for installed plugins / 涓哄凡瀹夎鎻掍欢鏂板鍒楄〃椤瑰弻鍑绘墦寮€閰嶇疆椤甸潰琛屼负
- **Settings UI / 璁剧疆鐣岄潰**: Fixed inconsistent sidebar shadow rendering across different PCs by replacing the settings navigation selection shadow with a stable highlight-only style / 淇璁剧疆椤典晶杈规爮闃村奖鍦ㄤ笉鍚岀數鑴戜笂鐨勬覆鏌撲笉涓€鑷撮棶棰橈紝鏀逛负鏇寸ǔ瀹氱殑楂樹寒鏍峰�?
- **Settings UI / 璁剧疆鐣岄潰**: Updated the default update repository owner shown in Settings to `SSC-STUDIO` and aligned owner placeholders across languages / 灏嗚缃〉涓洿鏂颁粨搴撴嫢鏈夎€呴粯璁ゆ樉绀烘洿鏂颁�?`SSC-STUDIO`锛屽苟鍚屾澶氳瑷€鍗犱綅�?
- **Plugin Navigation / 鎻掍欢瀵艰�?*: Fixed installed plugin sidebar visibility by including installed system plugins in navigation refresh / 淇宸插畨瑁呮彃浠朵晶杈规爮鍙鎬э紝鍦ㄥ鑸埛鏂颁腑鍖呭惈宸插畨瑁呯郴缁熸彃浠?
- **Plugin Loading / 鎻掍欢鍔犺浇**: Fixed plugin discovery and ZIP installation to support both `LenovoLegionToolkit.Plugins.*.dll` and ID-based DLL names (for example `custom-mouse.dll`) / 淇鎻掍欢鍙戠幇涓?ZIP 瀹夎閫昏緫锛屽吋�?`LenovoLegionToolkit.Plugins.*.dll` 涓庢寜鎻掍欢 ID 鍛藉悕鐨?DLL锛堝�?`custom-mouse.dll`�?
- **Plugin Manifest Compatibility / 鎻掍欢娓呭崟鍏煎鎬?*: Fixed legacy `minLLTVersion` compatibility in host manifest parsing and ecosystem metadata alignment / 淇涓荤▼搴忔竻鍗曡В鏋愬鏃у瓧�?`minLLTVersion` 鐨勫吋瀹癸紝骞跺榻愭彃浠剁敓鎬佸厓鏁版嵁
- **Plugin Download / 鎻掍欢涓嬭浇**: Fixed online install failures on GitHub 404 assets by adding multi-URL retry and local package fallback from existing compiled plugin directories / 淇�?GitHub 璧勬�?404 瀵艰嚧鐨勫湪绾垮畨瑁呭け璐ワ紝鏂板�?URL 閲嶈瘯涓庢湰鍦板凡缂栬瘧鎻掍欢鐩綍鎵撳寘鍥為€€鏈哄埗
- **Plugin Update UX / 鎻掍欢鏇存柊浣撻�?*: Fixed update hint visibility and metadata rendering by showing update info only for installed plugins with real updates, hiding empty release/changelog fields, formatting release date, and enabling changelog URL click-through from the update icon / 淇鎻掍欢鏇存柊鎻愮ず鍙鎬т笌鍏冩暟鎹樉绀洪€昏緫锛氫粎瀵瑰凡瀹夎涓旂‘鏈夋洿鏂扮殑鎻掍欢鏄剧ず鏇存柊鎻愮ず锛岄殣钘忕┖鐨勫彂甯冩棩鏈?鏇存柊鏃ュ織瀛楁锛屾牸寮忓寲鍙戝竷鏃ユ湡锛屽苟鏀寔浠庢洿鏂板浘鏍囩偣鍑昏烦杞洿鏂版棩蹇楅摼鎺?
- **Plugin Icon Color / 鎻掍欢鍥炬爣棰滆�?*: Fixed plugin icon background color instability across app restarts by replacing non-deterministic hash usage and wiring `store.json` `iconBackground` into plugin cards / 淇鎻掍欢鍥炬爣鑳屾櫙鑹查噸鍚悗鍙樺寲涓嶄竴鑷寸殑闂锛氭浛鎹㈤潪纭畾鎬у搱甯屾柟妗堬紝骞跺�?`store.json` �?`iconBackground` 姝ｅ紡鎺ュ叆鎻掍欢鍗＄墖鏄剧�?

### Improved / 鏀硅繘

- **Plugin Store Reliability / 鎻掍欢鍟嗗簵鍙潬鎬?*: Added store metadata fallback fetch order (`main` �?`master`) to reduce branch mismatch failures / 澧炲姞鍟嗗簵鍏冩暟鎹洖閫€鎷夊彇椤哄簭锛坄main` �?`master`锛夛紝闄嶄綆鍒嗘敮涓嶄竴鑷村鑷寸殑澶辫�?

## [3.6.0] - 2026-02-25

### Added / 鏂板�?

- **Plugin System / 鎻掍欢绯荤粺**: Implemented plugin dependency resolution, sandboxing, hot-reload, and event bus system / 瀹炵幇鎻掍欢渚濊禆瑙ｆ瀽銆佹矙绠便€佺儹閲嶈浇鍜屼簨浠舵€荤嚎绯荤�?
- **Plugin System / 鎻掍欢绯荤粺**: Created working plugin examples (CustomMouse, ShellIntegration) with full functionality / 鍒涘缓鍙敤鐨勬彃浠剁ず渚嬶紙CustomMouse銆丼hellIntegration锛夛紝鍔熻兘瀹屾�?
- **Plugin System / 鎻掍欢绯荤粺**: Implemented plugin version checking and update manager with three update strategies / 瀹炵幇鎻掍欢鐗堟湰妫€鏌ュ拰鏇存柊绠＄悊鍣紝鏀寔涓夌鏇存柊绛栫�?
- **Plugin System / 鎻掍欢绯荤粺**: Added plugin configuration management with user preferences / 娣诲姞鎻掍欢閰嶇疆绠＄悊锛屾敮鎸佺敤鎴峰亸濂借�?
- **Plugin System / 鎻掍欢绯荤粺**: Added plugin update settings (check on startup, auto-download, notification, frequency) / 娣诲姞鎻掍欢鏇存柊璁剧疆锛堝惎鍔ㄦ鏌ャ€佽嚜鍔ㄤ笅杞姐€侀€氱煡銆侀鐜囷級
- **Plugin System / 鎻掍欢绯荤粺**: Integrated plugins repository and migrated downloads to releases / 闆嗘垚鎻掍欢浠撳簱骞跺皢涓嬭浇杩佺Щ鍒?releases
- **Internationalization / 鍥介檯鍖?*: Added multilingual support for CustomMouse and ShellIntegration plugins (13 languages) / �?CustomMouse �?ShellIntegration 鎻掍欢娣诲姞澶氳瑷€鏀寔�?3绉嶈瑷€锛?
- **Internationalization / 鍥介檯鍖?*: Migrated hardcoded Chinese text in XAML files to resource files / �?XAML 鏂囦欢涓‖缂栫爜鐨勪腑鏂囨枃鏈縼绉诲埌璧勬簮鏂囦欢
- **Documentation / 鏂囨�?*: Created comprehensive documentation (ARCHITECTURE.md, DEPLOYMENT.md, SECURITY.md, CODE_OF_CONDUCT.md) / 鍒涘缓瀹屾暣鏂囨。锛圓RCHITECTURE.md銆丏EPLOYMENT.md銆丼ECURITY.md銆丆ODE_OF_CONDUCT.md�?
- **Documentation / 鏂囨�?*: Added quick start guide, troubleshooting section to README / �?README 涓坊鍔犲揩閫熷叆闂ㄦ寚鍗椼€佹晠闅滄帓鏌ラ儴鍒?
- **Testing Infrastructure / 娴嬭瘯鍩虹璁炬�?*: Added comprehensive test coverage for PowerModeFeature, BatteryFeature, and plugin features / �?PowerModeFeature銆丅atteryFeature 鍜屾彃浠跺姛鑳芥坊鍔犲叏闈㈡祴璇曡�?

### Improved / 鏀硅繘

- Migrated core projects to target `net10.0-windows` / 灏嗘牳蹇冮」鐩縼绉诲�?`net10.0-windows`
- Implemented Central Package Management (CPM) for centralized NuGet package version management / 浣跨敤涓ぎ鍖呯鐞?(CPM) 闆嗕腑绠＄悊 NuGet 鍖呯増鏈?
- Optimized shutdown performance from 8 seconds to 0.35 seconds (23x faster) / 浼樺寲鍏抽棴鎬ц兘�?8 绉掓彁鍗囧埌 0.35 绉掞紙鎻愬崌23鍊嶏�?
- Reduced plugin shutdown wait time from 500ms to 200ms / 灏嗘彃浠跺叧闂瓑寰呮椂闂翠�?500ms 鍑忓皯鍒?200ms
- Optimized service stop timeout from 8 seconds to 2 seconds / 灏嗘湇鍔″仠姝㈣秴鏃朵粠 8 绉掍紭鍖栧埌 2 �?
- Removed LibreHardwareMonitorLib dependency, simplified CPU voltage reading using WMI / 绉婚�?LibreHardwareMonitorLib 渚濊禆锛屼娇�?WMI 绠€�?CPU 鐢靛帇璇诲彇
- Enhanced plugin management UI with hover effects, author display, and modern "No Results" state / 澧炲己鎻掍欢绠＄�?UI锛氭偓鍋滄晥鏋溿€佷綔鑰呮樉绀恒€佺幇浠ｅ�?鏃犵粨鏋?鐘舵�?
- Added context menu for plugins (open folder, copy ID, uninstall) / 涓烘彃浠舵坊鍔犲彸閿彍鍗曪紙鎵撳紑鏂囦欢澶广€佸�?ID銆佸嵏杞斤級
- Improved search experience with built-in clear button / 鏀硅繘鎼滅储浣撻獙锛屽鍔犳竻闄ゆ寜閽?
- Optimized UI transition animations and enabled high-performance animations by default / 浼樺�?UI 鍒囨崲鍔ㄧ敾锛岄粯璁ゅ惎鐢ㄩ珮鎬ц兘鍔ㄧ敾
- Redesigned plugin item UI with checkmark badge on installed plugins / 閲嶆柊璁捐鎻掍欢鍒楄〃椤?UI锛屽凡瀹夎鎻掍欢鏄剧ず鍕鹃€夎�?

### Fixed / 淇�?

- **Security**: Fixed JSON deserialization vulnerability in AbstractSettings.cs / 淇�?AbstractSettings.cs 涓�?JSON 鍙嶅簭鍒楀寲瀹夊叏婕忔礊
- **Thread Safety**: Fixed race conditions in AbstractSettings.cs and BatteryDischargeRateMonitorService.cs / 淇�?AbstractSettings.cs �?BatteryDischargeRateMonitorService.cs 涓殑绔炴€佹潯�?
- **Memory**: Fixed memory leaks in MainWindow.xaml.cs and implemented proper IDisposable / 淇�?MainWindow.xaml.cs 鍐呭瓨娉勬紡锛屽疄鐜版纭�?IDisposable
- **Plugin System**: Fixed plugin assembly file locking issue enabling updates without restart / 淇鎻掍欢绋嬪簭闆嗘枃浠堕攣瀹氶棶棰橈紝鏀寔鏃犻渶閲嶅惎鏇存�?
- Fixed Snackbar overlap conflict between Plugin Extensions and Windows Optimization / 淇鎻掍欢鎵╁睍鍜岀郴缁熶紭鍖栫晫闈㈢殑 Snackbar 鍐茬�?
- Fixed process residue after exit with forced termination mechanism / 淇閫€鍑哄悗杩涚▼娈嬬暀闂锛屽紩鍏ュ己鍒剁粓姝㈡満�?
- Fixed 404 error when fetching plugins from store / 淇浠庡晢搴楄幏鍙栨彃浠舵椂鐨?404 閿欒�?
- Fixed XamlParseException in Plugin Extensions page / 淇鎻掍欢鎵╁睍椤甸潰�?XamlParseException

## [3.5.1] - 2026-01-29

### Added / 鏂板�?

- Safety confirmation dialog before system cleanup operations / 绯荤粺娓呯悊鎿嶄綔鍓嶇殑瀹夊叏纭寮圭�?
- New cleanup items: App leftovers, Chrome/Edge/Firefox browser cache / 鏂板娓呯悊椤癸細搴旂敤娈嬬暀鏂囦欢銆丆hrome/Edge/Firefox 娴忚鍣ㄧ紦�?
- Registry redundancy cleanup for recent documents and app usage / 娉ㄥ唽琛ㄥ啑浣欓」娓呯悊锛堟渶杩戞枃妗ｃ€佸簲鐢ㄤ娇鐢ㄨ褰曠瓑�?
- Large file scanning in user profile folders with customizable size filters / 鐢ㄦ埛涓汉鏂囦欢澶逛腑鐨勫ぇ鏂囦欢鎵弿鍔熻兘锛屾敮鎸佽嚜瀹氫箟澶у皬绛涢€?
- One-click "Start All" interaction for driver downloads in System Optimization / 绯荤粺浼樺寲椹卞姩涓嬭浇鏂板�?寮€濮嬪畨瑁呭叏�?涓€閿搷浣?

### Improved / 鏀硅繘

- Redesigned icons for "Select Recommended" and "Clear Selection" in Windows Optimization / 閲嶆柊璁捐绯荤粺浼樺寲椤甸�?閫夋嫨鎺ㄨ崘"�?娓呴櫎鍏ㄩ儴"鍥炬�?
- Instant execution mechanism for optimization items upon checking / 绯荤粺浼樺寲椤瑰嬀閫夊悗鍗虫椂鐢熸晥鏈哄埗
- Batched Snackbar notifications for multiple optimization actions / 浼樺寲鎵归噺搴旂敤椤规椂�?Snackbar 娑堟伅鎻愮ず锛屽悎骞舵樉绀?
- Expanded system cleanup algorithm for better efficiency and coverage / 鎵╁睍绯荤粺娓呯悊绠楁硶锛屾彁鍗囨晥鐜囦笌瑕嗙洊鑼冨�?
- Refactored Cleanup UI: Categories are now always visible, and Scan process is more transparent with a progress bar / 閲嶆瀯娓呯悊鐣岄潰锛氶」鐩垪琛ㄥ缁堝彲瑙侊紝鎵弿杩囩▼閰嶅悎杩涘害鏉℃洿閫忔�?
- Enhanced Driver Download UI: Dual-state toggle button (Start/Pause) for better control / 澧炲己椹卞姩涓嬭�?UI锛氶噰鐢?寮€�?鏆傚�?鍙屾€佸垏鎹㈡寜閽紝鎻愬崌鎺у埗浣撻�?
- Enhanced crash reports with memory state and process tree logging / 澧炲己宕╂簝鎶ュ憡锛屽鍔犲唴瀛樼姸鎬佸拰杩涚▼鏍戣�?
- Introduced `ProcessWatcher` for automatic child process lifecycle management / 寮曞�?`ProcessWatcher` 鑷姩绠＄悊瀛愯繘绋嬬敓鍛藉懆鏈?
- Optimized detection and cleanup of residual processes during startup / 浼樺寲鍚姩鏃剁殑娈嬬暀杩涚▼妫€娴嬩笌娓呯�?
- Redesigned plugin item UI with checkmark badge on installed plugins / 閲嶆柊璁捐鎻掍欢鍒楄〃椤?UI锛氬�?宸插畨瑁?鎸夐挳鏇挎崲涓哄浘鏍囦笂鐨勫凡瀹夎瑙掓爣

### Fixed / 淇�?

- Scan button hover visibility issue in Cleanup page / 淇娓呯悊椤甸潰鎵弿鎸夐挳鎮仠鏃剁殑鏄剧ず闂�?
- ShellIntegration plugin compilation errors and namespace conflicts / 淇�?ShellIntegration 鎻掍欢缂栬瘧閿欒鍙婂懡鍚嶇┖闂村啿�?
- Plugin SDK reference issues in ShellIntegration project / 淇�?ShellIntegration 椤圭洰涓殑鎻掍�?SDK 寮曠敤闂�?
- Resolved process residue after UI crash with a forced exit mechanism / 瑙ｅ�?UI 宕╂簝鍚庣殑杩涚▼娈嬬暀闂锛屽紩鍏ュ己鍒堕€€鍑烘満�?

## [3.5.0] - 2026-01-28

### Added / 鏂板�?

- Two new plugins have been added through the extension. For details, please refer to the CHANGELOG.md of each plugin. / 鎻掍欢鎷撳睍鏂板涓や釜鎻掍欢锛岃鎯呰鎻掍欢姣忎釜鎻掍欢鐨凜HANGELOG.md
- Real-time power usage display for CPU and GPU in dashboard / 鎺у埗鍙版柊澧?CPU �?GPU 瀹炴椂鍔熻€楁樉绀?
- Detailed model name display for CPU and GPU / CPU �?GPU 璇︾粏鍨嬪彿鍚嶇О鏄剧�?
- Double-click interaction to toggle sensor details / 鍙屽嚮浼犳劅鍣ㄥ崱鐗囧垏鎹㈣鎯呮樉绀?
- Plugin configuration management with user preferences / 鎻掍欢閰嶇疆绠＄悊锛屾敮鎸佺敤鎴峰亸濂借�?
- Multi-language support for plugin interface / 鎻掍欢鐣岄潰澶氳瑷€鏀寔

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Unified dashboard layout merging battery, CPU, and GPU stats / 缁熶竴鎺у埗鍙板竷灞€锛屽悎骞剁數姹犮€丆PU �?GPU 鐘舵�?
- Enhanced battery status display with progress bars for all metrics / 澧炲己鐢垫睜鐘舵€佹樉绀猴紝绠€鐣ヨ鍥炬墍鏈夋寚鏍囧潎閰嶆湁杩涘害�?
- Optimized progress bar styling and column spacing in sensors dashboard / 浼樺寲浼犳劅鍣ㄦ帶鍒跺彴鐨勮繘搴︽潯鏍峰紡鍜屽垪闂磋�?
- GPU clock display logic (Core clock in main view, Memory clock in details) / GPU 棰戠巼鏄剧ず閫昏緫锛堜富瑙嗗浘鏄剧ず鏍稿績棰戠巼锛岃鎯呮樉绀烘樉瀛橀鐜囷級
- Plugin management interface in Windows Optimization settings / Windows浼樺寲璁剧疆涓殑鎻掍欢绠＄悊鐣岄潰
- Better error handling for plugin configuration operations / 鎻掍欢閰嶇疆鎿嶄綔鐨勬洿濂介敊璇�?
- Plugin extensions list updates when switching to Extensions tab / 鍒囨崲鍒版墿灞曟爣绛鹃〉鏃舵彃浠舵墿灞曞垪琛ㄦ洿鏂?
- Removed beautification-related code from WindowsOptimizationService and WindowsOptimizationPage / 浠嶹indowsOptimizationService鍜學indowsOptimizationPage涓Щ闄ょ編鍖栫浉鍏充唬鐮?
- Organized working directory: removed unused templates, moved shell integration files to plugin directory / 鏁寸悊宸ヤ綔鐩綍锛氬垹闄ゆ湭浣跨敤鐨勬ā鏉匡紝灏哠hell闆嗘垚鏂囦欢绉诲姩鍒版彃浠剁洰褰?
- Refactored shell integration helper usage to instance-based pattern for consistency / 閲嶆瀯Shell闆嗘垚helper浣跨敤涓哄熀浜庡疄渚嬬殑妯″紡浠ョ‘淇濅竴鑷存�?

### Fixed / 淇�?

- Corrected plugin metadata and version information / 淇鎻掍欢鍏冩暟鎹拰鐗堟湰淇℃伅
- System optimization Extensions tab for managing installed plugins / 绯荤粺浼樺寲鎵╁睍鏍囩椤碉紝鐢ㄤ簬绠＄悊宸插畨瑁呯殑鎻掍欢
- Plugin Extension ViewModel for better integration with system optimization / 鎻掍欢鎵╁睍ViewModel锛屾洿濂藉湴涓庣郴缁熼泦�?
- Plugin icon background color mapping for different plugin types / 涓嶅悓鎻掍欢绫诲瀷鐨勫浘鏍囪儗鏅鑹叉槧灏?
- PluginManager TryGetPlugin method for better plugin discovery / PluginManager TryGetPlugin鏂规硶锛屾敼杩涙彃浠跺彂�?
- Removed donate functionality and all related UI components / 绉婚櫎璧炲姪鍔熻兘鍙婃墍鏈夌浉鍏砋I缁勪�?
- Added missing ExtensionsNavButton_Checked event handler for plugin tab navigation / 娣诲姞浜嗙己澶辩殑ExtensionsNavButton_Checked浜嬩欢澶勭悊鍣紝鐢ㄤ簬鎻掍欢鏍囩椤靛鑸?
- Plugin bulk import improvements for compiled plugins (DLL-only packages) / 閽堝缂栬瘧鎻掍欢锛堜粎鍖呭惈DLL鐨勫寘锛夌殑鎵归噺瀵煎叆鏀硅繘

## [3.4.1] - 2026-01-24

### Added / 鏂板�?

- Plugin Stop interface for safe updates and uninstallation / 鎻掍�?Stop 鎺ュ彛锛屾敮鎸佸畨鍏ㄦ洿鏂板拰鍗歌浇
- Debug logging for plugin configuration visibility diagnostics / 鎻掍欢閰嶇疆鍙鎬ц瘖鏂殑璋冭瘯鏃ュ織
- Bulk plugin import functionality / 鎵归噺鎻掍欢瀵煎叆鍔熻兘
- Comprehensive multilingual support for plugin bulk import features / 鎻掍欢鎵归噺瀵煎叆鍔熻兘鐨勫畬鏁村璇█鏀寔
- Plugin icon background color support from store.json / �?store.json 璇诲彇鎻掍欢鍥炬爣鑳屾櫙棰滆壊鏀寔
- Improved make.bat plugin build commands with local test copy option / 鏀硅繘 make.bat 鎻掍欢鏋勫缓鍛戒护锛屾敮鎸佹湰鍦版祴璇曞鍒堕€夐�?

### Fixed / 淇�?

- Plugin update process now stops plugins before updating / 鎻掍欢鏇存柊娴佺▼鐜板湪浼氬湪鏇存柊鍓嶅仠姝㈡彃�?
- Configuration button responsiveness with better error handling / 閰嶇疆鎸夐挳鍝嶅簲鎬у強鏇村ソ鐨勯敊璇�?
- Plugin installation/uninstallation file lock issues / 鎻掍欢瀹夎�?鍗歌浇鏂囦欢閿佸畾闂�?
- PluginManifestAdapter missing Stop() method implementation / PluginManifestAdapter 缂哄�?Stop() 鏂规硶瀹炵�?
- Plugin configuration button appearing for uninstalled plugins (with debug logging) / 鎻掍欢閰嶇疆鎸夐挳鍑虹幇鍦ㄦ湭瀹夎鎻掍欢涓婄殑闂锛堥檮甯﹁皟璇曟棩蹇楋�?
- IsInstalled check now verifies plugin files exist on disk / IsInstalled 妫€鏌ョ幇鍦ㄤ細楠岃瘉鎻掍欢鏂囦欢鏄惁瀛樺湪浜庣�?
- BooleanAndConverter safety improvements for null and non-boolean values / BooleanAndConverter 瀹夊叏鎬ф敼杩涳紝澶勭�?null 鍜岄潪甯冨皵�?
- **Plugin configuration button not responding - completely redesigned implementation** / **鎻掍欢閰嶇疆鎸夐挳鏃犲搷�?- 瀹屽叏閲嶆柊璁捐瀹炵�?*
- **Configuration button visibility logic with HasConfiguration property** / **閰嶇疆鎸夐挳鍙鎬ч€昏緫浣跨敤 HasConfiguration 灞炴�?*
- **PluginViewModel compilation errors after configuration support changes** / **閰嶇疆鏀寔鏇存敼鍚庣殑 PluginViewModel 缂栬瘧閿欒**
- **ViveTool plugin appearing multiple times due to development folder scanning** / **ViveTool鎻掍欢鍥犲紑鍙戞枃浠跺す鎵弿鑰屽娆℃樉绀?*
- **PluginManager.PluginManifestAdapter priority issue - installed plugins showing as online adapters** / **PluginManager.PluginManifestAdapter浼樺厛绾ч棶棰?- 宸插畨瑁呮彃浠舵樉绀轰负鍦ㄧ嚎閫傞厤�?*
- **UI update loop caused by excessive UpdateAllPluginsUI calls** / **UI鏇存柊寰幆鐢辫繃澶氱殑UpdateAllPluginsUI璋冪敤寮曡捣**
- **Plugin icon background colors changing on each app launch** / **鎻掍欢鍥炬爣鑳屾櫙棰滆壊鍦ㄦ瘡娆″簲鐢ㄥ惎鍔ㄦ椂鍙樺�?*
- **XAML tag mismatch error in PluginExtensionsPage** / **PluginExtensionsPage 涓�?XAML 鏍囩涓嶅尮閰嶉敊璇?*
- **Missing translations for plugin snackbar messages** / **鎻掍�?snackbar 娑堟伅缂哄皯缈昏�?*
- **Hardcoded English text in plugin UI elements** / **鎻掍�?UI 鍏冪礌涓殑纭紪鐮佽嫳鏂囨枃鏈?*
- **Resource.Designer.cs missing new plugin resource strings** / **Resource.Designer.cs 缂哄皯鏂扮殑鎻掍欢璧勬簮瀛楃涓?*

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Plugin update reliability with proper resource cleanup / 鎻掍欢鏇存柊鍙潬鎬у強姝ｇ‘鐨勮祫婧愭竻鐞?
- Plugin configuration window error handling / 鎻掍欢閰嶇疆绐楀彛閿欒澶勭�?
- Plugin state tracking and installation status validation / 鎻掍欢鐘舵€佽窡韪拰瀹夎鐘舵€侀獙璇?
- **Configuration button click handling with detailed logging** / **閰嶇疆鎸夐挳鐐瑰嚮澶勭悊鍙婅缁嗘棩蹇楄褰?*
- **Plugin configuration support detection with CheckConfigurationSupport method** / **鎻掍欢閰嶇疆鏀寔妫€娴嬩娇鐢?CheckConfigurationSupport 鏂规�?*
- **Error handling and user feedback for configuration operations** / **閰嶇疆鎿嶄綔鐨勯敊璇鐞嗗拰鐢ㄦ埛鍙嶉�?*
- **Plugin scanning filter to exclude development folders (obj, bin, Debug, Release)** / **鎻掍欢鎵弿杩囨护鍣ㄦ帓闄ゅ紑鍙戞枃浠跺す锛坥bj銆乥in銆丏ebug銆丷elease�?*
- **Plugin merging logic to prioritize installed plugins over online adapters** / **鎻掍欢鍚堝苟閫昏緫浼樺厛閫夋嫨宸插畨瑁呮彃浠惰€岄潪鍦ㄧ嚎閫傞厤�?*
- **Optimized UI update flow to prevent infinite loops** / **浼樺寲UI鏇存柊娴佺▼闃叉鏃犻檺寰�?*
- **Simplified plugin scanning logic - only scan root plugin directories** / **绠€鍖栨彃浠舵壂鎻忛€昏�?- 浠呮壂鎻忔牴鐩綍鐨勬彃浠剁洰褰?*
- **Plugin icon background colors now read from store.json instead of dynamic generation** / **鎻掍欢鍥炬爣鑳屾櫙棰滆壊鐜板湪浠?store.json 璇诲彇锛岃€岄潪鍔ㄦ€佺敓鎴?*
- **ViveTool status display moved to configuration page only** / **ViveTool 鐘舵€佹樉绀轰粎绉昏嚦閰嶇疆椤甸�?*
- **Enhanced make.bat with improved plugin build and local test copy functionality** / **澧炲�?make.bat锛屾敼杩涙彃浠舵瀯寤哄拰鏈湴娴嬭瘯澶嶅埗鍔熻兘**

## [3.4.0] - 2026-01-22

### Added / 鏂板�?

- Bulk plugin import functionality with progress tracking / 鎵归噺鎻掍欢瀵煎叆鍔熻兘鍙婅繘搴﹁窡�?
- Plugin icon background colors in store configuration / 鎻掍欢鍟嗗簵涓浘鏍囪儗鏅鑹查厤�?
- Comprehensive multilingual support for plugins (ja, ko, de, zh-hant) / 鎻掍欢瀹屾暣澶氳瑷€鏀寔锛堟棩璇€侀煩璇€佸痉璇€佺箒浣撲腑鏂囷�?
- ViveTool status display and download functionality in plugin settings / 鎻掍欢璁剧疆涓殑ViveTool鐘舵€佹樉绀哄拰涓嬭浇鍔熻兘
- Plugin localization framework and resource standardization / 鎻掍欢鏈湴鍖栨鏋跺強璧勬簮鏍囧噯鍖?

### Fixed / 淇�?

- Hard-coded strings in NetworkAcceleration plugin XAML files / NetworkAcceleration 鎻掍欢XAML鏂囦欢涓殑纭紪鐮佸瓧绗︿�?
- Plugin configuration button click handler issues / 鎻掍欢閰嶇疆鎸夐挳鐐瑰嚮澶勭悊闂�?
- Missing multilingual resource keys for UI elements / UI鍏冪礌缂哄け鐨勫璇█璧勬簮閿?
- Plugin version synchronization with store metadata / 鎻掍欢鐗堟湰涓庡晢搴楀厓鏁版嵁鍚屾�?
- Missing Resource.Designer.cs entries for bulk import / 鎵归噺瀵煎叆缂哄け�?Resource.Designer.cs 鏉＄�?
- Duplicate resource entries in Resource.resx file / Resource.resx 鏂囦欢涓噸澶嶇殑璧勬簮鏉＄�?
- JSON syntax errors in plugin store preventing online plugin loading / 鎻掍欢鍟嗗簵JSON璇硶閿欒瀵艰嚧鏃犳硶鍔犺浇鍦ㄧ嚎鎻掍�?
- Removed redundant PluginImport plugin from store / 浠庡晢搴椾腑绉婚櫎鍐椾綑鐨凱luginImport鎻掍�?
- Plugin uninstall button not updating UI after successful uninstall / 鎻掍欢鍗歌浇鎴愬姛鍚庡嵏杞芥寜閽甎I鏈洿鏂?
- Plugin descriptions not supporting Chinese localization / 鎻掍欢鎻忚堪涓嶆敮鎸佷腑鏂囨湰鍦板寲
- Bulk import button icon and tooltip unclear / 鎵归噺瀵煎叆鎸夐挳鍥炬爣鍜屾彁绀轰笉娓呮�?
- Implemented proper ZIP file import functionality for local plugins / 瀹炵幇浜嗘湰鍦版彃浠禯IP鏂囦欢鐨勬纭鍏ュ姛�?
- Fixed configure button visibility to require both installed and supports configuration / 淇閰嶇疆鎸夐挳鍙鎬э紝闇€瑕佸悓鏃跺畨瑁呬笖鏀寔閰嶇�?

### Added / 鏂板�?

- Plugin state reset functionality with Ctrl+Shift+R shortcut / 娣诲姞浜嗘彃浠剁姸鎬侀噸缃姛鑳斤紝浣跨敤Ctrl+Shift+R蹇嵎閿?
- Visual tip about plugin state reset shortcut in UI / 鍦║I涓坊鍔犱簡鎻掍欢鐘舵€侀噸缃揩鎹烽敭鐨勮瑙夋彁绀?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- ViveTool plugin interface optimization by removing redundant status display / 浼樺寲ViveTool鎻掍欢鐣岄潰锛岀Щ闄ゅ啑浣欑姸鎬佹樉绀?
- Plugin import workflow with ZIP file validation / 澧炲己鎻掍欢瀵煎叆宸ヤ綔娴佸強ZIP鏂囦欢楠岃瘉
- Plugin store UI with improved icon display and color coding / 鏀硅繘鎻掍欢鍟嗗簵鐣岄潰鍥炬爣鏄剧ず鍜岄鑹茬紪鐮?
- Plugin management error handling and user feedback / 鎻掍欢绠＄悊閿欒澶勭悊鍜岀敤鎴峰弽�?
- Plugin icon text color now adapts to theme (white in dark mode, black in light mode) / 鎻掍欢鍥炬爣鏂囧瓧棰滆壊鐜板湪鏍规嵁涓婚鑷姩閫傞厤锛堟繁鑹叉ā寮忕櫧鑹诧紝浜壊妯″紡榛戣壊锛?

---

## [3.3.0] - 2026-01-XX

### Added / 鏂板�?

- Complete plugin system with online store and GitHub Actions publishing workflow / 瀹屾暣鐨勬彃浠剁郴缁燂紝鍖呭惈鍦ㄧ嚎鍟嗗簵鍜?GitHub Actions 鍙戝竷宸ヤ綔�?
- Network acceleration plugin with traffic statistics and UI improvements / 缃戠粶鍔犻€熸彃浠跺強娴侀噺缁熻鐣岄潰鏀硅繘
- ViveTool plugin integration with v1.3.0 release / ViveTool 鎻掍欢闆嗘垚�?v1.3.0 鍙戝�?
- Plugin SDK for third-party development / 绗笁鏂规彃浠跺紑鍙?SDK

### Fixed / 淇�?

- Plugin installation permission errors and build issues / 鎻掍欢瀹夎鏉冮檺閿欒鍜屾瀯寤洪棶�?
- Compilation errors in plugin extensions and related components / 鎻掍欢鎵╁睍鍙婄浉鍏崇粍浠剁殑缂栬瘧閿欒�?
- Hard-coded strings in NetworkAcceleration plugin XAML files / NetworkAcceleration 鎻掍�?XAML 鏂囦欢涓殑纭紪鐮佸瓧绗︿�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Plugin UI with card-based layout and multilingual support / 鍗＄墖寮忓竷灞€鍜屽璇█鏀寔鐨勬彃浠剁晫�?
- Plugin store with automatic file hash generation / 鑷姩鏂囦欢鍝堝笇鐢熸垚鐨勬彃浠跺晢�?
- Localization consistency across all plugin components / 鎵€鏈夋彃浠剁粍浠剁殑鏈湴鍖栦竴鑷存�?

---

## [3.2.0] - 2026-01-XX

### Added / 鏂板�?

- Plugin auto-update functionality with version checking / 鎻掍欢鑷姩鏇存柊鍔熻兘鍙婄増鏈�?
- Plugin import from compressed files / 浠庡帇缂╂枃浠跺鍏ユ彃�?
- Plugin installation with download progress bar / 甯︿笅杞借繘搴︽潯鐨勬彃浠跺畨瑁?
- Comprehensive plugin multilingual support (ja, ko, de, zh-hant) / 鎻掍欢瀹屾暣澶氳瑷€鏀寔锛堟棩璇€侀煩璇€佸痉璇€佺箒浣撲腑鏂囷�?

### Fixed / 淇�?

- Plugin icon loading logic for installed vs uninstalled plugins / 宸插畨瑁呭拰鏈畨瑁呮彃浠剁殑鍥炬爣鍔犺浇閫昏緫
- Plugin UI layout and interaction issues / 鎻掍欢鐣岄潰甯冨眬鍜屼氦浜掗棶棰?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Plugin details panel with automatic icon generation / 鑷姩鍥炬爣鐢熸垚鐨勬彃浠惰鎯呴潰�?
- Performance optimizations for plugin loading and management / 鎻掍欢鍔犺浇鍜岀鐞嗙殑鎬ц兘浼樺�?
- Plugin resource file organization and maintainability / 鎻掍欢璧勬簮鏂囦欢缁勭粐鎬у拰鍙淮鎶ゆ€?

---

## [3.2.1] - 2026-01-15

### Fixed / 淇�?

- Fixed bugs in settings interface / 淇璁剧疆鐣岄潰涓殑閿欒�?

---

## [3.2.0] - 2026-01-14

### Added / 鏂板�?

- Updated 'Tools' UI with improved layout / 鏇存�?宸ュ�?鐣岄潰锛屾敼杩涘竷灞�?
- Added collection utilities for better data handling / 娣诲姞闆嗗悎宸ュ叿浠ユ敼杩涙暟鎹�?

---

## [3.1.5] - 2026-01-14

### Added / 鏂板�?

- Optimized ViVeTool plugin and updated Settings page navigation style / 浼樺寲ViVeTool鎻掍欢骞舵洿鏂拌缃〉闈㈠鑸牱寮?

---

## [3.1.3] - 2026-01-13

### Fixed / 淇�?

- Version bump only - no actual code changes / 浠呯増鏈彿鍗囩骇 - 鏃犲疄闄呬唬鐮佸彉鏇?

---

## [3.1.2] - 2026-01-12

### Fixed / 淇�?

- Updated tools configuration / 鏇存柊宸ュ叿閰嶇�?

---

## [3.1.1] - 2026-01-12

### Fixed / 淇�?

- Version bump only - minor update preparation / 浠呯増鏈彿鍗囩骇 - 涓哄皬鏇存柊鍑嗗�?

---

## [3.1.0] - 2025-11-XX

### Added / 鏂板�?

- Categorized settings page navigation / 鍒嗙被璁剧疆椤甸潰瀵艰�?
- Advanced CLI with enhanced functionality / 澧炲己鍔熻兘鐨勯珮绾у懡浠よ宸ュ�?
- Multiple SSIDs support for WiFi automation triggers / WiFi 鑷姩鍖栬Е鍙戝櫒鏀寔澶氫�?SSID
- Periodic action automation / 鍛ㄦ湡鎬ф搷浣滆嚜鍔ㄥ�?

### Fixed / 淇�?

- Power plan selector in settings / 璁剧疆涓殑鐢垫簮璁″垝閫夋嫨鍣?
- User inactivity timer bug / 鐢ㄦ埛闈炴椿鍔ㄨ鏃跺櫒閿欒�?
- CLI validator logic and duplicate WTS entries / CLI 楠岃瘉鍣ㄩ€昏緫鍜岄噸澶?WTS 鏉＄�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- UI responsiveness and performance / 鐣岄潰鍝嶅簲鎬у拰鎬ц兘
- Error messages and user feedback / 閿欒娑堟伅鍜岀敤鎴峰弽�?
- Hardware detection and compatibility / 纭欢妫€娴嬪拰鍏煎鎬?

---

## [3.0.5] - 2026-01-11

### Fixed / 淇�?

- Installer build configuration in make.bat / make.bat 涓殑瀹夎绋嬪簭鏋勫缓閰嶇疆
- GitHub Actions workflow permissions / GitHub Actions 宸ヤ綔娴佹潈�?

---

## [3.0.4] - 2026-01-11

### Fixed / 淇�?

- GitHub Actions release permissions and updates / GitHub Actions 鍙戝竷鏉冮檺鍜屾洿鏂?

---

## [3.0.3] - 2026-01-11

### Fixed / 淇�?

- Minor compatibility fixes / 灏忕殑鍏煎鎬т慨�?

---

## [3.0.2] - 2026-01-11

### Fixed / 淇�?

- Version bump to 3.0 series / 鐗堟湰鍗囩骇�?.0绯诲�?

---

## [3.0.1] - 2025-09-XX

### Added / 鏂板�?

- .NET 8.0 migration / .NET 8.0 杩佺Щ
- Improved error handling and logging / 鏀硅繘閿欒澶勭悊鍜屾棩蹇楄�?
- Shell integration enhancements / Shell 闆嗘垚澧炲己

### Fixed / 淇�?

- ShellIntegration submodule paths and build artifacts / ShellIntegration 瀛愭ā鍧楄矾寰勫拰鏋勫缓浜х�?
- Installation and distribution issues / 瀹夎鍜屽垎鍙戦棶棰?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Performance optimizations / 鎬ц兘浼樺�?
- Code cleanup and refactoring / 浠ｇ爜娓呯悊鍜岄噸鏋?

---

## [2.26.1] - 2023-08-XX

### Fixed / 淇�?

- Security vulnerability fixes / 瀹夊叏婕忔礊淇�?
- Minor bug fixes and stability improvements / 灏忛敊璇慨澶嶅拰绋冲畾鎬ф敼�?

---

## [2.26.0] - 2023-08-XX

### Added / 鏂板�?

- Final stability improvements before 3.0 migration / 3.0杩佺Щ鍓嶇殑鏈€缁堢ǔ瀹氭€ф敼�?
- Enhanced hardware support for new Legion models / 瀵规柊Legion鍨嬪彿鐨勫寮虹‖浠舵敮�?

---

## [2.25.3] - 2023-08-XX

### Fixed / 淇�?

- Critical bug fixes for production stability / 鐢熶骇鐜绋冲畾鎬х殑鍏抽敭閿欒淇

---

## [2.25.2] - 2023-08-XX

### Fixed / 淇�?

- Minor bug fixes and performance optimizations / 灏忛敊璇慨澶嶅拰鎬ц兘浼樺寲

---

## [2.25.1] - 2023-08-XX

### Fixed / 淇�?

- Stability improvements and crash fixes / 绋冲畾鎬ф敼杩涘拰宕╂簝淇

---

## [2.25.0] - 2023-08-XX

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Code quality and maintainability improvements / 浠ｇ爜璐ㄩ噺鍜屽彲缁存姢鎬ф敼�?
- Performance optimizations and resource management / 鎬ц兘浼樺寲鍜岃祫婧愮鐞?

---

## [2.24.2] - 2023-07-XX

### Fixed / 淇�?

- Minor bug fixes and stability improvements / 灏忛敊璇慨澶嶅拰绋冲畾鎬ф敼�?

---

## [2.24.1] - 2023-07-XX

### Fixed / 淇�?

- Bug fixes for reported issues / 宸叉姤鍛婇棶棰樼殑閿欒淇�?

---

## [2.24.0] - 2023-07-XX

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Overall system stability and performance / 鏁翠綋绯荤粺绋冲畾鎬у拰鎬ц�?
- User experience enhancements / 鐢ㄦ埛浣撻獙澧炲�?

---

## [2.23.1] - 2023-07-XX

### Fixed / 淇�?

- Critical bug fixes and stability improvements / 鍏抽敭閿欒淇鍜岀ǔ瀹氭€ф敼�?

---

## [2.23.0] - 2023-07-XX

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Performance optimizations and memory management / 鎬ц兘浼樺寲鍜屽唴瀛樼鐞?
- Enhanced error handling and logging / 澧炲己鐨勯敊璇鐞嗗拰鏃ュ織璁板綍

---

## [2.22.2] - 2023-06-XX

### Fixed / 淇�?

- Security patches and minor bug fixes / 瀹夊叏琛ヤ竵鍜屽皬閿欒淇�?

---

## [2.22.1] - 2023-06-XX

### Fixed / 淇�?

- Bug fixes for stability and compatibility / 绋冲畾鎬у拰鍏煎鎬х殑閿欒淇

---

## [2.22.0] - 2023-06-XX

### Added / 鏂板�?

- Performance monitoring improvements / 鎬ц兘鐩戞帶鏀硅繘
- Enhanced hardware detection capabilities / 澧炲己鐨勭‖浠舵娴嬭兘鍔?

---

## [2.21.3] - 2023-05-XX

### Fixed / 淇�?

- Critical stability fixes and crash prevention / 鍏抽敭绋冲畾鎬т慨澶嶅拰宕╂簝闃叉�?

---

## [2.21.2] - 2023-05-XX

### Fixed / 淇�?

- Minor bug fixes and user experience improvements / 灏忛敊璇慨澶嶅拰鐢ㄦ埛浣撻獙鏀硅繘

---

## [2.21.1] - 2023-05-XX

### Fixed / 淇�?

- Bug fixes for reported issues / 宸叉姤鍛婇棶棰樼殑閿欒淇�?

---

## [2.21.0] - 2023-05-XX

### Added / 鏂板�?

- Advanced fan control improvements / 楂樼骇椋庢墖鎺у埗鏀硅繘
- Enhanced system integration features / 澧炲己鐨勭郴缁熼泦鎴愬姛�?

---

## [2.20.2] - 2023-04-XX

### Fixed / 淇�?

- Minor stability fixes and performance improvements / 灏忕ǔ瀹氭€т慨澶嶅拰鎬ц兘鏀硅繘

---

## [2.20.1] - 2023-04-XX

### Fixed / 淇�?

- Bug fixes for user-reported issues / 鐢ㄦ埛鎶ュ憡闂鐨勯敊璇慨澶?

---

## [2.20.0] - 2023-04-XX

### Added / 鏂板�?

- New automation triggers and actions / 鏂扮殑鑷姩鍖栬Е鍙戝櫒鍜屾搷�?
- Enhanced RGB lighting effects / 澧炲己鐨凴GB鐏厜鏁堟灉

---

## [2.19.0] - 2023-03-XX

### Added / 鏂板�?

- Improved system monitoring capabilities / 鏀硅繘鐨勭郴缁熺洃鎺у姛�?
- Enhanced user interface responsiveness / 澧炲己鐨勭敤鎴风晫闈㈠搷搴旀�?

---

## [2.18.0] - 2023-02-XX

### Added / 鏂板�?

- Additional hardware support for new Legion models / 瀵规柊Legion鍨嬪彿鐨勯澶栫‖浠舵敮�?
- Performance optimizations and bug fixes / 鎬ц兘浼樺寲鍜岄敊璇慨澶?

---

## [2.17.0] - 2023-02-XX

### Added / 鏂板�?

- Enhanced automation system features / 澧炲己鐨勮嚜鍔ㄥ寲绯荤粺鍔熻�?
- Improved system stability and performance / 鏀硅繘鐨勭郴缁熺ǔ瀹氭€у拰鎬ц兘

---

## [2.16.1] - 2023-08-25

### Fixed / 淇�?

- Fix resharper warnings / 淇�?ReSharper 璀﹀�?
- Fix #935 / 淇闂�?#935
- Fix crash caused by inputting non-digit into color picker input (#934) / 淇棰滆壊閫夋嫨鍣ㄨ緭鍏ラ潪鏁板瓧瀵艰嚧鐨勫穿�?(#934)
- New Crowdin updates (#930) / 鏂扮�?Crowdin 鏇存�?(#930)
- New Crowdin updates (#929) / 鏂扮�?Crowdin 鏇存�?(#929)

---

## [2.16.0] - 2023-08-24

### Added / 鏂板�?

- Final 2.x release with stability improvements / 甯︹€嬧€嬬ǔ瀹氭€ф敼杩涚殑鏈€缁?.x鐗堟�?
- Enhanced compatibility and performance / 澧炲己鐨勫吋瀹规€у拰鎬ц兘

---

## [2.15.4] - 2023-07-18

### Fixed / 淇�?

- Critical stability fixes for production use / 鐢熶骇浣跨敤鐨勫叧閿ǔ瀹氭€т慨�?
- Performance improvements and bug fixes / 鎬ц兘鏀硅繘鍜岄敊璇慨�?

---

## [2.15.3] - 2023-07-18

### Fixed / 淇�?

- Minor bug fixes and improvements / 灏忛敊璇慨澶嶅拰鏀硅繘

---

## [2.15.2] - 2023-07-18

### Fixed / 淇�?

- Additional bug fixes and stability improvements / 棰濆鐨勯敊璇慨澶嶅拰绋冲畾鎬ф敼杩?

---

## [2.15.1] - 2023-07-12

### Fixed / 淇�?

- Bug fixes for user-reported issues / 鐢ㄦ埛鎶ュ憡闂鐨勯敊璇慨澶?
- Stability and performance improvements / 绋冲畾鎬у拰鎬ц兘鏀硅繘

---

## [2.15.0] - 2023-08-XX

### Added / 鏂板�?

- Experimental GPU Working Mode switch / 瀹為獙鎬?GPU 宸ヤ綔妯″紡鍒囨�?
- Spectrum RGB keyboard backlight control / Spectrum RGB 閿洏鑳屽厜鎺у埗
- Panel logo and ports backlight options / 闈㈡澘鏍囧織鍜岀鍙ｈ儗鍏夐€夐�?
- Boot logo customization / 鍚姩鏍囧織鑷畾涔?
- Advanced fan curve controls / 楂樼骇椋庢墖鏇茬嚎鎺у�?

### Fixed / 淇�?

- Compatibility with various Legion models / 涓庡悇绉?Legion 鍨嬪彿鐨勫吋瀹规�?
- Keyboard backlight control issues / 閿洏鑳屽厜鎺у埗闂�?
- Power mode switching stability / 鐢垫簮妯″紡鍒囨崲绋冲畾�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- RGB lighting effects and customization / RGB 鐏厜鏁堟灉鍜岃嚜瀹氫�?
- UI for keyboard and lighting controls / 閿洏鍜岀伅鍏夋帶鍒剁晫闈?
- Hardware detection and device support / 纭欢妫€娴嬪拰璁惧鏀寔

---

## [2.14.0] - 2023-07-XX

### Added / 鏂板�?

- GPU overclocking support / GPU 瓒呴鏀寔
- Advanced automation with time-based triggers / 鍩轰簬鏃堕棿鐨勮Е鍙戝櫒楂樼骇鑷姩鍖?
- Custom tray icon tooltips / 鑷畾涔夋墭鐩樺浘鏍囧伐鍏锋彁绀?
- Monitor (dis)connected automation triggers / 鏄剧ず鍣ㄨ繛�?鏂紑鑷姩鍖栬Е鍙戝�?

### Fixed / 淇�?

- Runtime exceptions and crashes / 杩愯鏃跺紓甯稿拰宕╂簝
- Process listener restart issues / 杩涚▼鐩戝惉鍣ㄩ噸鍚棶�?
- Various UI bugs and inconsistencies / 鍚勭鐣岄潰閿欒鍜屼笉涓€�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Performance optimization for sensors / 浼犳劅鍣ㄦ€ц兘浼樺寲
- Notification system and positioning / 閫氱煡绯荤粺鍜屽畾浣?
- Compatibility with newer Windows versions / 涓庤緝鏂?Windows 鐗堟湰鐨勫吋瀹规�?

---

## [2.14.3] - 2023-06-26

### Fixed / 淇�?

- Critical bug fixes and stability improvements / 鍏抽敭閿欒淇鍜岀ǔ瀹氭€ф敼�?

---

## [2.14.2] - 2023-06-24

### Fixed / 淇�?

- Additional bug fixes and performance improvements / 棰濆鐨勯敊璇慨澶嶅拰鎬ц兘鏀硅繘

---

## [2.14.1] - 2023-06-21

### Fixed / 淇�?

- Minor bug fixes and user experience improvements / 灏忛敊璇慨澶嶅拰鐢ㄦ埛浣撻獙鏀硅繘

---

## [2.13.2] - 2023-05-25

### Fixed / 淇�?

- Minor stability fixes and performance improvements / 灏忕ǔ瀹氭€т慨澶嶅拰鎬ц兘鏀硅繘

---

## [2.13.1] - 2023-05-25

### Fixed / 淇�?

- Bug fixes for user-reported issues / 鐢ㄦ埛鎶ュ憡闂鐨勯敊璇慨澶?

---

## [2.13.2] - 2023-05-25

### Fixed / 淇�?

- Additional WiFi automation stability fixes / 棰濆鐨刉iFi鑷姩鍖栫ǔ瀹氭€т慨�?

---

## [2.13.1] - 2023-05-25

### Fixed / 淇�?

- WiFi automation bug fixes and improvements / WiFi鑷姩鍖栭敊璇慨澶嶅拰鏀硅繘

---

## [2.13.0] - 2023-06-XX

### Added / 鏂板�?

- WiFi connect/disconnect automation actions / WiFi 杩炴�?鏂紑鑷姩鍖栨搷浣?
- Resume trigger for automation pipelines / 鑷姩鍖栨祦姘寸嚎鐨勬仮澶嶈Е鍙戝�?
- Battery temperature monitoring and wear level / 鐢垫睜娓╁害鐩戞帶鍜屾崯鑰楃瓑绾?
- HWiNFO64 integration for advanced monitoring / HWiNFO64 闆嗘垚鐢ㄤ簬楂樼骇鐩戞帶

### Fixed / 淇�?

- Gaming detection and automation / 娓告垙妫€娴嬪拰鑷姩鍖?
- Power mode synchronization / 鐢垫簮妯″紡鍚屾�?
- Various stability and compatibility issues / 鍚勭绋冲畾鎬у拰鍏煎鎬ч棶棰?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Automation pipeline processing / 鑷姩鍖栨祦姘寸嚎澶勭悊
- Hardware monitoring and sensors / 纭欢鐩戞帶鍜屼紶鎰熷櫒
- User interface responsiveness / 鐢ㄦ埛鐣岄潰鍝嶅簲鎬?

---

## [2.23.1] - 2023-XX-XX

### Fixed / 淇�?

- Critical stability fixes and performance optimizations / 鍏抽敭绋冲畾鎬т慨澶嶅拰鎬ц兘浼樺寲

---

## [2.23.0] - 2023-XX-XX

### Added / 鏂板�?

- Performance monitoring improvements and system integration / 鎬ц兘鐩戞帶鏀硅繘鍜岀郴缁熼泦�?

---

## [2.12.0] - 2023-05-XX

### Added / 鏂板�?

- HDR state automation and triggers / HDR 鐘舵€佽嚜鍔ㄥ寲鍜岃Е鍙戝�?
- Device connected/disconnected automation / 璁惧杩炴帴/鏂紑鑷姩�?
- Advanced power plan management / 楂樼骇鐢垫簮璁″垝绠＄�?
- Custom boot logo feature / 鑷畾涔夊惎鍔ㄦ爣蹇楀姛鑳?

### Fixed / 淇�?

- Display brightness control issues / 鏄剧ず浜害鎺у埗闂�?
- Power mode indicator errors / 鐢垫簮妯″紡鎸囩ず鍣ㄩ敊�?
- Automation pipeline failures / 鑷姩鍖栨祦姘寸嚎鏁呴殰

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- User activity detection / 鐢ㄦ埛娲诲姩妫€�?
- Battery information accuracy / 鐢垫睜淇℃伅鍑嗙‘鎬?
- Overall system performance / 鏁翠綋绯荤粺鎬ц兘

---

## [2.11.2] - 2023-03-18

### Fixed / 淇�?

- Critical stability fixes and bug resolutions / 鍏抽敭绋冲畾鎬т慨澶嶅拰閿欒瑙ｅ�?

---

## [2.11.1] - 2023-03-18

### Fixed / 淇�?

- Minor bug fixes and performance improvements / 灏忛敊璇慨澶嶅拰鎬ц兘鏀硅繘

---

## [2.11.0] - 2023-04-XX

### Added / 鏂板�?

- Multiple SSIDs for WiFi triggers / WiFi 瑙﹀彂鍣ㄦ敮鎸佸涓?SSID
- DPI scale automation / DPI 缂╂斁鑷姩�?
- Screen resolution switching automation / 灞忓箷鍒嗚鲸鐜囧垏鎹㈣嚜鍔ㄥ�?
- Custom notification positioning / 鑷畾涔夐€氱煡瀹氫�?

### Fixed / 淇�?

- Touchpad scrolling performance / 瑙︽懜鏉挎粴鍔ㄦ€ц兘
- Process listener functionality / 杩涚▼鐩戝惉鍣ㄥ姛鑳?
- Notification display and positioning / 閫氱煡鏄剧ず鍜屽畾浣?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- UI scaling and high DPI support / 鐣岄潰缂╂斁鍜岄�?DPI 鏀寔
- Automation step execution / 鑷姩鍖栨楠ゆ墽琛?
- Error handling and user feedback / 閿欒澶勭悊鍜岀敤鎴峰弽�?

---

## [2.10.0] - 2023-03-XX

### Added / 鏂板�?

- RGB keyboard automation steps / RGB 閿洏鑷姩鍖栨楠?
- Custom dashboard widgets and groups / 鑷畾涔変华琛ㄦ澘灏忛儴浠跺拰鍒嗙粍
- Update available notifications / 鏇存柊鍙敤閫氱�?
- Battery usage time estimation / 鐢垫睜浣跨敤鏃堕棿浼扮畻

### Fixed / 淇�?

- Power mode state restoration / 鐢垫簮妯″紡鐘舵€佹仮澶?
- GPU controller initialization / GPU 鎺у埗鍣ㄥ垵濮嬪寲
- Settings import/export functionality / 璁剧疆瀵煎�?瀵煎嚭鍔熻兘

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Dashboard customization and layout / 浠〃鏉胯嚜瀹氫箟鍜屽竷灞€
- RGB lighting consistency / RGB 鐏厜涓€鑷存€?
- Overall application performance / 鏁翠綋搴旂敤绋嬪簭鎬ц�?

---

## [2.9.1] - 2023-02-08

### Fixed / 淇�?

- Stability improvements and bug fixes / 绋冲畾鎬ф敼杩涘拰閿欒淇
- Performance optimizations / 鎬ц兘浼樺�?

---

## [2.9.0] - 2023-02-XX

### Added / 鏂板�?

- AI mode with intelligent performance adjustment / AI 妯″紡鍙婃櫤鑳芥€ц兘璋冩暣
- Advanced fan control with custom curves / 楂樼骇椋庢墖鎺у埗鍙婅嚜瀹氫箟鏇茬嚎
- GPU temperature and utilization monitoring / GPU 娓╁害鍜屽埄鐢ㄧ巼鐩戞帶
- Custom power mode settings / 鑷畾涔夌數婧愭ā寮忚缃?

### Fixed / 淇�?

- Hybrid mode switching reliability / 娣峰悎妯″紡鍒囨崲鍙潬�?
- Fan curve application / 椋庢墖鏇茬嚎搴旂�?
- Thermal sensor readings / 娓╁害浼犳劅鍣ㄨ鏁?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Fan control algorithms / 椋庢墖鎺у埗绠楁硶
- Temperature monitoring accuracy / 娓╁害鐩戞帶鍑嗙‘鎬?
- System stability under load / 璐熻浇涓嬬郴缁熺ǔ瀹氭�?

---

## [2.8.1] - 2023-01-18

### Fixed / 淇�?

- Minor bug fixes and stability improvements / 灏忛敊璇慨澶嶅拰绋冲畾鎬ф敼�?
- GPU mode switching reliability / GPU妯″紡鍒囨崲鍙潬�?

---

## [2.8.0] - 2023-01-XX

### Added / 鏂板�?

- Hybrid GPU mode support / 娣峰�?GPU 妯″紡鏀寔
- Advanced power limit controls / 楂樼骇鍔熻€楅檺鍒舵帶鍒?
- Battery health monitoring / 鐢垫睜鍋ュ悍鐩戞�?
- Custom automation triggers / 鑷畾涔夎嚜鍔ㄥ寲瑙﹀彂鍣?

### Fixed / 淇�?

- GPU mode switching / GPU 妯″紡鍒囨�?
- Power limit application / 鍔熻€楅檺鍒跺簲�?
- Battery status reporting / 鐢垫睜鐘舵€佹姤�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- GPU management and control / GPU 绠＄悊鍜屾帶�?
- Power efficiency optimization / 鍔熸晥浼樺寲
- Hardware compatibility detection / 纭欢鍏煎鎬ф�?

---

## [2.7.1] - 2022-12-15

### Fixed / 淇�?

- Automation pipeline reliability improvements / 鑷姩鍖栨祦姘寸嚎鍙潬鎬ф敼�?
- Minor bug fixes and performance tweaks / 灏忛敊璇慨澶嶅拰鎬ц兘璋冩暣

---

## [2.7.0] - 2022-12-XX

### Added / 鏂板�?

- Automation system with pipelines and triggers / 鑷姩鍖栫郴缁熷強娴佹按绾垮拰瑙﹀彂鍣?
- Process start/stop automation / 杩涚▼鍚姩/鍋滄鑷姩�?
- Time-based automation triggers / 鍩轰簬鏃堕棿鐨勮嚜鍔ㄥ寲瑙﹀彂鍣?
- WiFi network automation triggers / WiFi 缃戠粶鑷姩鍖栬Е鍙戝�?

### Fixed / 淇�?

- Application startup and initialization / 搴旂敤绋嬪簭鍚姩鍜屽垵濮嬪�?
- Settings persistence and loading / 璁剧疆鎸佷箙鍖栧拰鍔犺浇
- UI responsiveness during automation / 鑷姩鍖栨湡闂寸晫闈㈠搷搴旀�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Automation performance and reliability / 鑷姩鍖栨€ц兘鍜屽彲闈犳�?
- User interface for automation setup / 鑷姩鍖栬缃敤鎴风晫�?
- Error handling in automation pipelines / 鑷姩鍖栨祦姘寸嚎涓殑閿欒澶勭悊

---

## [2.6.5] - 2022-11-06

### Fixed / 淇�?

- Keyboard backlight control improvements / 閿洏鑳屽厜鎺у埗鏀硅繘
- Stability fixes and performance optimizations / 绋冲畾鎬т慨澶嶅拰鎬ц兘浼樺�?

---

## [2.6.4] - 2022-10-19

### Fixed / 淇�?

- RGB lighting consistency fixes / RGB鐏厜涓€鑷存€т慨澶?
- Minor bug fixes and improvements / 灏忛敊璇慨澶嶅拰鏀硅繘

---

## [2.6.3] - 2022-10-16

### Fixed / 淇�?

- Color application and persistence fixes / 棰滆壊搴旂敤鍜屾寔涔呮€т慨澶?
- Performance optimizations / 鎬ц兘浼樺�?

---

## [2.6.2] - 2022-09-30

### Fixed / 淇�?

- Keyboard detection improvements / 閿洏妫€娴嬫敼�?
- Minor stability fixes / 灏忕ǔ瀹氭€т慨�?

---

## [2.6.1] - 2022-09-29

### Fixed / 淇�?

- RGB control conflicts and errors / RGB鎺у埗鍐茬獊鍜岄敊�?
- Initial RGB system stability / 鍒濆RGB绯荤粺绋冲畾�?

---

## [2.6.0] - 2022-11-XX

### Added / 鏂板�?

- RGB keyboard backlight control / RGB 閿洏鑳屽厜鎺у埗
- Multiple color zones and effects / 澶氳壊褰╁尯鍩熷拰鏁堟灉
- Keyboard lighting presets / 閿洏鐏厜棰勮�?
- Real-time color picker / 瀹炴椂棰滆壊閫夋嫨鍣?

### Fixed / 淇�?

- RGB control conflicts with Vantage / �?Vantage �?RGB 鎺у埗鍐茬�?
- Keyboard detection and initialization / 閿洏妫€娴嬪拰鍒濆鍖?
- Color application and persistence / 棰滆壊搴旂敤鍜屾寔涔呭寲

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- RGB lighting performance / RGB 鐏厜鎬ц�?
- User interface for RGB controls / RGB 鎺у埗鐢ㄦ埛鐣岄潰
- Hardware compatibility for RGB / RGB 纭欢鍏煎�?

---

## [2.5.0] - 2022-10-XX

### Added / 鏂板�?

- Package downloader for drivers and utilities / 椹卞姩绋嬪簭鍜屽疄鐢ㄧ▼搴忓寘涓嬭浇�?
- System information and warranty display / 绯荤粺淇℃伅鍜屼繚淇樉绀?
- Advanced compatibility checking / 楂樼骇鍏煎鎬ф�?
- Custom notification system / 鑷畾涔夐€氱煡绯荤�?

### Fixed / 淇�?

- Update checking and notifications / 鏇存柊妫€鏌ュ拰閫氱�?
- Package download and installation / 鍖呬笅杞藉拰瀹夎�?
- System information accuracy / 绯荤粺淇℃伅鍑嗙‘鎬?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Download management and reliability / 涓嬭浇绠＄悊鍜屽彲闈犳€?
- User interface for system info / 绯荤粺淇℃伅鐢ㄦ埛鐣岄潰
- Overall application stability / 鏁翠綋搴旂敤绋嬪簭绋冲畾�?

---

## [2.4.1] - 2022-08-16

### Fixed / 淇�?

- Display configuration issues / 鏄剧ず閰嶇疆闂�?
- Stability improvements and bug fixes / 绋冲畾鎬ф敼杩涘拰閿欒淇

---

## [2.4.0] - 2022-09-XX

### Added / 鏂板�?

- Custom power mode with full control / 瀹屽叏鎺у埗鐨勮嚜瀹氫箟鐢垫簮妯″紡
- Advanced CPU and GPU power limits / 楂樼�?CPU �?GPU 鍔熻€楅檺鍒?
- Temperature-based performance scaling / 鍩轰簬娓╁害鐨勬€ц兘缂╂�?
- Real-time performance monitoring / 瀹炴椂鎬ц兘鐩戞帶

### Fixed / 淇�?

- Power mode switching reliability / 鐢垫簮妯″紡鍒囨崲鍙潬�?
- Performance limit application / 鎬ц兘闄愬埗搴旂敤
- Temperature sensor readings / 娓╁害浼犳劅鍣ㄨ鏁?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Power management algorithms / 鐢垫簮绠＄悊绠楁�?
- Hardware control precision / 纭欢鎺у埗绮惧害
- User interface responsiveness / 鐢ㄦ埛鐣岄潰鍝嶅簲鎬?

---

## [2.3.1] - 2022-08-04

### Fixed / 淇�?

- RGB keyboard control improvements and logging / RGB閿洏鎺у埗鏀硅繘鍜屾棩蹇楄�?
- Display and power management fixes / 鏄剧ず鍜岀數婧愮鐞嗕慨澶?

---

## [2.3.0] - 2022-08-XX

### Added / 鏂板�?

- White keyboard backlight control / 鐧借壊閿洏鑳屽厜鎺у埗
- Microphone mute/unmute automation / 楹﹀厠椋庨潤�?鍙栨秷闈欓煶鑷姩鍖?
- Display refresh rate control / 鏄剧ず鍒锋柊鐜囨帶鍒?
- Advanced power plan management / 楂樼骇鐢垫簮璁″垝绠＄�?

### Fixed / 淇�?

- Keyboard backlight detection / 閿洏鑳屽厜妫€�?
- Display configuration issues / 鏄剧ず閰嶇疆闂�?
- Power plan synchronization / 鐢垫簮璁″垝鍚屾�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Keyboard control reliability / 閿洏鎺у埗鍙潬�?
- Display management / 鏄剧ず绠＄悊
- Power management integration / 鐢垫簮绠＄悊闆嗘�?

---

## [2.2.1] - 2022-06-30

### Fixed / 淇�?

- Color application consistency / 棰滆壊搴旂敤涓€鑷存�?
- RGB control reliability / RGB鎺у埗鍙潬鎬?

---

## [2.2.0] - 2022-07-XX

### Added / 鏂板�?

- RGB keyboard preset system / RGB 閿洏棰勮绯荤�?
- Custom color schemes and effects / 鑷畾涔夐鑹叉柟妗堝拰鏁堟�?
- Keyboard automation integration / 閿洏鑷姩鍖栭泦鎴?
- Enhanced RGB control algorithms / 澧炲己鐨?RGB 鎺у埗绠楁�?

### Fixed / 淇�?

- RGB control conflicts and errors / RGB 鎺у埗鍐茬獊鍜岄敊�?
- Color application consistency / 棰滆壊搴旂敤涓€鑷存�?
- Keyboard detection issues / 閿洏妫€娴嬮棶�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- RGB lighting performance / RGB 鐏厜鎬ц�?
- User interface for RGB controls / RGB 鎺у埗鐢ㄦ埛鐣岄潰
- Hardware compatibility / 纭欢鍏煎�?

---

## [2.1.1] - 2022-06-25

### Fixed / 淇�?

- Fix restart after hybrid mode change / 淇娣峰悎妯″紡鏇存敼鍚庨噸鍚棶棰?
- Fix for crash on AMD systems / 淇AMD绯荤粺宕╂簝闂�?
- Added accent color picker / 娣诲姞寮鸿皟鑹查€夋嫨鍣?
- Apply current preset on startup / 鍚姩鏃跺簲鐢ㄥ綋鍓嶉�?

---

## [2.1.0] - 2022-06-XX

### Added / 鏂板�?

- System accent color matching / 绯荤粺涓婚鑹插尮閰?
- Custom themes and appearance settings / 鑷畾涔変富棰樺拰澶栬璁剧�?
- Enhanced UI with WPFUI framework / 浣跨�?WPFUI 妗嗘灦鐨勫寮虹晫闈?
- Tray icon improvements and actions / 鎵樼洏鍥炬爣鏀硅繘鍜屾搷浣?

### Fixed / 淇�?

- Theme application and persistence / 涓婚搴旂敤鍜屾寔涔呭寲
- UI rendering and scaling issues / UI 娓叉煋鍜岀缉鏀鹃棶�?
- Tray icon functionality / 鎵樼洏鍥炬爣鍔熻�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- User interface design and usability / 鐢ㄦ埛鐣岄潰璁捐鍜屽彲鐢ㄦ�?
- System integration and consistency / 绯荤粺闆嗘垚鍜屼竴鑷存€?
- Overall visual experience / 鏁翠綋瑙嗚浣撻�?

---

## [2.0.0] - 2022-05-XX

### Added / 鏂板�?

- Complete rewrite with WPFUI framework / 浣跨�?WPFUI 妗嗘灦瀹屽叏閲嶅啓
- Modern user interface design / 鐜颁唬鐢ㄦ埛鐣岄潰璁捐
- Enhanced hardware compatibility / 澧炲己鐨勭‖浠跺吋瀹规�?
- Advanced power management features / 楂樼骇鐢垫簮绠＄悊鍔熻兘

### Fixed / 淇�?

- Legacy UI framework limitations / 浼犵�?UI 妗嗘灦闄愬埗
- Hardware control reliability / 纭欢鎺у埗鍙潬�?
- System integration issues / 绯荤粺闆嗘垚闂�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Application performance and responsiveness / 搴旂敤绋嬪簭鎬ц兘鍜屽搷搴旀�?
- User experience and workflow / 鐢ㄦ埛浣撻獙鍜屽伐浣滄祦
- Code architecture and maintainability / 浠ｇ爜鏋舵瀯鍜屽彲缁存姢鎬?

---

## [1.6.0] - 2022-04-XX

### Added / 鏂板�?

- Initial RGB keyboard support / 鍒濆�?RGB 閿洏鏀寔
- Basic color control and presets / 鍩烘湰棰滆壊鎺у埗鍜岄璁?
- Keyboard detection and initialization / 閿洏妫€娴嬪拰鍒濆鍖?

### Fixed / 淇�?

- Keyboard compatibility issues / 閿洏鍏煎鎬ч棶�?
- Color application errors / 棰滆壊搴旂敤閿欒�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Hardware detection accuracy / 纭欢妫€娴嬪噯纭�?
- User interface for keyboard controls / 閿洏鎺у埗鐢ㄦ埛鐣岄�?

---

## [1.5.0] - 2022-03-XX

### Added / 鏂板�?

- GPU monitoring and control / GPU 鐩戞帶鍜屾帶�?
- dGPU deactivation support / dGPU 鍋滅敤鏀寔
- Power mode synchronization / 鐢垫簮妯″紡鍚屾�?

### Fixed / 淇�?

- GPU detection issues / GPU 妫€娴嬮棶棰?
- Power mode switching / 鐢垫簮妯″紡鍒囨�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- GPU management reliability / GPU 绠＄悊鍙潬�?
- Performance optimization / 鎬ц兘浼樺�?

---

## [1.4.0] - 2022-02-XX

### Added / 鏂板�?

- Power plan management / 鐢垫簮璁″垝绠＄�?
- Enhanced power mode controls / 澧炲己鐨勭數婧愭ā寮忔帶鍒?
- Windows integration features / Windows 闆嗘垚鍔熻兘

### Fixed / 淇�?

- Power plan synchronization / 鐢垫簮璁″垝鍚屾�?
- Mode switching reliability / 妯″紡鍒囨崲鍙潬�?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- User interface for power management / 鐢垫簮绠＄悊鐢ㄦ埛鐣岄潰
- System integration depth / 绯荤粺闆嗘垚娣卞�?

---

## [1.3.0] - 2022-01-XX

### Added / 鏂板�?

- GPU activity monitoring / GPU 娲诲姩鐩戞帶
- Enhanced compatibility detection / 澧炲己鐨勫吋瀹规€ф�?
- Additional device support / 棰濆璁惧鏀寔

### Fixed / 淇�?

- GPU monitoring accuracy / GPU 鐩戞帶鍑嗙‘鎬?
- Compatibility detection / 鍏煎鎬ф娴?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- Hardware support breadth / 纭欢鏀寔骞垮�?
- Monitoring reliability / 鐩戞帶鍙潬�?

---

## [1.2.0] - 2021-12-XX

### Added / 鏂板�?

- Basic automation features / 鍩烘湰鑷姩鍖栧姛鑳?
- Process monitoring / 杩涚▼鐩戞帶
- Settings persistence / 璁剧疆鎸佷箙�?

### Fixed / 淇�?

- Application stability / 搴旂敤绋嬪簭绋冲畾鎬?
- Settings loading / 璁剧疆鍔犺浇

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- User experience / 鐢ㄦ埛浣撻獙
- System integration / 绯荤粺闆嗘垚

---

## [1.1.0] - 2021-11-XX

### Added / 鏂板�?

- Power mode controls / 鐢垫簮妯″紡鎺у埗
- Basic hardware monitoring / 鍩烘湰纭欢鐩戞�?
- System tray integration / 绯荤粺鎵樼洏闆嗘�?

### Fixed / 淇�?

- Initial stability issues / 鍒濆绋冲畾鎬ч棶�?
- Hardware detection / 纭欢妫€娴?

### Improved / 鏀硅繘

- 淇绯荤粺浼樺寲鐣岄潰鐨勫竷灞€鍒楃储寮曢敊璇苟闄愬埗鍏舵渶澶у搴︼紝闃叉鍐呭婧㈠嚭閬尅宸︿晶鍖哄�?/ Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 澧炲姞绯荤粺浼樺寲鐣岄潰鐨勯€夋嫨鐘舵€佸拰妯″紡璁板繂鍔熻兘 / Added persistence for selection state and page mode in Windows Optimization
- User interface / 鐢ㄦ埛鐣岄潰
- System compatibility / 绯荤粺鍏煎�?

---

## [1.0.0] - 2021-10-XX

### Added / 鏂板�?

- Initial release of Lenovo Legion Toolkit / Lenovo Legion Toolkit 鍒濆鐗堟湰
- Basic power mode switching / 鍩烘湰鐢垫簮妯″紡鍒囨�?
- Hardware compatibility detection / 纭欢鍏煎鎬ф�?
- User interface for Legion devices / Legion 璁惧鐢ㄦ埛鐣岄�?

---

## Migration Guide / 杩佺Щ鎸囧�?

### From 2.x to 3.x / �?2.x �?3.x

- Backup your settings before upgrading / 鍗囩骇鍓嶅浠芥偍鐨勮�?
- Some automation features have been redesigned / 鏌愪簺鑷姩鍖栧姛鑳藉凡閲嶆柊璁捐
- Plugin system replaces old tools functionality / 鎻掍欢绯荤粺鏇挎崲鏃у伐鍏峰姛�?

### From 1.x to 2.x / �?1.x �?2.x

- Complete UI overhaul / 瀹屾暣鐨?UI 鏀归€?
- Settings migration required / 闇€瑕佽缃縼�?
- Enhanced hardware support / 澧炲己鐨勭‖浠舵敮�?

---

## Support / 鏀寔

- **GitHub Issues**: [Report bugs and request features](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/issues)
- **Discord**: [Community support and discussions](https://discord.com/invite/legionseries)

---

## Contributors / 璐＄尞鑰?

Thanks to everyone who has contributed to this project!
鎰熻阿鎵€鏈変负杩欎釜椤圭洰鍋氬嚭璐＄尞鐨勪汉锛?

- Main developer: BartoszCichecki / 涓昏寮€鍙戣€咃細BartoszCichecki
- Community contributors and translators / 绀惧尯璐＄尞鑰呭拰缈昏瘧鑰?
- Beta testers and feedback providers / Beta 娴嬭瘯鑰呭拰鍙嶉鎻愪緵�?

---

*This changelog follows the format established by [Keep a Changelog](https://keepachangelog.com/).*
*姝ゆ洿鏂版棩蹇楅伒寰?[Keep a Changelog](https://keepachangelog.com/) 寤虹珛鐨勬牸寮忋�?
