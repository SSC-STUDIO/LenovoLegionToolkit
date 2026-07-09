# Universal Device Toolkit 娣卞害鎬ц兘浼樺寲涓?AI Agent 鎵归噺閲嶆瀯鎸囧崡

---
### 馃И MANDATORY TESTING METHODOLOGY: THE PRE-CONFIGURED 3-TIER SUITE
When testing, building, or verifying any modification, **YOU MUST STRICTLY USE THE PRE-CONFIGURED 3-TIER TESTING SUITE** defined in:
馃憠 i/CANONICAL_TESTING_AND_VERIFICATION_SUITE.md

This enforces:
1. **Tier 1 (Automated AST/Test Gate)**: dotnet test / cargo test / go test -race / pytest / strict build -warnaserror.
2. **Tier 2 (Live Debug Trace Trap)**: Attached WPF DataBinding TraceListener Level=Warning & Task.Run WMI 2500ms timeout traps.
3. **Tier 3 (5-Locale x 3-DPI Multimodal OCR Matrix)**: Quantified verification against [UI-OCR-Clipping], [UI-OCR-Mojibake], [UI-OCR-Collision], and [UI-OCR-Contrast] across 100% / 125% / 150% DPI and en / zh-Hans / ja / de / ru.
(For Novel, use the pre-configured Literary Continuity & Repetition Pruning Audit loop defined in CANONICAL_TESTING_AND_VERIFICATION_SUITE.md).
---


鏈寚鍗楀熀浜庨€氱敤璁惧宸ュ叿绠?(`UniversalDeviceToolkit` / `LenovoLegionToolkit`) 鍦?Windows 10/11 (.NET 10 + WPF) 鐜涓嬮暱鏈熺殑鎺掓煡涓庢繁搴︿紭鍖栧疄璺垫€荤粨鑰屾垚銆傛棬鍦ㄤ负鍥㈤槦寮€鍙戜汉鍛樺強鍚庣画鎺ュ叆鐨?**AI Agents** 鎻愪緵涓€濂楁爣鍑嗗寲鐨勬€ц兘鎺掓煡銆佸苟鍙戝畨鍏ㄦ敼閫犲強浠ｇ爜閲嶆瀯瑙勮寖銆?
---

