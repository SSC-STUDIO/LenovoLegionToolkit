# Live Debugging & OCR UI Visual Inspector Prompt (Real-Device Execution & Step-by-Step Trace Protocol)

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


This document provides a specialized, industrial-grade prompt designed for **OpenCode**, **Codex**, **Claude Code**, and **Cursor / Multi-Agent Watchdogs** to transition from static code scanning (`AST / grep / unit tests`) into **Dynamic Live Debugging & OCR-Driven UI Visual Auditing** across our **Tri-Repo Ecosystem**:
1. `UniversalDeviceToolkit` (Main C#/WPF Repo)
2. `UniversalDeviceToolkit-Plugins` (WPF Plugin Repo)
3. `Veser` (Rust/Tauri/React AI Productivity Software Repo)

---

## 鈿?Section 1: Ultra-Compact Prompt (绮剧畝瀹炴垬鐗?- 涓撲负鑷姩鍖栧鏅鸿兘浣撲笌璋冭瘯缁堢璁捐)

*(Copy and paste this prompt directly into your autonomous agent CLI to initiate real-device debugging and OCR UI inspection)*

```markdown
# [LIVE DEBUG & OCR INSPECTOR] Real-Device Step-by-Step Execution Trace & Visual Defect Hunter

You are operating as a Live-Debugging & OCR UI Inspector monitoring our THREE core repositories:
1. Main Repo: `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit\`
2. Plugin Repo: `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins\`
3. Veser Repo: `D:\EliuaK_Csy\Working-Paper\My-Program\Veser\`

## 1. MANDATORY GOVERNANCE & QUEUE CHECK
Before running, read `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md` and `KNOWLEDGE_BASE.md`. Check `.bugs/1_NEW_REPORTS.md` and `.bugs/2_IN_PROGRESS.md` to avoid duplicating already reported issues.

## 2. PHASE 1: REAL-DEVICE DEBUG LAUNCH & STEP-BY-STEP TRACING
Do not rely solely on static code search! You must launch the actual applications in **Debug Mode** and attach debugging/logging hooks (`dotnet run -c Debug`, `cargo tauri dev --debug`, `.NET Debugger`, or `TraceListener`):
- **Step-by-Step Execution Audit**: Trace execution flow through critical startup paths (`App.OnStartup`, `MainWindow` construction, `ManagementObjectSearcher` WMI queries, `Veserd` local server initialization, and Tauri IPC bridges).
- **Zero-Warning & Zero-Exception Mandate**: Capture and analyze:
  1. All compiler/build warnings (`CS8600`, `CS0618`, Rust `dead_code`/`unused_variables`).
  2. All XAML runtime DataBinding warnings (`System.Windows.Data Error: 40... BindingExpression path error`).
  3. All swallowed exceptions (`catch (Exception ex)` without fallback or logging) or IPC timeout drop warnings.
- **Immediate Remediation (`Auto-Solve`)**: Whenever a runtime warning, binding error, thread stall (`.Result` / `Task.Wait`), or silent exception is hit during debugging, **DO NOT skip it**! Open the exact source file immediately, fix the root cause on the spot, and re-run to verify zero warnings remain!

## 3. PHASE 2: OCR & MULTIMODAL VISION UI INSPECTION
Once the live application window is rendered on screen, capture desktop/window screenshots (`screencapture`, `PowerShell Graphics.CopyFromScreen`, or browser/canvas capture tools) and execute **OCR (Optical Character Recognition) + Multimodal Vision Analysis** on every UI view:
- **Text Truncation & Clipping (`[OCR-Clipping]`)**: Scan all rendered labels, buttons, cards, and data grids. Detect truncated text ending in ellipses (`Networ...`, `Temp...`) or vertically/horizontally clipped numbers (e.g., `450` showing as `45` due to `Width="40"`).
- **Layout Overlap & DPI Collision (`[OCR-Layout]`)**: Inspect UI alignment under simulated or real Windows scaling (125%, 150%, 200%). Flag any text colliding with icons, overlapping bounding boxes, or squeezed controls.
- **Mojibake & Placeholder Leaks (`[OCR-I18n]`)**: Run character recognition across Chinese (`zh-Hans`), English (`en`), Japanese (`ja`), German (`de`), and Russian (`ru`) views. Detect any garbled characters (`??`, `鈻♀枴`, `茂驴陆`), unrendered translation placeholders (`activity.workflowSchema.kindPreflight`, `{{count}}`), or stray hardcoded CJK text inside English/European locale views.
- **Contrast & Visual Hierarchy (`[OCR-Contrast]`)**: Verify dark mode and light mode contrast ratios (WCAG AA compliance). Flag unreadable dark-gray text on dark backgrounds or white-on-white button states.

## 4. PHASE 3: REMEDIATION & LEDGER SYNC
1. **Immediate Code Fixes**: For every warning or OCR-detected visual bug, immediately modify the corresponding `.cs`, `.xaml`, `.rs`, or `.tsx` file (e.g., replace fixed `Width="40"` with `Width="*" / MinWidth="60"`, extract raw strings to `Resource.resx` / `locales/*.ts`, or fix broken XAML data bindings).
2. **Queue Logging**: For any complex architectural refactoring that requires long-term team collaboration, append a structured ticket directly to `.bugs/1_NEW_REPORTS.md`:
```markdown
- [ ] **[ID-xxx]** `[Live-Debug-Warning]` `[Category]` `File.cs:Lxx` triggered runtime XAML Binding Error `BindingExpression path error: 'CpuLoad' property not found on 'MainViewModel'`. *Fix*: Added observable `CpuLoad` property to `MainViewModel.cs`.
- [ ] **[ID-xxx]** `[UI-OCR-Bug]` `[Category]` OCR detected clipped text `Networ...` on `ViveToolPage.xaml:L88` under 150% DPI. *Fix*: Replaced rigid `Width="80"` with `Width="Auto" MinWidth="100"`.
```

## 5. CONTINUOUS EXECUTION LOOP
Loop: `Launch Debug Build -> Trace Execution & Solve Runtime Warnings -> Capture UI & Run OCR Visual Audit -> Auto-Fix UI/Binding Defects -> Re-test -> Repeat`. Never stop until all three software executables run with 0 runtime warnings, 0 exceptions, and 100% pristine visual UI!
```

---

## 馃摉 Section 2: Full Detailed Architecture & Execution Trace Protocol

### A. Why Live Debugging + OCR Vision is the Ultimate Quality Standard
While static AST grep and automated unit tests (`dotnet test` / `cargo test`) catch compilation breaks and unit logic errors, they **cannot detect**:
1. **Runtime XAML DataBinding Failures**: WPF silently fails when `Binding Path=PropertyThatDoesNotExist` is evaluated at runtime, leaving UI fields blank or throwing hidden binding warnings to `TraceListener`.
2. **OS Environment & WMI Permission Traps**: Real hardware queries (`ManagementObjectSearcher`, NVAPI, Lenovo Legion WMI calls) behave differently on live Windows machines depending on admin privileges, power modes, and driver availability.
3. **High-DPI & Multi-Locale UI Clipping**: A button that looks fine in English with `Width="100"` will instantly clip or overlap when rendered in German (`Netzwerkbeschleunigungskontrolle`) or under 150% Windows display scaling.

By combining **Live Debug Attachment** with **OCR Visual Screenshot Analysis**, our autonomous agents achieve the same perceptual and debugging depth as a senior human quality engineer.

```mermaid
flowchart TD
    Start[Agent Loop: Live Debug & OCR Watchdog] --> BuildDebug[Compile & Launch in Debug Mode<br>dotnet run -c Debug / cargo tauri dev --debug]
    
    subgraph LiveDebugLayer [Phase 1: Step-by-Step Execution Trace & Warning Capture]
        BuildDebug --> AttachDebugger[Attach Debugger & Console TraceListener]
        AttachDebugger --> StepTrace[Step-over critical init hooks:<br>App.OnStartup / WMI / IPC Router]
        StepTrace --> CaptureWarnings[Capture Runtime Warnings:<br>- XAML Binding 40 Errors<br>- Swallowed Exceptions<br>- Thread Synchronous Blocks]
        CaptureWarnings --> AutoSolveDebug[Auto-Solve in Source Code Immediately]
    </StepTrace>
    
    subgraph OCRVisionLayer [Phase 2: OCR & Vision Visual Inspection]
        AutoSolveDebug --> CaptureScreen[Capture Desktop / Window Screenshot<br>screencapture / Graphics.CopyFromScreen]
        CaptureScreen --> OCRAnalysis[Run OCR & Multimodal Vision Analysis]
        OCRAnalysis --> DetectClipping[Detect Text Truncation (Networ...)]
        OCRAnalysis --> DetectMojibake[Detect Mojibake (?? / 鈻♀枴) & Raw Keys]
        OCRAnalysis --> DetectOverlap[Detect Layout Overlap under 150% DPI]
        DetectClipping & DetectMojibake & DetectOverlap --> AutoSolveUI[Auto-Solve XAML/React Layout & I18n Keys]
    end
    
    subgraph VerifyAndQueue [Phase 3: Verification & Queue Sync]
        AutoSolveUI --> ReBuild[Re-run Debug Build & Re-verify Screenshot]
        ReBuild --> CheckClean{0 Warnings & Clean OCR?}
        CheckClean -->|No| AttachDebugger
        CheckClean -->|Yes| LogLedger[Append remaining items to .bugs/1_NEW_REPORTS.md]
    end
    
    LogLedger --> Sleep[Sleep / Await Next Trigger]
    Sleep --> Start
```

---

### B. Specific Debugging Tools & Commands for Sub-Agents

#### 1. UniversalDeviceToolkit & Plugins (.NET 10 / WPF / C#)
- **Launch Command**: `dotnet run --project D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit\UniversalDeviceToolkit\UniversalDeviceToolkit.csproj -c Debug`
- **Capturing XAML Binding Errors Programmatically**:
  Agents must verify or inject a `TraceListener` inside `App.xaml.cs` (`OnStartup`) during debug runs to pipe all XAML binding warnings directly to standard error or a log file:
  ```csharp
  #if DEBUG
  System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Warning;
  System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
  #endif
  ```
- **Step-by-Step Tracing Priorities**:
  1. `App.xaml.cs` -> `OnStartup()`: Check if unhandled app domain exception handlers are wired (`AppDomain.CurrentDomain.UnhandledException`).
  2. `MainWindowViewModel.cs`: Trace property initialization. Ensure no synchronous `.Result` calls on async IPC or WMI tasks during constructor execution.
  3. `HardwareMonitorService.cs` / `ManagementObjectSearcher`: Verify WMI queries execute asynchronously inside `Task.Run()` with explicit 2500ms timeout tokens.

#### 2. Veser (Rust / Tauri 2.0 / React Frontend)
- **Launch Command**: `cd D:\EliuaK_Csy\Working-Paper\My-Program\Veser\apps\desktop && npx tauri dev --debug` (or run `veserd` backend standalone via `cargo run --bin veserd`).
- **Rust Backend Debug Tracing**:
  Check `RUST_LOG=debug,veser=trace` output for any dropped IPC channels, sqlite database lock contention, or P2P handshake retries.
- **Frontend Chrome DevTools / Console Error Capture**:
  When Tauri webview opens, attach to the browser debug socket or inspect console output for `Uncaught (in promise)`, React hydration warnings, or TanStack query key mismatches.

---

### C. Specific OCR Visual Audit Checklists

When analyzing window screenshots with OCR / Vision LLM tools, verify every single UI panel against these strict visual rules:
1. **Zero Text Truncation (`No Ellipses / No Clipped Numbers`)**:
   - *Violation*: `CPU Load: 4...%` or `Network Acceleration...` (clipped).
   - *Fix*: Remove `Width="xx"` on the XAML `TextBlock` / React `div`; use `TextTrimming="None" TextWrapping="Wrap"` or flex/grid `Star` sizing (`Width="*"`).
2. **100% I18n Rendering (`No Placeholder Raw Keys`)**:
   - *Violation*: OCR sees `activity.workflowSchema.kindPreflight` or `chat.veserd.status.passed` on screen instead of `Preflight Check` or `棰勬`.
   - *Fix*: Check the i18n locale file (`Resource.resx` or `src/locales/en/*.ts`), verify key spelling, and ensure `useTranslation()` / `DynamicResource` binding is correctly attached.
3. **Multi-DPI Layout Integrity (`No Element Collisions at 150% Scaling`)**:
   - *Violation*: At 150% DPI, label text overlaps with the right-side toggle switch or input box border.
   - *Fix*: Replace fixed coordinate margins (`Margin="0,0,120,0"`) with proper container panels (`Grid` columns with `Auto`/`Star` widths, `DockPanel`, or `WrapPanel`).

