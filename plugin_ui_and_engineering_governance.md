# Plugin UI/UX Design Standards, Engineering Governance & Verification Plan

This document establishes the mandatory UI/UX design specifications, architectural constraints, localization rules, OCR verification pipeline, and open-source marketing standards for all plugins developed within the **UniversalDeviceToolkit-Plugins** repository. 

Our goal is to elevate plugin development from fragmented, ad-hoc scripts into a premium, state-of-the-art ecosystem that seamlessly matches the visual excellence and rock-solid stability of the host Universal Device Toolkit application.

---

## 1. Plugin UI/UX Design Standards & Refactoring Guidelines (插件 UI/UX 强约束与设计规范)

### A. Case Study: Why Legacy Plugin UIs Failed (为什么部分旧版插件 UI “做得跟屎一样”)
An analysis of legacy plugins—specifically the **Network Acceleration (`NetworkAccelerationControl.xaml`)** plugin—reveals critical UI/UX flaws that must be strictly eradicated across all plugins:
1. **Monolithic Clutter & Lack of Information Hierarchy (单页无脑堆砌与层级混乱)**:
   - *The Flaw*: `NetworkAccelerationControl.xaml` spans over 600 lines, dumping hero status banners, mode selection cards, live telemetry metrics, quick optimization plans, and behavior toggles vertically into a single scrolling `StackPanel`. Users are greeted by a wall of text and boxes without visual breathing room or clear operational focus.
   - *The Fix*: **Modular Tabs & Visual Cards**. Separate operational monitoring (Dashboard/Status) from configuration rules (Settings/Advanced) using WPF UI `TabControl` or clean navigation pivots.
2. **Rigid Layouts, Hardcoded Margins & Broken Scaling (死板布局与自适应失效)**:
   - *The Flaw*: Using rigid fixed widths (`Width="40"`), hardcoded pixel padding/margins (`Margin="0,0,18,4"`), and inflexible stack panels. When window sizes change or users run on high-DPI 4K displays with 150%+ scaling, elements clip, text truncates, and alignment breaks awkwardly.
   - *The Fix*: **Responsive Grid & Star Sizing**. Use adaptive WPF layout containers (`Grid` with `Width="*"`, `UniformGrid`, `WrapPanel` with dynamic `MinWidth`) and strictly bind to global design system spacing tokens.
3. **Poor Telemetry Presentation (监控数据展示粗糙)**:
   - *The Flaw*: Live network speeds (Download/Upload Mbps, peak traffic, adapter status) are rendered as plain, uninspired text lines inside nested stack panels, making vital real-time data hard to read at a glance.
   - *The Fix*: **Prominent Metric Cards & Visual Sparklines**. Present real-time telemetry inside dedicated visual cards featuring large, bold typography for numerical values, muted secondary typography for units/labels, and smooth status badges.
4. **Absence of Interactive Polish & Micro-Animations (缺乏交互反馈与微动画)**:
   - *The Flaw*: Clicking "Run Quick Optimization" or switching network profiles abruptly swaps text strings or blocks UI rendering without visual progress feedback.
   - *The Fix*: **Dynamic State Transitions**. Incorporate smooth progress rings during network stack resets, subtle hover glow effects on selectable mode cards, and clean toast notifications upon action completion.

---

### B. Mandatory Plugin UI Design Specifications (插件界面开发十二条铁律)

```mermaid
flowchart TD
    subgraph UI_Architecture [Plugin UI Architecture Standards]
        CardLayout[1. Modular Card Design:<br>CornerRadius=8/10, Subtle Border] --> ThemeBinding[2. 100% Theme Token Binding:<br>No Hardcoded Hex Colors]
        ThemeBinding --> Responsive[3. Adaptive Layouts:<br>Grid Star-Sizing & WrapPanel]
        Responsive --> Typography[4. Clear Visual Hierarchy:<br>Large Bold Metrics vs Muted Labels]
        Typography --> StateFeedback[5. Dynamic State Feedback:<br>Progress Rings & Toasts]
    end
```