## 鐩綍
1. [绗竴閮ㄥ垎锛歐PF 绾跨▼妯″瀷涓庡紓姝ュ苟鍙戣鑼冿紙闃查棯閫€鏍稿績锛塢(#涓€wpf-绾跨▼妯″瀷涓庡紓姝ュ苟鍙戣鑼冮槻闂€€鏍稿績)
2. [绗簩閮ㄥ垎锛歐MI 涓庣‖浠跺簳灞傞€氫俊浼樺寲锛堥槻鍗℃涓?RDP 浼樺寲锛塢(#浜寃mi-涓庣‖浠跺簳灞傞€氫俊浼樺寲闃插崱姝讳笌-rdp-浼樺寲)
3. [绗笁閮ㄥ垎锛氬悗鍙拌疆璇€佷紶鎰熷櫒鐩戞帶涓庣鐩?I/O 娌荤悊](#涓夊悗鍙拌疆璇紶鎰熷櫒鐩戞帶涓庣鐩?io-娌荤悊)
4. [绗洓閮ㄥ垎锛歐PF UI 娓叉煋涓庨珮棰戝搷搴斾紭鍖朷(#鍥泈pf-ui-娓叉煋涓庨珮棰戝搷搴斾紭鍖?
5. [绗簲閮ㄥ垎锛氶潰鍚?AI Agent 鐨勬壒閲忔帓鏌ヤ笌鏀归€犳爣鍑嗘祦绋?(SOP)](#浜旈潰鍚?ai-agent-鐨勬壒閲忔帓鏌ヤ笌鏀归€犳爣鍑嗘祦绋?sop)
6. [绗叚閮ㄥ垎锛氬彲浠ョ洿鎺ュ彂缁欏叾浠?Agent 鐨?Prompt 妯℃澘](#鍏彲浠ョ洿鎺ュ彂缁欏叾浠?agent-鐨?prompt-妯℃澘)
7. [绗竷閮ㄥ垎锛氬椤圭洰鏈潵鍙戝睍鐨勫缓璁炬€ф剰瑙佷笌鏋舵瀯瑙勫垝](#涓冨椤圭洰鏈潵鍙戝睍鐨勫缓璁炬€ф剰瑙佷笌鏋舵瀯瑙勫垝)

---

## 涓€銆乄PF 绾跨▼妯″瀷涓庡紓姝ュ苟鍙戣鑼冿紙闃查棯閫€鏍稿績锛?
### 1. 涓ユ牸鍒掑垎 `.ConfigureAwait(false)` 鐨勪娇鐢ㄨ竟鐣?鍦?.NET 寮傛缂栫▼涓紝`.ConfigureAwait(false)` 鐢ㄤ簬鎸囩ず涓嶉渶瑕佸湪璋冪敤鏂圭殑 `SynchronizationContext`锛堝悓姝ヤ笂涓嬫枃锛変笂缁х画鎵ц锛屼粠鑰屽噺灏戜笂涓嬫枃鍒囨崲寮€閿€骞堕槻姝㈡閿併€傜劧鑰屽湪 WPF 搴旂敤绋嬪簭涓紝婊ョ敤璇ユ柟娉曟槸瀵艰嚧绋嬪簭绁炵闂€€銆佹棤鎻愮ず宕╂簝鐨?*澶村彿鍏冨嚩**銆?
> [!CAUTION]
> **缁濆绂佹鍦?UI 瑙嗗浘灞備娇鐢?`.ConfigureAwait(false)`锛?*
> 涓€鏃﹀悗鍙扮嚎绋嬫睜璇曞浘璇诲彇鎴栧啓鍏?`DependencyProperty`銆佹洿鏂?`Visibility`銆佹搷浣滄帶浠舵枃鏈垨璋冪敤 UI 鏈嶅姟锛堝 `SnackbarHelper`锛夛紝WPF 灏嗙珛鍗虫姏鍑?`InvalidOperationException: The calling thread cannot access this object because a different thread owns it`銆傚湪 `async void` 浜嬩欢涓紝姝ゅ紓甯稿皢鐩存帴鎽ф瘉涓昏繘绋嬨€?
#### 鍒嗗眰鍘熷垯锛?* **馃敶 UI 灞傦紙缁濆绂佹锛?*锛?  * 閫傜敤鑼冨洿锛歚UniversalDeviceToolkit.WPF` 鐩綍涓嬬殑鎵€鏈?`Pages/`銆乣Windows/`銆乣Controls/`銆乣ViewModels/` 浠ュ強浠讳綍鍖呭惈 UI 缁戝畾銆佷簨浠跺鐞嗭紙濡?`Button_Click`銆乣Loaded`銆乣OnRefreshed`锛夌殑绫汇€?  * 瑙勫垯锛氭墍鏈夌殑 `await` 璋冪敤**蹇呴』淇濇寔榛樿**锛堝嵆淇濈暀鍚屾涓婁笅鏂囷級锛屼笉寰楅澶栨坊鍔?`.ConfigureAwait(false)`銆?* **馃煝 搴曞眰绫诲簱灞傦紙寮虹儓鎺ㄨ崘锛?*锛?  * 閫傜敤鑼冨洿锛歚UniversalDeviceToolkit.Lib/`銆乣UniversalDeviceToolkit.CLI/`銆乣UniversalDeviceToolkit.Lib.Automation/` 绛夋棤 WPF 渚濊禆銆佹棤 UI 缁戝畾鐨勫簳灞傛牳蹇冩湇鍔″簱銆?  * 瑙勫垯锛氭墍鏈夌殑寮傛 I/O銆佺‖浠舵煡璇€佹枃浠惰鍐?`await` 璋冪敤锛?*蹇呴』**杩藉姞 `.ConfigureAwait(false)`锛屼互纭繚鏈€澶х▼搴︾殑骞跺彂鎬ц兘涓庨伩鍏嶄笂灞傛閿併€?
---

### 2. 鍚庡彴浠诲姟瀹夊叏鏇存柊 UI 鐨勬爣鍑嗚寖寮?褰撳簳灞傜殑 `Task.Run`銆佸畾鏃跺櫒鎴栫‖浠跺洖璋冧簨浠堕渶瑕佸湪鍚庡彴绾跨▼鍚戝墠鍙板弽棣堣繘搴︽垨鐘舵€佹椂锛屽繀椤讳娇鐢?UI 绾跨▼鐨勫垎鍙戝櫒 (`Dispatcher`) 杩涜瀹夊叏缂栫粍銆?
```csharp
// 鎺ㄨ崘鑼冨紡锛氫娇鐢?Dispatcher.BeginInvoke / Dispatcher.InvokeAsync
Task.Run(async () =>
{
    try
    {
        // 1. 鍦ㄥ悗鍙扮嚎绋嬫墽琛岃€楁椂鎿嶄綔/搴曞眰鏌ヨ (鍔犱笂 .ConfigureAwait(false))
        var mi = await MachineCompatibility.GetMachineInformationAsync().ConfigureAwait(false);
        
        // 2. 鍒囧洖 UI 绾跨▼鏇存柊鐣岄潰
        await Dispatcher.InvokeAsync(() =>
        {
            _deviceInfoIndicatorText.Text = mi.Model;
            _deviceInfoIndicator.Visibility = Visibility.Visible;
        });
    }
    catch (Exception ex)
    {
        Log.Instance.Trace("Failed to update device info.", ex);
    }
});
```

---

### 3. `async void` 浜嬩欢澶勭悊鍣ㄧ殑闃插尽鎬х紪绋?闄や簡 WPF/WinForms 鐨勯《绾т簨浠跺鐞嗗櫒锛堝 `Button_Click`銆乣Loaded`銆乣Unloaded`銆乣Closing`锛夊锛岀粷瀵逛笉瑕佷娇鐢?`async void`銆傚湪浜嬩欢澶勭悊鍣ㄥ唴閮紝蹇呴』鐢?`try-catch` 鍏ㄩ潰鍖呰９淇濇姢銆?
> [!IMPORTANT]
> `async void` 鏂规硶鍐呯殑鏈崟鑾峰紓甯告棤娉曡澶栧眰鐨勮皟鐢ㄨ€呮垨 `Task` 鎹曡幏锛屽畠浠細鐩存帴琚姏鍒?`SynchronizationContext` 鐨勯《灞傦紝瀵艰嚧鏁翠釜绋嬪簭宕╂簝銆?
```csharp
// 姝ｇ‘鐨勪簨浠跺鐞嗗櫒鍐欐硶
private async void ScanButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // 蹇呴』涓嶅姞 ConfigureAwait(false)
        await ViewModel.ScanAsync(CancellationToken.None);
    }
    catch (Exception ex)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace("Scan failed during button click.", ex);
        
        await SnackbarHelper.ShowAsync("Error", ex.Message, SnackbarType.Error);
    }
}
```

---

## 浜屻€乄MI 涓庣‖浠跺簳灞傞€氫俊浼樺寲锛堥槻鍗℃涓?RDP 浼樺寲锛?
### 1. 鑷村懡鐨?WMI 鍚屾闃诲涓庤繙绋嬫闈?(RDP) 閿佹
杞欢鍦ㄥ惎鍔ㄦ椂浼氶€氳繃 WMI锛圵indows Management Instrumentation锛夋煡璇?ACPI銆佺郴缁?BIOS銆佺‖浠堕厤缃互鍙婄洃鍚郴缁熸敞鍐岃〃涓婚鍙樺寲銆傚湪浠ヤ笅鍦烘櫙涓紝鍚屾 WMI 璋冪敤鐨勫簳灞傚唴鏍哥瓑寰呬細闄峰叆**鍐呮牳鎬佹寰幆鎴栬秴鏃堕暱绛夊緟**锛?* 閫氳繃杩滅▼妗岄潰 (RDP) 杩炴帴璁＄畻鏈烘椂锛?* 瀹夎浜嗚櫄鎷熸樉鍗￠┍鍔紙濡?GameViewer Virtual Display / Parsec / 鍓槧铏氭嫙涓叉祦椹卞姩锛夋椂锛?* 鑱旀兂绗旇鏈浜庣壒瀹氱殑鐫＄湢鍞ら啋 / 鑺傝兘鐘舵€佹椂銆?
鐜拌薄涓猴細`ManagementObjectSearcher.Get()` 鎴?`ManagementEventWatcher.Start()` 鍚屾鍗℃鏁板崄绉掞紝CPU 鍐呮牳鎬佸崰鐢ㄩ鍗囷紝涓荤獥鍙ｈ繜杩熶笉寮圭獥鐢氳嚦瀵艰嚧杩滅▼妗岄潰鐢婚潰鍋囨銆?
---

### 2. WMI 鏀归€犻粍閲戞硶鍒?
#### 瑙勫垯 A锛氬畬鍏ㄥ純鐢ㄥ悓姝?WMI 鏌ヨ锛屽繀椤婚檮鍔犺秴鏃朵繚鎶?绂佹鍦ㄤ唬鐮佷腑浣跨敤鐩存帴鍚屾鐨?`mos.Get()`銆傚繀椤婚€氳繃寮傛 Task 鍖呰锛屽苟浣跨敤 `CancellationTokenSource` 鏂藉姞涓ユ牸鐨勮秴鏃堕檺鍒讹紙鎺ㄨ崘 2500ms ~ 5000ms锛夈€?
```csharp
// 鎺ㄨ崘鑼冨紡锛氬甫瓒呮椂鐨勫紓姝?WMI 鏌ヨ灏佽
public static async Task<ManagementObjectCollection> GetAsyncWithTimeout(
    this ManagementObjectSearcher searcher, 
    int timeoutMs = 3000)
{
    using var cts = new CancellationTokenSource(timeoutMs);
    try
    {
        return await Task.Run(() => searcher.Get(), cts.Token).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
        Log.Instance.Warn($"WMI query timed out after {timeoutMs}ms: {searcher.Query.QueryString}");
        throw new TimeoutException($"WMI query timed out after {timeoutMs}ms.");
    }
}
```

#### 瑙勫垯 B锛氱鐢?WMI 鐩戝惉绯荤粺浜嬩欢锛屾敼鐢?Win32 API
瀵逛簬绯荤粺涓婚锛堟繁鑹?娴呰壊妯″紡锛夊垏鎹€佹敞鍐岃〃鍊煎彉鍖栫瓑闀挎湡鐩戝惉浠诲姟锛?*绂佹浣跨敤 `ManagementEventWatcher`**銆傛敼鐢?Win32 绯荤粺鐨勫師鐢熺殑 `PInvoke.RegNotifyChangeKeyValue` 鎴栫獥鍙ｆ秷鎭?`WndProc` (WM_SETTINGCHANGE)锛屽唴瀛樺拰 CPU 寮€閿€闄嶄綆 95% 浠ヤ笂锛屼笖瀹屽叏涓嶄細闃诲鍚姩銆?
---

## 涓夈€佸悗鍙拌疆璇€佷紶鎰熷櫒鐩戞帶涓庣鐩?I/O 娌荤悊

### 1. 鏉滅粷楂橀鍚庡彴杞涓殑鏃ュ織杞扮偢 (Log Spamming)
纭欢宸ュ叿绠遍渶楂橀杞 CPU/GPU 鍔熻€椼€侀鎵囪浆閫熷拰娓╁害浼犳劅鍣紙閫氬父姣忕 1~2 娆★級銆傚湪楂橀鎵ц鐨勫惊鐜唬鐮佽矾寰勶紙濡?`GetDataAsync`銆乣RefreshLoopAsync`锛変腑锛?
> [!WARNING]
> **缁濆绂佹鍦ㄩ珮棰戣疆璇㈠惊鐜腑杈撳嚭甯歌 Trace / Debug 鏃ュ織鎴栨牸寮忓寲 JSON 瀛楃涓诧紒**
> 涔嬪墠鎴戜滑鍦ㄤ紶鎰熷櫒鑾峰彇寰幆涓瘡娆¤緭鍑?`SensorsData` JSON 搴忓垪鍖栧瓧绗︿覆锛屽鑷村湪鍑犱釜灏忔椂鐨勮繍琛屽唴瀛橀噷浜х敓鏁?GB 鐨勭鐩樺啓鍏ユ棩蹇楋紝寮曞彂涓ラ噸鐨勭鐩?I/O 鏀惧ぇ銆佸唴瀛?GC 棰戠箒鍥炴敹浠ュ強绯荤粺鍗￠】銆?
* **鏁存敼鏍囧噯**锛氬彧鏈夊湪**鐘舵€佸彂鐢熼噸澶ф敼鍙?*锛堝椋庢墖妯″紡鍒囨崲銆佹俯搴﹁Е鍙戣鍛婁笂闄愩€佺‖浠剁绾匡級鎴?*鎹曟崏鍒板紓甯?*鏃讹紝鎵嶅厑璁稿啓鍏ョ鐩樻棩蹇椼€傛甯歌疆璇㈠彧闇€鍦ㄥ唴瀛樹腑闈欓粯鏇存柊灞炴€с€?
---

### 2. 鎬ц兘璁℃暟鍣?(Performance Counters) 鐨勫喎鍗寸啍鏂満鍒?鑾峰彇缃戠粶娴侀噺銆佺鐩樿鍐欑瓑鐩戞帶鏁版嵁渚濊禆 `System.Diagnostics.PerformanceCounter`銆傚鏋滈儴鍒?Windows 绯荤粺锛堝绮剧畝鐗?Win10/11锛夋崯鍧忔垨缂哄け鏌愪簺璁℃暟鍣紝楂橀杞浼氭瘡绉掕Е鍙戝苟鎹曡幏澶氭搴曞眰寮傚父锛屾秷鑰楀ぇ閲?CPU銆?
* **瑙勮寖**锛氬繀椤诲搴曞眰鐨勭洃鎺ф簮鍔犲叆**鍐峰嵈鐔旀柇鏃堕棿锛圕ooldown Timer锛?*銆備竴鏃﹁鍙栧け璐ユ垨鎶涘嚭寮傚父锛岀珛鍗虫崟鑾峰苟灏嗚鐩戞帶椤规爣璁颁负绂佺敤锛屾寕璧?30~60 绉掑悗鍐嶅皾璇曢噸寤猴紱濡傛灉鍦ㄥ喎鍗存湡鍐咃紝鐩存帴杩斿洖榛樿鍊硷紙0锛夛紝涓嶅啀棰戠箒鍙戣捣搴曞眰璋冪敤銆?
---

### 3. CLI 杩涚▼涓庡紓姝ユ祦閲嶅畾鍚戠殑璧勬簮鍥炴敹
鍦ㄩ€氳繃 `Process` 璋冪敤鍛戒护琛岋紙濡?`powercfg`銆乣nvidiacli` 绛夛級骞跺紓姝ヨ鍙?`StandardOutput` / `StandardError` 鏃讹細
* 鍦ㄥ鐞嗕换鍔″彇娑?(`CancellationToken`) 鎴栬秴鏃剁粓姝㈡椂锛屽繀椤绘樉寮忚皟鐢?`process.Kill(true)` 鏉€鎺夋暣涓繘绋嬫爲銆?* 蹇呴』姝ｇ‘绛夊緟寮傛璇诲彇娴佸畬鎴愶紝閬垮厤鍦ㄦ祦鏈叧闂墠寮鸿閲婃斁杩涚▼瀵硅薄鑰屾姏鍑?`NullReferenceException`銆?
---

## 鍥涖€乄PF UI 娓叉煋涓庨珮棰戝搷搴斾紭鍖?
### 1. 楂橀浜嬩欢鐨勯槻鎶?(Debounce) 涓庤妭娴?(Throttle)
瀵逛簬鐢ㄦ埛璋冩暣婊戝姩鏉★紙Slider锛夈€佽緭鍏ユ悳绱㈣繃婊ゆ枃鏈€佸疄鏃跺埛鏂版姌绾垮浘绛夐珮棰戣Е鍙?UI 浜嬩欢锛?* **绂佹鐩存帴杩涜瀹炴椂閲嶇粯鎴栧簳灞侷/O璋冪敤**銆?* **鎼滅储/杩囨护鏂囨湰妗?*锛氫娇鐢ㄩ槻鎶栵紙Debounce锛夛紝绛夊緟鐢ㄦ埛鍋滄鎸夐敭 300ms ~ 500ms 鍚庯紝鍐嶈Е鍙戝垪琛ㄩ噸杞斤紙鍙傝€?`WindowsOptimizationPage.Drivers.cs` 涓殑 `CancellationTokenSource` 闃叉姈瀹炵幇锛夈€?* **楂橀鏁板€肩洃鎺х晫闈?*锛氫娇鐢ㄨ妭娴佸垎鍙戝櫒锛坄ThrottleFirstDispatcher` / `ThrottleLastDispatcher`锛夛紝灏?UI 鍒锋柊棰戠巼鎺у埗鍦ㄦ渶楂樻瘡绉?3~5 甯т互鍐咃紝纭繚涓荤晫闈笣婊戜笉鎺夊抚銆?
### 2. 娑堥櫎瑙嗚闃村奖涓庡姞閫熸覆鏌撶憰鐤?鍦?WPF 鑷畾涔夊渾瑙掑崱鐗囪竟妗嗭紙濡?`AppStatusBanner`銆佽鍛婃锛変腑锛屽鏋滃鍥?`Border` 鍏锋湁鍦嗚涓斿唴閮ㄥ祵濂楀鏉傚竷灞€锛屽線寰€浼氱湅鍒板懆閬嚭鐜伴粦鑹茬煩褰㈤槾褰辨垨瑁佸壀杈圭紭銆?* **瑙勮寖**锛氬湪鏈€澶栧眰鐨?`<UserControl>` 蹇呴』璁剧疆 `Background="Transparent"`锛涘寘瑁瑰渾瑙掕儗鏅殑瀹瑰櫒蹇呴』璁剧疆 `BorderBrush="Transparent"` 涓?`BorderThickness="0"`锛岄槻姝㈢‖浠跺姞閫熸覆鏌撴椂鑳屾櫙鍒蜂笌瑁佸壀鍖轰骇鐢熸贩鑹插啿绐併€?
---

## 浜斻€侀潰鍚?AI Agent 鐨勬壒閲忔帓鏌ヤ笌鏀归€犳爣鍑嗘祦绋?(SOP)

涓轰簡璁╁叾浠?AI Agent 鑳藉鑷姩銆佸畨鍏ㄣ€佹壒閲忓湴浼樺寲鏁翠釜浠ｇ爜搴擄紝璇锋寚绀哄畠浠弗鏍奸伒寰互涓?**SOP 鍏宸ヤ綔娉?*锛?
```mermaid
graph TD
    A[姝ラ 1: Grep 瀹氫綅楂樺嵄浠ｇ爜妯″紡] --> B[姝ラ 2: 纭畾鎵€灞炵殑鍒嗗眰杈圭晫]
    B --> C{鏄惁鍦?WPF UI 灞?}
    C -- 鏄?--> D[姝ラ 3: 涓ユ牸绉婚櫎 .ConfigureAwait(false)]
    C -- 鍚?--> E[姝ラ 3: 纭繚浣跨敤 .ConfigureAwait(false)]
    D --> F[姝ラ 4: 妫€鏌ュ苟淇 async void 寮傚父淇濇姢]
    E --> F
    F --> G[姝ラ 5: 瀹¤楂橀杞涓庢棩蹇?I/O]
    G --> H[姝ラ 6: 鑷姩鍖栫紪璇戜笌鍗曟祴楠岃瘉]
```

### 1. 瀹氫綅楂樺嵄妯″紡 (Grep Search)
Agent 蹇呴』浼樺厛浣跨敤楂樼簿搴﹀伐鍏?`grep_search`锛堢粷瀵逛笉瑕佸湪缁堢鐢?bash `grep` / `cat`锛夊叏搴撴悳绱互涓嬬壒寰侊細
* `ConfigureAwait\(false\)` 锛堟鏌ユ槸鍚﹁鐢ㄤ簬 UI 灞傦級
* `async void` 锛堟鏌ユ槸鍚︾己灏?`try-catch` 鎴栬鐢ㄤ簬闈炰簨浠跺鐞嗗櫒锛?* `ManagementObjectSearcher` / `ManagementEventWatcher` / `.Get\(\)` 锛堟鏌ユ槸鍚︽湁鍚屾 WMI 璋冪敤锛?* `Log.Instance.(Trace|Debug|Info)` 锛堟鏌ユ槸鍚﹀湪楂橀杞寰幆涓緭鍑猴級

### 2. 鍒ゅ畾鎵€灞炲垎灞傝竟鐣?* 璇诲彇鏂囦欢缁濆璺緞涓庨《灞傚懡鍚嶇┖闂淬€?* 鍒ゅ畾涓?**WPF UI 灞?* (`*.WPF/Pages`, `*.WPF/Windows`, `*.WPF/Controls`, `*.WPF/ViewModels`) 杩樻槸 **搴曞眰绫诲簱** (`*.Lib`, `*.CLI`, `*.Macro`, `*.Automation`)銆?
### 3. 鏂藉姞寮傛鏀归€犺鍒?* 濡傛灉鍦?UI 灞備笖璋冪敤鏂圭洿鎺ユ搷浣?UI 鍏冪礌銆佷緷璧栧睘鎬ф垨瑙﹀彂鏁版嵁缁戝畾锛氫娇鐢?`replace_file_content` 鎴?`multi_replace_file_content` 鎵归噺绉婚櫎 `.ConfigureAwait(false)`銆?* 濡傛灉鍦ㄧ函搴曞眰绫诲簱灞傦細纭繚鎵€鏈?`await` 鍧囧甫涓?`.ConfigureAwait(false)`銆?
### 4. 琛ュ叏瀹夊叏闃叉姢
* 涓烘墍鏈?`async void` 浜嬩欢鍔犱笂瀹屽杽鐨?`try-catch` 缁撴瀯锛屽苟鍦?`catch` 涓€氳繃 `SnackbarHelper` 鎻愮ず鐢ㄦ埛锛屾潨缁濈▼搴忓穿婧冦€?* 灏嗘墍鏈夌殑鍚屾 WMI 鏌ヨ鏇挎崲涓哄甫瓒呮椂鍖呰鐨勫紓姝?`GetAsyncWithTimeout()`銆?
### 5. 楂橀 I/O 瀹¤
* 妫€鏌?`Timer`銆乣While(true)` 寰幆銆乣Task.Delay` 杞鍐呴儴鐨勬棩蹇楄緭鍑轰唬鐮併€傚皢姣忔寰幆蹇呮墦鐨勫父瑙勬棩蹇楃Щ闄わ紝鏀逛负鐘舵€佸彉鏇磋Е鍙戙€?
### 6. 缂栬瘧涓庡崟鍏冩祴璇曢獙璇?* 鎵ц鍛戒护 `dotnet build <SLN_PATH> -c Debug`锛岀‘淇?**0 Error** 涓旀病鏈夊紩鍏ユ柊鐨勪弗閲嶈鍛娿€?* 鎵ц鍛戒护 `dotnet test`锛岃繍琛岃嚜鍔ㄥ寲娴嬭瘯濂椾欢锛屼繚璇佹牳蹇冮€昏緫鏃犵牬鎹熴€?
---

## 鍏€佸彲浠ョ洿鎺ュ彂缁欏叾浠?Agent 鐨?Prompt 妯℃澘

浣犲彲浠ョ洿鎺ュ鍒朵互涓?Prompt 妯℃澘锛屽垎鍙戠粰鍏朵粬 AI Agent 鎵ц鎵归噺閲嶆瀯涓庢帓鏌ヤ换鍔★細

```markdown
<TASK_PROMPT>
浣犳槸涓€涓祫娣辩殑 .NET 10 + WPF 妗岄潰搴旂敤绋嬪簭鏋舵瀯甯堜笌鎬ц兘浼樺寲涓撳銆傜幇鍦ㄨ浣犳寜鐓с€奤niversal Device Toolkit 娣卞害鎬ц兘浼樺寲涓?AI Agent 鎵归噺閲嶆瀯鎸囧崡銆嬶紝瀵规寚瀹氱殑妯″潡/椤圭洰杩涜鎵归噺瀹℃煡鍜屾€ц兘浼樺寲銆?
### 鏍稿績浠诲姟涓庣害鏉燂細
1. **WPF 寮傛绾跨▼瑙勫垯锛堟渶浼樺厛锛?*锛?   - 涓ョ浣跨敤 bash `grep`锛岃浣跨敤楂樼簿搴︽悳绱㈠伐鍏凤紙濡?`grep_search`锛夋壂鎻忔寚瀹氱洰褰曚笅鎵€鏈夋枃浠朵腑鐨?`ConfigureAwait(false)`銆?   - 濡傛灉鏂囦欢灞炰簬 WPF UI 灞傦紙鍖呭惈 `UserControl`銆乣Window`銆乣Page`銆乣ViewModel`銆佷簨浠跺鐞嗗櫒銆佹搷浣?UI 鎺т欢/渚濊禆灞炴€?Snackbar锛夛紝浣?*蹇呴』**鎵归噺绉婚櫎鍏朵腑鐨?`.ConfigureAwait(false)`锛岃浠ｇ爜杩斿洖 UI 鍚屾涓婁笅鏂囷紝闃叉鎶涘嚭璺ㄧ嚎绋嬪紓甯稿鑷寸▼搴忛棯閫€锛?   - 濡傛灉鏂囦欢灞炰簬搴曞眰鏍稿績绫诲簱锛堟病鏈?UI 渚濊禆鐨勭函鏈嶅姟/宸ュ叿绫伙級锛岃纭繚鍏跺紓姝ユ柟娉曚娇鐢?`.ConfigureAwait(false)`銆?
2. **WMI 涓庡悓姝ラ樆濉炴敼閫?*锛?   - 妫€鏌ヤ唬鐮佷腑鏄惁瀛樺湪鍚屾鐨?WMI 鏌ヨ锛堝 `ManagementObjectSearcher.Get()`锛夋垨鍚屾鐩戝惉锛坄ManagementEventWatcher`锛夈€?   - 灏嗗悓姝?WMI 鏌ヨ鏀逛负寮傛骞堕檮鍔犺秴鏃堕檺鍒讹紙寤鸿 3000ms锛夛紝闃叉鍦ㄨ繙绋嬫闈?(RDP) 鎴栬櫄鎷熸樉鍗￠┍鍔ㄤ笅鍗℃涓荤▼搴忋€?   - 瀵规敞鍐岃〃鍜岀郴缁熻缃洃鍚紝浼樺寲涓?Win32 `RegNotifyChangeKeyValue` 鎴栧紓姝ラ槻鎶栧鐞嗐€?
3. **寮傚父鎹曡幏涓庡悗鍙?I/O 娌荤悊**锛?   - 妫€鏌ユ墍鏈夌殑 `async void` 浜嬩欢澶勭悊鍣紝纭繚鍐呴儴鏈夊畬鏁寸殑 `try-catch` 缁撴瀯淇濇姢锛岄槻姝㈡湭鎹曡幏寮傚父瀵艰嚧搴旂敤绋嬪簭宕╂簝闂€€銆?   - 妫€鏌ラ珮棰戣疆璇唬鐮侊紙濡備紶鎰熷櫒璇诲彇銆侀鎵?鍔熻€楃洃鎺у惊鐜級锛屽交搴曠Щ闄ゅ惊鐜唴鐨勫父瑙勬暟鎹墦鍗版棩蹇楋紙`Log.Instance.Trace/Debug`锛夛紝鏉滅粷纾佺洏 I/O 椋欏崌銆?
4. **瀹夊叏缂栫▼涓庨獙璇?*锛?   - 涓嶈鐮村潖浠ｇ爜涓棤鍏崇殑鏃㈡湁娉ㄩ噴銆佷笟鍔￠€昏緫鍙婅瑷€鏈湴鍖栬祫婧愩€?   - 姣忔閲嶆瀯瀹屾垚鍚庯紝鍔″繀浣跨敤 `run_command` 杩愯 `dotnet build` 妫€鏌ョ紪璇戦敊璇紝骞剁‘淇濈紪璇戦€氳繃锛? 閿欒锛夈€?</TASK_PROMPT>
```

---

## 涓冦€佸椤圭洰鏈潵鍙戝睍鐨勫缓璁炬€ф剰瑙佷笌鏋舵瀯瑙勫垝

鍦ㄦ繁鍏ユ帓鏌ヤ笌閲嶆瀯鏈」鐩殑杩囩▼涓紝涓虹‘淇濋」鐩殑闀胯繙鍙淮鎶ゆ€с€侀珮鍙敤鎬у強瑙勮寖鍖栵紝鎻愬嚭浠ヤ笅寤鸿鎬ф剰瑙佷笌瑙勫垝锛?
### 1. 鏋舵瀯鏀归€狅細鏇翠弗鏍肩殑 MVVM 闅旂涓?UI 绾跨▼璋冨害瑙ｈ€?* **鐜扮姸**锛氱洰鍓嶉儴鍒?UI 鎺т欢鍙?Page 椤甸潰涓紙濡?`WindowsOptimizationPage`銆佸悇涓?`Control` 鍐呴儴锛夋贩鍚堜簡闈炲父澶氱殑涓氬姟閫昏緫銆佸簳灞備笅杞借皟鐢ㄥ拰鐩存帴鐨?`Dispatcher.Invoke` / `Dispatcher.BeginInvoke` 鎿嶄綔銆傝繖瀵艰嚧浠ｇ爜閫昏緫鍦?UI 绾跨▼涓庡悗鍙扮嚎绋嬩箣闂翠氦缁囷紝鏋佹槗寮曟潵绾跨▼瀹夊叏闅愭偅銆?* **寤鸿**锛?  * 寮曞叆鎶借薄灞?**`IUiDispatcher` / `IUiScheduler`** 鎺ュ彛锛屽湪搴曞眰鏈嶅姟鍜?ViewModel 涓彧閫氳繃鎺ュ彛璇锋眰鍥炲埌涓荤嚎绋嬶紝灏?`Wpf.Dispatcher` 浣滀负瀹炵幇绫诲湪 IoC 瀹瑰櫒涓敞鍏ャ€?  * 杩欐牱涓嶄粎鍙互鎶?ViewModel 褰诲簳鍚?WPF 瑙嗗浘瑙ｈ€︼紝澶у箙鎻愬崌鍗曞厓娴嬭瘯鐨勮鐩栫巼鍜岃嚜鍔ㄥ寲娴嬭瘯鍙鎬э紝杩樿兘浠庢牴鏈笂閬垮厤鍥犱负鎵嬪姩鍐欓敊 `Dispatcher` 鎴?`ConfigureAwait` 瀵艰嚧鐨勬閿佷笌璺ㄧ嚎绋嬪紓甯搞€?
### 2. 鑷姩鍖栦唬鐮佸鏌?(CI/CD) 涓庨潤鎬佽娉曞垎鏋愬櫒 (Roslyn Analyzers)
* **鐜扮姸**锛氬紑鍙戜汉鍛樻垨 Agent 鍦ㄥ啓寮傛浠ｇ爜鏃讹紝瀹规槗鍑範鎯『鎵嬪姞涓?`.ConfigureAwait(false)`锛屾垨鑰呬娇鐢ㄥ悓姝ョ殑 WMI API锛岃繖浜涢棶棰樺線寰€鍦ㄧ壒瀹氱‖浠舵垨 RDP 杩滅▼妗岄潰涓嬫墠浼氱垎闆枫€?* **寤鸿**锛?  * 寮曞叆 **Roslyn 闈欐€佽娉曞垎鏋愬櫒锛堝 Meziantou.Analyzer / Microsoft.VisualStudio.Threading.Analyzers锛?*锛屽苟鍦ㄩ」鐩殑 `.editorconfig` 涓厤缃嚜瀹氫箟瑙勫垯銆?  * 鍦?CI 缂栬瘧娴佹按绾匡紙濡?GitHub Actions锛変腑寮哄埗鍚敤瑙勫垯锛?*褰撴娴嬪埌 WPF 瑙嗗浘灞傝皟鐢ㄤ簡 `ConfigureAwait(false)` 鎴栬皟鐢ㄤ簡鍚屾闃诲 API 鏃讹紝鐩存帴瑙﹀彂缂栬瘧 Error**銆備粠婧愬ご鎷︽埅楂樺嵄浠ｇ爜鐨勬彁浜ゃ€?
### 3. 澧炲己骞跺彂涓庣‖浠舵ā鎷熶笓椤规祴璇?* **鐜扮姸**锛氱洰鍓嶉」鐩嫢鏈夊緢濂界殑鍗曞厓娴嬭瘯鍩虹锛?340+ 鍗曟祴锛夛紝浣嗗ぇ閮ㄥ垎闆嗕腑鍦ㄧ函閫昏緫銆佸瓧绗︿覆搴忓垪鍖栧拰宸ュ叿绫伙紝瀵逛簬骞跺彂绔炴€併€佸绾跨▼鎳掑姞杞斤紙濡?`Lazy<T>` 缂撳瓨缂撳瓨娣樻卑锛夈€佺‖浠舵煡璇㈠搷搴旇秴鏃剁瓑鍦烘櫙瑕嗙洊杈冨皯銆?* **寤鸿**锛?  * 缂栧啓閽堝纭欢鎺ュ彛鍜屽簳灞傛湇鍔＄殑 **Mock 妯℃嫙灞?*锛屽湪娴嬭瘯涓汉涓烘敞鍏ュ欢杩燂紙濡傛ā鎷?WMI 闃诲 10 绉掞級涓庡苟鍙戦珮棰戣皟鐢ㄧ殑娴嬭瘯鐢ㄤ緥锛岄獙璇佺郴缁熷湪楂樿礋杞藉拰鏋佺缃戠粶/纭欢鐜涓嬬殑瀹归敊鐔旀柇鑳藉姏銆?  * 閽堝鍗曚緥缂撳瓨鍜岀‖浠剁姸鎬佺鐞嗗櫒锛屽鍔犲绾跨▼骞跺彂璇诲啓鐨勫帇鍔涙祴璇曪紝纭繚绔炴€佸畨鍏ㄣ€?
### 4. 纭欢閫傞厤灞傜殑鎻掍欢鍖栦笌瑙ｈ€?(Hardware Adapter Pattern)
* **鐜扮姸**锛氫綔涓洪€氱敤璁惧宸ュ叿绠憋紝搴曞眰鎵胯浇浜嗗ぇ閲忛拡瀵?Lenovo Legion銆乀hinkPad 绛変笉鍚屾満鍨嬨€丒C 瀵勫瓨鍣ㄣ€佸姛鑰楀銆丟PU 鎺у埗鐨勭壒鍖栭€昏緫銆?* **寤鸿**锛?  * 杩涗竴姝ュ皢纭欢鎺у埗鎶借薄涓烘爣鍑嗗寲鐨?**纭欢閫傞厤鍣ㄩ┍鍔ㄦ帴鍙?(Hardware Adapter / Provider Pattern)**銆?  * 灏嗚仈鎯崇壒鏈夌殑 WMI 鎺ュ彛銆佸祵鍏ュ紡鎺у埗鍣?(EC) 璇诲啓銆佸厜鏁堟帶鍒舵槑纭皝瑁呭湪鐙珛鐨?Provider 鎻掍欢妯″潡涓€傝繖鏍锋湭鏉ュ鏋滆鎵╁睍鏀寔鍏朵粬鍝佺墝锛堝鍗庣 ROG銆佹満姊伴潻鍛姐€佹儬鏅瓑锛夌殑纭欢鎺у埗锛屽彧闇€寮€鍙戝疄鐜版柊鐨?Provider锛岃€屼笉蹇呭湪鏍稿績涓讳笟鍔℃祦绋嬩腑涓嶆柇澧炲姞 `if-else` 鎴栬澶囧吋瀹规€у垽鏂紝鏋佸ぇ鎻愬崌杞‖浠剁敓鎬佺殑鎵╁睍鑳藉姏銆?
