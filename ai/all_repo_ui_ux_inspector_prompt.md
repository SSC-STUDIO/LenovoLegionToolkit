# [OPENCODE / CODEX COMPLIANT] Multi-Repository Autonomous Batch UI/UX Inspection & Visual Polish Protocol

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


You are authorized and instructed to execute a comprehensive, multi-phase batch inspection and visual optimization across our three core repositories: **UniversalDeviceToolkit** (WPF/XAML), **UniversalDeviceToolkit-Plugins** (WPF/XAML), and **Veser** (React/TS/Tailwind/Tauri). Most AI agents focus too much on backend logic and neglect visual excellence鈥攜our mission is to systematically inspect, audit, and polish user interfaces to ensure our applications achieve state-of-the-art, premium visual quality that WOWs users at first glance.

## 0. MANDATORY BATCH EXECUTION & CONTINUOUS PHASE PROTOCOL
1. Read `plugin_ui_and_engineering_governance.md`, `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`, and `Veser/planning/app-review-findings.md`.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: To maximize developer productivity and avoid unnecessary manual prompts between repositories, you must execute a continuous 3-phase batch UI/UX audit across all three codebases. When you complete Phase 1 (`UniversalDeviceToolkit\`), do NOT pause or yield control; immediately transition to Phase 2 (`UniversalDeviceToolkit-Plugins\`), and then Phase 3 (`Veser\`).
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all three repositories have been fully inspected against the 5 UI Pillars, all visual defects logged into `.bugs/1_NEW_REPORTS.md` (or polished inline), and a comprehensive visual polish report is presented.

## 1. 5 CORE UI/UX OPTIMIZATION PILLARS (What to Check & Polish)
- **Pillar A: Visual Hierarchy & Premium Aesthetics (楂樿川鎰熻瑙変笌缇庡瑙勮寖)**:
  - *Veser (React/TS/Tailwind)*: Enforce rich aesthetics! Use curated dark mode palettes, subtle glassmorphism (`backdrop-blur`), smooth linear gradients, modern typography (proper font weight/size hierarchy), and sleek shadow elevation (`shadow-lg`, ambient glow).
  - *UDT & PLG (WPF/XAML)*: Enforce Windows 11 Fluent Design / Mica / Acrylic styling! All cards must use `CornerRadius="8"` or `"10"`, proper inner padding (`Padding="16,12"`), clean margin separation, and `{DynamicResource ControlFillColorDefaultBrush}`. Zero hardcoded hex colors!
- **Pillar B: Dynamic Interaction & Micro-Animations (鐢熷懡鍔涗氦浜掍笌寰姩鐢?**:
  - Every interactive button, card, and toggle MUST have responsive hover transitions (e.g. subtle translateY, scale up, brightness boost) and clear focus rings!
  - When loading data or switching views, enforce smooth fade-in transitions (`CSSTransition`, `framer-motion`, XAML `Storyboard` opacity transitions) and skeleton screens instead of abrupt visual jumps!
- **Pillar C: Adaptive Layout & Anti-Clipping across 78+ Languages (鑷€傚簲涓庢姉婧㈠嚭)**:
  - Scan all UI containers across Chinese, English, German, Japanese, and Russian! Eradicate rigid pixel widths (e.g., `Width="120"`, `w-32` on text containers) that cause text clipping or ellipsis (`...`). Enforce flexible grid star-sizing (`Width="*"`, `flex-1`, `WrapPanel`, `break-words`).
- **Pillar D: Human-Centric Copy & Anti-Robotic Cleansing (鍘绘妧鏈寲涓庢湁娓╁害鏂囨)**:
  - **Zero Backend Telemetry on Home Screens**: NEVER display internal engineering metrics (`KV 缂撳瓨鍛戒腑 76%`, `棣栧瓧寤惰繜 396ms`, `Token 鎴愭湰 $0.04`, `Thread ID`) on user-facing home screens! Move all technical telemetry to a dedicated "Developer / Diagnostics" settings page!
  - **Warm Actionable Copy**: Replace cold technical jargon ("Null ref", "Task executed", "API 200") with warm, user-friendly feedback ("姝ｅ湪涓烘偍浼樺寲绯荤粺...", "璁惧鐘舵€佸钩绋?, "璁剧疆宸插畨鍏ㄤ繚瀛?).
- **Pillar E: UI Defect Logging & Proactive Polish**:
  - Actively audit XAML/React files. If a visual violation is found, log it into `.bugs/1_NEW_REPORTS.md` with `[UI/UX Violation]`, or proactively fix and polish it inline!

## 2. BATCH EXECUTION WORKFLOW
Execute sequentially: `Phase 1: Audit & Polish UDT XAML -> Phase 2: cd ../UniversalDeviceToolkit-Plugins -> Audit & Polish PLG XAML -> Phase 3: cd ../Veser -> Audit & Polish Veser React -> Output Final UI Polish Summary & Yield Control`.