1. **Adhere strictly to Host Design System Tokens**: Never hardcode colors (like `#FFFFFF` or `#333333`) or rigid brushes in plugin XAML. Always bind dynamically to host WPF UI resources:
   - Backgrounds: `{DynamicResource ControlFillColorDefaultBrush}`, `{DynamicResource ControlFillColorSecondaryBrush}`
   - Borders: `{DynamicResource ControlStrokeColorDefaultBrush}`
   - Text: `{DynamicResource TextFillColorPrimaryBrush}`, `{DynamicResource TextFillColorSecondaryBrush}`
   - Accents: `{DynamicResource SystemAccentColorPrimaryBrush}`
2. **Standardize Card Geometry**: All feature containers and settings blocks must use rounded `Border` cards with `CornerRadius="8"` or `"10"`, `BorderThickness="1"`, and standard padding (`16px, 14px`).
3. **Limit Vertical Scroll Length**: If a plugin control exceeds 2 screens of vertical height, you MUST split the interface into logical sub-views or tabs (e.g., `Overview`, `Optimization Rules`, `Settings`).
4. **Empty & Loading States**: When telemetry is initializing or no active network adapter is found, display a beautifully designed empty/loading state card with a helpful description and retry button—never leave blank spaces or ugly `null` labels!

---

## 2. Architectural & Threading Governance (核心底层与多线程强约束)

Just like the main application, all plugin codebase contributions must strictly follow our core stability constraints:

### A. WPF UI Thread Affinity & Zero `.ConfigureAwait(false)` (UI 线程安全)
- **Never use `.ConfigureAwait(false)` in Plugin UI/ViewModel Code**: Stripping the synchronization context in plugin controls guarantees `InvalidOperationException` crashes when background downloaders or telemetry timers fire.
- **Defensive Dispatcher Marshaling**: When background services (such as `NetworkAccelerationRuntime` or WMI network adapter monitoring) trigger events (`StateChanged`, `TrafficUpdated`), always marshal UI repaints via `Dispatcher.CheckAccess()` and `Dispatcher.InvokeAsync()`.
- **Safe Process Execution & Teardown**: Network optimization plugins frequently invoke system utilities (`netsh`, `ipconfig`, Winsock resets, DNS flushes). All external process calls must use asynchronous wrappers with cancellation tokens and clean exit-code verification.

### B. Zero-Spam Polling & Low Background Footprint (零冗余监控与极致轻量)
- **No Per-Poll Disk Logging**: Network telemetry loops running every 1–3 seconds must **never** emit trace logs or serialize JSON data on every tick.
- **Resource Cleanup on Unload**: When a plugin tab is closed or unloaded (`UserControl_Unloaded`), explicitly stop background monitoring timers and unsubscribe from static event handlers to prevent memory leaks.

---

## 3. Localization & Pure Text LLM OCR Verification (多语言与 OCR 纯文本大模型质检)

Plugins must support international users and participate in our **Automated OCR & Pure Text LLM Translation Verification Pipeline**:

### A. Eradication of Hardcoded Strings in Plugins (插件杜绝硬编码文案)
- All user-facing text in plugin XAML and C# (including button labels, tooltips, status messages, and error hints) must be extracted into the plugin's local `Resources/Resource.resx` file.
- Use numbered placeholders (`{0}`, `{1}`) with `string.Format(CultureInfo.CurrentCulture, ...)` instead of string concatenation.

### B. Automated 5-Dimension Verification Pipeline (插件 UI 五维自动化质检)
When a plugin is built and validated, our automated UI test driver (`FlaUI` + `PluginWorkbench`) launches the plugin across priority locales (`zh-Hans`, `zh-Hant`, `de`, `es`, `ja`, `fr`, `ru`).

```mermaid
flowchart LR
    Workbench[PluginWorkbench Host] -->|Set Locale & Open Plugin| Screenshot[Capture PNG Bitmap]
    Workbench -->|Dump Control Tree| UIATree[UIAutomation Control Rects]
    Screenshot -->|WinRT OCR Engine| OCRBox[OCR Words + Bounding Boxes X,Y,W,H]
    OCRBox & UIATree -->|JSON Payload| TextLLM[Pure Text LLM Verification Engine]
    TextLLM -->|Check: Truncation / Overflow / Mojibake / Untranslated| AutoFix[Auto-Remediate .resx & XAML]
```

