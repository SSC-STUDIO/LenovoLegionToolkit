# Claude Code Goal Mode Prompts: Ultra-Compact & Full Versions (Road to 100 Stars)

This document provides both **Ultra-Compact (<1500 chars)** and **Full Detailed** goal-mode prompts for **Claude Code** (or Cursor / Agent CLI). The compact versions are specifically engineered to fit well within any **4000-character / token input limit** while retaining 100% of their authoritative guidance by leveraging the governance documents already present in the project root directories.

---

## ⚡ Section 1: Ultra-Compact Prompts (精简高效版 - 专为 4000 字限制与 BUG.md 修复设计)

### 🚀 Prompt 1 (Compact): Main Repository Optimization, BUG.md Fix & Marketing (100+ Stars)
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit\`

```markdown
# [GOAL MODE] Autonomous Full-Stack Maintenance, BUG.md Fix & 100+ Star Promotion

You are in autonomous Goal Mode. Your North Star objective is to maintain, fix bugs, optimize, and promote **Universal Device Toolkit** until reaching **100+ GitHub Stars**.

## 1. MANDATORY INGESTION
Before writing code or running commands, read and obey all rules in:
1. `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit\AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`
2. `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit\implementation_plan.md` & `agent_optimization_guide.md`
3. `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit\walkthrough.md` & `task.md`

## 2. AUTONOMOUS EXECUTION PILLARS
- **Pillar 0 (Priority 1): BUG.md Triage & Fix**: Check if `BUG.md` exists and has open items from the automated bug reporter. If yes, solving `BUG.md` takes Priority 1! Root-cause against our 4 Pillars (Threading, WMI, Polling, UI Overflow), refactor code, verify via tests/OCR, update `KNOWLEDGE_BASE.md`, and mark `[x]` in `BUG.md`.
- **Pillar A: Thread Safety & WMI Protection**: Zero `.ConfigureAwait(false)` in WPF UI/ViewModels. Guard UI updates with `Dispatcher.CheckAccess()` / `Dispatcher.InvokeAsync()`. Enforce async WMI queries with 2500ms timeouts against RDP freezes.
- **Pillar B: 78+ Language Localization**: Zero hardcoded strings. Extract all UI text to `Resource.resx`. Use parameterized format strings (`{0}`, `{1}`)—never string concatenation!
- **Pillar C: OCR UI Verification**: Execute FlaUI + WinRT OCR automated tests across priority locales (`zh-Hans`, `de`, `ja`, `ru`). Autonomously fix `.resx` translations and XAML layouts (`TextWrapping="Wrap"`, `MinWidth`) when OCR detects text clipping or mojibake.
- **Pillar D: 100-Star Marketing**: Write truthful, evidence-based copy highlighting our `<30MB` RAM, `<1s` startup, zero telemetry, and native WPF edge over bloated OEM suites. Publish across GitHub, Reddit (`r/LenovoLegion`, `r/windows`), V2EX, 52Poje, Chiphell, Bilibili, and Zhihu.

## 3. EXECUTION LOOP
Loop continuously: `Ingest BUG.md -> Refactor -> Build (0 errors) -> OCR Verify -> Promote -> Handover/Iterate`. Do not stop until achieving 100+ Stars!
```

---

### 🎨 Prompt 2 (Compact): Plugin Repo UI Redesign, BUG.md Fix & Marketing (100+ Stars)
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins\`

```markdown
# [GOAL MODE] Autonomous Plugin Redesign, BUG.md Fix & 100+ Star Promotion

You are in autonomous Goal Mode. Your North Star objective is to refactor, fix bugs, redesign, and promote **UniversalDeviceToolkit-Plugins** until reaching **100+ GitHub Stars**.

## 1. MANDATORY INGESTION
Before writing code or XAML, read and obey all rules in:
1. `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins\AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`
2. `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins\plugin_ui_and_engineering_governance.md`
3. `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins\implementation_plan.md` & `agent_optimization_guide.md`
4. `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins\walkthrough.md` & `task.md`

## 2. AUTONOMOUS EXECUTION PILLARS
- **Pillar 0 (Priority 1): BUG.md Triage & Fix**: Check if `BUG.md` exists and has open items from the automated bug reporter. Solving `BUG.md` takes Priority 1! Root-cause against our 4 Pillars, fix XAML/code, verify in `PluginWorkbench`, update `KNOWLEDGE_BASE.md`, and mark `[x]` in `BUG.md`.
- **Pillar A: Total UI/UX Redesign (`NetworkAcceleration`)**: Refactor `NetworkAccelerationControl.xaml` (currently 600+ lines in a single stack panel). Implement modular `TabControl` tabs (Dashboard vs Settings), responsive `Grid` star-sizing (`Width="*"`), prominent telemetry metric cards for speeds, and micro-animations/progress rings. Ensure 100% binding to host theme brushes (`ControlFillColorDefaultBrush`)—zero hardcoded hex colors or fixed pixel widths! Apply across all plugins.
- **Pillar B: Thread Safety & Async Timeouts**: Zero `.ConfigureAwait(false)` in UI code. Marshal callbacks via `Dispatcher.InvokeAsync()`. Wrap system calls (`netsh`, `ipconfig`, Winsock resets) in strict async timeouts.
- **Pillar C: Workbench OCR Verification**: Extract hardcoded strings to `Resource.resx`. Launch plugins in `PluginWorkbench` (`.\llt-plugin.cmd preview`) across locales (`zh-Hans`, `de`, `ja`, `ru`). Use WinRT OCR + UIAutomation bounding boxes to fix text truncation and mojibake.
- **Pillar D: Store Promotion**: Polish `plugin.manifest.json` metadata. Write ethical, compelling copy highlighting 1-click network stack optimization without background VPN bloat. Promote on GitHub, Reddit, 52Poje, V2EX, Chiphell, Bilibili, and Zhihu.

## 3. EXECUTION LOOP
Loop continuously: `Ingest BUG.md -> Redesign XAML -> Refactor -> Build (.\llt-plugin.cmd build) -> Verify in Workbench -> Promote -> Handover/Iterate`. Do not stop until achieving 100+ Stars!
```

---

## 📖 Section 2: Full Detailed Reference Versions

*(See previous sections for expanded diagnostic context and complete rules)*
