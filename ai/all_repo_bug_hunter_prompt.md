# [OPENCODE / CODEX COMPLIANT] Multi-Repository Autonomous Bug Hunter, Live Debugger & OCR Optimizer Protocol

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


You are authorized and instructed to execute a comprehensive, multi-phase batch inspection, real-device live debugging, OCR visual audit, and proactive performance optimization sweep across our three core repositories: **UniversalDeviceToolkit**, **UniversalDeviceToolkit-Plugins**, and **Veser**. Your objective is to systematically scan codebases, attach live debuggers, and run OCR character recognition on live UI windows to eliminate all concurrency hazards, memory leaks, runtime warnings, visual text truncations, and **High-ROI Project Optimization Opportunities (`[Optimization]`)**, logging all findings into structured queues.

## 0. MANDATORY BATCH EXECUTION & CONTINUOUS PHASE PROTOCOL
1. Read `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`, `KNOWLEDGE_BASE.md`, and `LIVE_DEBUGGING_AND_OCR_UI_INSPECTOR_PROMPT.md` across repositories.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: To maximize developer productivity and avoid unnecessary manual prompts between repositories, you must execute a continuous 3-phase batch inspection across all three codebases. When you complete Phase 1 (`UniversalDeviceToolkit\`), do NOT pause or yield control; immediately transition to Phase 2 (`UniversalDeviceToolkit-Plugins\`), and then Phase 3 (`Veser\`).
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all three repositories have been fully inspected statically, live debugged, OCR-audited, all defects/warnings logged into their respective `.bugs/1_NEW_REPORTS.md` ledgers, and a comprehensive cross-repository audit summary is presented.

## 1. MULTI-REPO AUDIT PILLARS (Defects, Debug Warnings, OCR Bugs + Optimizations to Hunt For)
- **Pillar A: UDT & PLG (WPF / C# / XAML) Audit & Optimization**:
  - *Defects (`[Bug]`)*: Grep for `.ConfigureAwait(false)` in UI code or ViewModels (Crash Risk!). Hunt for synchronous `Task.Wait()` or `.Result` on async methods. Ensure all WMI queries (`ManagementObjectSearcher`) have `TimeSpan.FromMilliseconds(2500)` timeouts. Hunt for hardcoded hex colors (`#FFFFFF`), rigid pixel widths (`Width="40"`), or hardcoded Chinese/English strings not extracted to `Resource.resx`.
  - *Optimizations (`[Optimization]`)*: Hunt for high-frequency string parsing using `.Split()` or heavy `Regex` and propose `Span<char>` / `SearchValues<T>` zero-allocation slicing. Hunt for buffer allocations (`new byte[...]`, `new List<T>()`) inside polling loops and propose `ArrayPool<T>` pooling. Propose `IAsyncEnumerable<T>` streaming on large sensor/log scans.
- **Pillar B: Veser (Rust / TypeScript / React / Gateway) Audit & Optimization**:
  - *Backend Defects (`[Bug]`)*: Hunt for memory leaks in rate limiters (`SlidingWindowLimiter`), missing request body size limits, unhandled DB transaction rollbacks, or `.unwrap()`/`.expect()` calls that could panic production threads.
  - *Backend Optimizations (`[Optimization]`)*: Hunt for coarse `Arc<Mutex<T>>` / `RwLock<T>` held across `.await` points and propose `DashMap<K, V>` or `moka::future::Cache`. Hunt for heavy JSON serialization across high-frequency IPC and propose zero-copy `Box<RawValue>` or Tauri 2 binary streams. Replace unbounded channels (`mpsc::unbounded_channel`) with bounded backpressure channels (`mpsc::channel`).
  - *Frontend Defects (`[Bug]`)*: Hunt for mojibake (`??`) in `src/locales/`, missing nullish coalescing (`??`/`?.`), unhandled Promise rejections, and check if internal engineering telemetry (`KV 缂撳瓨鍛戒腑`) is exposed on user home screens.
  - *Frontend Optimizations (`[Optimization]`)*: Hunt for long lists (>50 items) lacking `@tanstack/react-virtual` virtualization (`useVirtualizer`). Hunt for heavy UI modals lacking `React.lazy()` dynamic import code-splitting. Mandate `React.memo` and `useCallback`/`useMemo` on child components subscribed to high-frequency IPC state streams.
- **Pillar C: Real-Device Live Debugging (`[Live-Debug-Warning]`)**:
  - Launch applications in **Debug Mode** (`dotnet run -c Debug` / `cargo tauri dev --debug`) and attach `TraceListener` or console logger.
  - Trace step-by-step through startup and WMI/IPC initialization.
  - **Zero-Warning & Zero-Exception Mandate**: Capture and immediately fix on the spot every compiler warning (`CS8600`, `CS0618`), XAML DataBinding runtime error (`System.Windows.Data Error: 40`), or swallowed exception (`catch (Exception ex)` without fallback).
- **Pillar D: OCR & Vision UI Visual Inspection (`[UI-OCR-Bug]`)**:
  - Capture desktop/window screenshots (`screencapture` / `.CopyFromScreen()`) and run **OCR + Multimodal Vision** analysis across `zh-Hans / en / ja / de / ru` views.
  - **Detect & Fix Text Truncation (`Networ...`)**: Replace rigid `Width="xx"` with `Star` or `Auto / MinWidth` sizing.
  - **Detect & Fix Mojibake & Raw Keys (`??` / `activity.xxx`)**: Fix unrendered translation placeholders or stray CJK text immediately.
  - **Detect & Fix Multi-DPI Overlap**: Verify element spacing under 125%/150% Windows scaling.
- **Pillar E: Structured Bug & Optimization Ledger (`.bugs/1_NEW_REPORTS.md`)**:
  - Whenever an issue is found that requires team tracking or multi-step refactoring, write a structured report directly into that repository's `.bugs/1_NEW_REPORTS.md` ledger using this exact format:
    `- [ ] **[ID-xxx]** \`[Bug]\` \`[Category]\` Short description in \`File.ext:Lxx\`. *Root Cause*: Why it violates rules. *Suggested Fix*: Copy-pasteable snippet showing exact remediation.`
    `- [ ] **[ID-xxx]** \`[Live-Debug-Warning]\` \`[WPF/Runtime]\` \`File.xaml:Lxx\` threw runtime XAML DataBinding Error \`Property 'X' not found\`. *Fix*: Added observable property.`
    `- [ ] **[ID-xxx]** \`[UI-OCR-Bug]\` \`[I18n/Clipping]\` OCR detected truncated text \`Networ...\` on \`File.xaml:Lxx\` under 150% DPI. *Fix*: Replaced rigid width with Star sizing.`
    `- [ ] **[ID-xxx]** \`[Optimization]\` \`[Category]\` Short description in \`File.ext:Lxx\`. *Optimization Rationale*: Why this improves perf/memory. *Suggested Refactor*: Copy-pasteable snippet (\`Span<T>\` / \`DashMap\` / \`Virtualizer\`).`

## 2. BATCH EXECUTION WORKFLOW
Execute sequentially: `Phase 1: Scan, Debug & OCR UDT -> Phase 2: cd ../UniversalDeviceToolkit-Plugins -> Scan, Debug & OCR PLG -> Phase 3: cd ../Veser -> Scan, Debug & OCR Veser -> Output Final Triage Summary & Yield Control`.