The **Pure Text LLM Verification Engine** evaluates the mapped OCR coordinate data against UIAutomation bounding boxes using 5 strict rules:
1. **Untranslated Detection**: Flagging English fallback text appearing in non-English locales.
2. **Mojibake & Encoding**: Flagging corrupted UTF-8/UTF-16 characters or replacement boxes (`□`).
3. **Broken Placeholders**: Flagging unreplaced format tags (`{0}`, `{1}`) or raw data bindings.
4. **Layout Truncation & Box Overflow**: Comparing OCR text bounding widths against UI container widths. If OCR text ends in ellipses (`...`) or `width >= control_bounds.width * 0.98`, the system automatically flags and modifies the XAML layout (enabling `TextWrapping="Wrap"`, `TextTrimming="CharacterEllipsis"`, or changing fixed widths to dynamic `MinWidth`).
5. **Technical Domain Semantics**: Verifying accurate hardware and network terminology (e.g., translating "Winsock Reset", "DNS Flush", "TCP/IP Stack", "MUX Switch" accurately without literal machine-translation errors).

---

## 4. Open Source Promotion & Marketing Governance (插件仓库宣传推广与上架规范)

When promoting individual plugins or releasing updates to the official plugin store (`store-entry.json`), all contributors and AI agents must follow ethical marketing guidelines:

### A. Key Promotion Channels (核心宣传渠道)
- **Global Developer & Gaming Communities**: GitHub Releases/Discussions, Reddit (`r/LenovoLegion`, `r/GamingLaptops`, `r/windows`), Discord tech channels.
- **Chinese Geek & Tech Communities (国内核心极客社区)**: 吾爱破解 (52Poje)、V2EX、Chiphell (CHH)、B站科技区开源推荐、知乎硬件调优专栏、百度贴吧（拯救者吧、显卡吧、极客吧）、小红书电脑实用工具分享。

### B. Copywriting Elements & Constraints (宣传文案黄金要素与底线约束)
1. **The Hook / Pain Point**: Why use this plugin? (e.g., *"Windows default DNS caching and bloated third-party game boosters consume 200MB+ RAM and run background tracking service. Our Network Acceleration plugin provides pure, 1-click system-level network stack optimization directly inside your toolkit."*)
2. **Truthful & Evidence-Based (严守事实，绝不虚假夸大)**:
   - **Never** make baseless claims like *"Lowers your gaming ping by 100ms"* or *"Guarantees zero packet loss"*.
   - Use technical, accurate descriptions: *"Optimizes Windows TCP/IP stack parameters, flushes DNS cache, and resets Winsock catalog to eliminate local socket bottlenecks without running background VPN or proxy daemons."*
3. **Security & Transparency**: Emphasize that all plugin code is 100% open-source, auditable on GitHub, contains zero telemetry, zero adware, and executes standard Windows network administrative commands cleanly.

---

## 5. Summary Checklist for Plugin Contributors

Before submitting a pull request or promoting an official plugin release:
- [ ] **UI Polish**: Does the XAML use modular cards (`CornerRadius="8"`), responsive sizing (`Width="*"`, `MinWidth`), and host theme brushes without hardcoded hex colors?
- [ ] **Thread Safety**: Have all `.ConfigureAwait(false)` calls been removed from UI/ViewModel paths, and are background UI updates marshaled via `Dispatcher.InvokeAsync()`?
- [ ] **Localization**: Are all user-facing strings extracted to `Resource.resx` without string concatenation?
- [ ] **OCR Verification**: Has the plugin been tested in `PluginWorkbench` under non-English locales to ensure zero text clipping or layout overflow?
- [ ] **Marketing Copy**: Does the store description and release notes adhere to truthful, evidence-based copywriting without exaggerated claims?
