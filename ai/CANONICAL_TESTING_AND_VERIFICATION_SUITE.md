# [CANONICAL TESTING & VERIFICATION SUITE] The Pre-Configured 3-Tier Verification Engine

*(Version 2.0.0-TEST 鈥?Standardized Verification Protocol across all 16 Projects under D:\EliuaK_Csy\Working-Paper\My-Program\<ProjectName>)*

Whenever any agent (Watchdog, Bug Reporter, /goal unattended agent, or Subagent) modifies source code, prose, XAML, or assets, **IT MUST EXCLUSIVELY USE THIS EXACT PRE-CONFIGURED 3-TIER TESTING SUITE** before marking any work [x] or committing to Git.

---

## 馃彈锔?Tier 1: Automated Unit/Integration & Static AST Gate ([Static-Check])

Depending on the project's exact engineering stack, run the pre-configured automated test suites:

### 1. .NET 10 / C# / WPF Projects (UniversalDeviceToolkit, UniversalDeviceToolkit-Plugins, Nilesoft-Shell)
- **Automated Test Run**: Execute dotnet test -v minimal across all .Tests projects (e.g., verifying SensorsNullFallbackAndThreadSafetyTests.cs, AbstractSettings overrides, and L131 unescaped JSON literal safety).
- **Strict Build Verification**: Run dotnet build -warnaserror to guarantee zero CS8600, CS0618, or nullability warnings.

### 2. Rust / Tauri 2.0 / React Projects (Veser, Survivor-Arena, Greedy-Snake-2026)
- **Rust Workspace Tests**: Execute cargo check --workspace --message-format=json and cargo test --workspace.
- **Frontend TS/React Tests**: Execute pnpm test / 
px vitest run and 
px tsc --noEmit to verify type boundaries and zero @tanstack/react-virtual regression.

### 3. Go / Python / C++ High-Throughput & AI Projects (Ai-Model-Gateway, Athena-Owl-FC, Home-Cloud-Server, Plants-Disease-Detection)
- **Go / Python Test Suites**: Execute go test -race -v ./... (verifying zero race conditions across concurrent maps) and pytest -v across all test directories (	ests/).
- **SITL / Simulation Loops**: For drone firmware (Athena-Owl-FC), execute automated SITL simulation scripts (sitl_test/) to verify flight dynamics without memory allocation (malloc/free) faults.

---

## 馃悶 Tier 2: Real-Device Live Debug Mode & Trace Verification ([Live-Debug-Trap])

Never rely solely on static compilation! Launch the software in **Debug Mode** and enforce runtime verification:

### 1. WPF XAML DataBinding Trace Listener Trap
When executing .NET WPF apps (UDT, Plugins), verify that the pre-configured DataBinding trace listener catches zero warnings:
`csharp
#if DEBUG
System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Warning;
System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
#endif
`
If the debug output contains BindingExpression path error: 'xxx' property not found, **it is considered a test failure** and must be resolved immediately!

### 2. WMI & Async Thread Timeout Trap
All hardware sensor queries (ManagementObjectSearcher, WQL) must be verified to complete or abort within TimeSpan.FromMilliseconds(2500) via Task.Run() isolation. Synchronous .Result thread blocking is a hard test failure.

---

## 馃憗锔?Tier 3: Multimodal Vision & OCR UI Visual Matrix ([UI-OCR-Matrix])

When verifying UI rendering (WPF, React, Godot, Phaser), execute automated screen capture and inspect via our pre-configured **5-Locale 脳 3-DPI Matrix**:

### 1. The 3-DPI Scaling Check
Inspect rendering outputs under:
- 100% DPI (96 DPI) 鈥?Standard Baseline
- 125% DPI (120 DPI) 鈥?High-Density Laptop Screen
- 150% DPI (144 DPI) 鈥?Ultra-High Density Display

### 2. The 5-Locale Internationalization Check (M-014)
Inspect every view under all 5 canonical localization targets:
- en (English)
- zh-Hans (Simplified Chinese)
- ja (Japanese)
- de (German 鈥?multi-syllable expansion check)
- u (Russian 鈥?Cyrillic character width check)

### 3. The 4 Quantitative OCR Visual Defect Rules
- **[UI-OCR-Clipping]**: Any text truncated with ellipses (Networ..., Preflig...) or clipped numbers (450 -> 45 due to rigid Width='40') is a **TEST FAILURE**. Must replace with Width='*' or MinWidth.
- **[UI-OCR-Mojibake]**: Any unrendered translation token (ctivity.kindPreflight, {{count}}), garbled encoding (??, 鈻♀枴), or stray CJK character inside English/German views is a **TEST FAILURE**.
- **[UI-OCR-Collision]**: Any overlapping bounding box under 150% DPI is a **TEST FAILURE**.
- **[UI-OCR-Contrast]**: Any WCAG AA contrast violation (< 4.5:1 ratio) is a **TEST FAILURE**.

---

## 馃摉 Special Domain Override: Novel Creative Universe Verification Suite
Inside D:\EliuaK_Csy\Working-Paper\My-Program\Novel\, the pre-configured testing suite operates as the **Literary Continuity & Quality Audit Engine**:
1. **Automated Repetition Pruning Test**: Run word frequency scans across 姝ｆ枃/ to guarantee zero excessive adverb repeats (鍗佸垎, 涓嶇敱寰梎, 杩欎竴鍒籤, 鏁翠釜浜篳).
2. **Timeline & Realm Continuity Verification**: Cross-reference 銆婂懡杩愭媿鍗栬銆?绗簩鍗烽樁娈礎浣滄垬琛?md, 瑙掕壊鍗?, and 璁惧畾/ to verify zero character ability or timeline contradictions across鍗?(volumes).
3. **Dialogue Cadence Check**: Verify character voices match exact universe rules (銆婇潚寮﹁礋闆€媊 classical rhythm vs. 銆婃垜娌¤杩囧畬鏁寸殑钃濊壊銆媊 cyberpunk alienation).
