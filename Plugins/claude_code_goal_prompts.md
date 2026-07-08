# Claude Code & Goal Mode Prompts: Ultra-Compact & Full Versions (Ecosystem & Games)

This document provides both **Ultra-Compact (<1500 chars)** and **Full Detailed** goal-mode prompts for **Claude Code** (or Cursor / Codex / OpenCode / Agent CLI) across our entire **Ecosystem & Games**:
1. `UniversalDeviceToolkit` (Main Repo)
2. `UniversalDeviceToolkit-Plugins` (Plugin Repo)
3. `Veser` (Dual-Engine AI Productivity Software Repo - Desktop, Mobile & Video Commercials)
4. `Greedy-Snake-2026` (Godot 4.6 Roguelike Arena Survival Game Repo)
5. `Multi-Board-Game-Collection` (5-in-1 Board Game Suite with Three.js 3D & LLM Coach)

---

## ⚡ Section 1: Ultra-Compact Prompts (精简高效全能版 - 专为 4000 字限制、拟人化 UX、连续多阶段与防请示设计)

### 🚀 Prompt 1 (Compact): Main Repo Maintenance, .bugs/ Queue Fix & Marketing
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit\`

```markdown
# [OPENCODE / CODEX COMPLIANT] UniversalDeviceToolkit Autonomous Batch Maintenance & Engineering Evolution Protocol

You are authorized and instructed to execute a continuous multi-phase batch evolution for **Universal Device Toolkit** (`D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit\`). Your mission is to triage bug queues, enforce thread safety, expand unit test coverage toward 99%+, polish WPF XAML UI, and update marketing copy until reaching **1,000+ GitHub Stars (1k Stars)**.

## 0. MANDATORY BATCH EXECUTION & ZERO-CONFIRMATION CHAINING
1. Read `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`, `KNOWLEDGE_BASE.md`, `agent_optimization_guide.md`, and `task.md`.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: To maximize productivity and avoid unnecessary manual prompts, execute a continuous 4-phase batch maintenance. When you finish Phase 1, do NOT pause or yield control; immediately transition to Phase 2, then Phase 3, and Phase 4!
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all 4 phases are executed, all unit tests pass (`dotnet test` with 0 errors), and all verified chunks are cleanly committed to git (`git commit -m "..."`).
4. **🚫 STAR-POLLING PREVENTION & HUMAN BOUNDARY RULE**: Do NOT get stuck polling `gh api` or checking star counts! External posting on Reddit, HN, V2EX, Zhihu, and Bilibili requires manual human account authentication. Once you build clean, pass all tests, empty the bug queue, and update promotion copy in `Docs/`, your programmatic job is 100% COMPLETE!
5. **🚀 PROACTIVE EVOLUTION RULE (主动功能扩展·UT测试强化·文档精磨铁律)**: When `.bugs/1_NEW_REPORTS.md` is empty and all tests pass, DO NOT treat the repository as 'finished'! Proactively switch to: (1) Writing new unit tests in `LenovoLegionToolkit.Tests` to expand test coverage toward 99%+, (2) Adding new toolkit features and Windows 11 24H2 integrations, and (3) Polishing `README.md`, developer guides, and multilingual localizations (`zh-Hans`, `de`, `ja`, `ru`)! Never stop evolving!

## 1. 4 CORE AUTONOMOUS EXECUTION PHASES
- **Phase 1: Multi-Document Bug Queue (`.bugs/`) Triage & Zeroing**: Check `.bugs/1_NEW_REPORTS.md`. Claim an open bug by moving it to `.bugs/2_IN_PROGRESS.md` with `[CLAIMED by <Agent-ID>]`. Fix C#/XAML code against our engineering governance, verify via `dotnet test`, move to `.bugs/3_RESOLVED.md`, and log rule in `KNOWLEDGE_BASE.md`.
- **Phase 2: Thread Safety, WMI Protection & XAML Governance**: Eradicate `.ConfigureAwait(false)` from WPF UI/ViewModels (Crash Risk!). Guard UI updates with `Dispatcher.CheckAccess()` / `Dispatcher.InvokeAsync()`. Enforce async WMI queries (`ManagementObjectSearcher`) with 2500ms timeouts (`GetAsyncWithTimeout(2500)`). Ensure all XAML cards use `CornerRadius="8"` or `"10"`, `{DynamicResource ControlFillColorDefaultBrush}`, zero rigid pixel widths, and zero hardcoded hex colors.
- **Phase 3: Proactive Unit Test & OCR Layout Expansion**: Run `dotnet test` across all suites (`[CollectionDefinition(DisableParallelization = true)]`). When `.bugs/` is empty, proactively write new unit tests in `LenovoLegionToolkit.Tests` covering null-sensor fallbacks, corrupted JSON settings recovery, and WinRT OCR visual assertions (`assert text clipping == 0` across `zh-Hans`, `de`, `ja`, `ru`).
- **Phase 4: Marketing Copy Polish & Batch Checkpoint**: Update truthful copy in `Docs/` highlighting `<30MB` RAM, `<1s` startup, and zero telemetry. Run `dotnet build` and `dotnet test` to confirm 0 errors / 0 warnings. Execute `git add -A; git commit -m "feat/fix(udt): ..."` for each verified chunk! Once all 4 phases complete cleanly, present a concise summary report and yield control!
```

---

### 🎨 Prompt 2 (Compact): Plugin Repo Redesign, .bugs/ Queue Fix & Marketing
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins\`

```markdown
# [OPENCODE / CODEX COMPLIANT] UniversalDeviceToolkit-Plugins Autonomous Batch Evolution & UI Redesign Protocol

You are operating in autonomous Goal Mode as the Chief Plugin Architect, WPF XAML UI Designer, and Quality Watchdog for **UniversalDeviceToolkit-Plugins** (`D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins\`). Your objective is to systematically redesign plugin interfaces, expand unit test coverage toward 99%+, enforce thread safety, eradicate WMI timeouts/deadlocks, and prepare our marketing copy to achieve **1,000+ GitHub Stars (1k Stars)**!

## 0. MANDATORY BATCH EXECUTION & CONTINUOUS PHASE PROTOCOL
1. Read `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`, `plugin_ui_and_engineering_governance.md`, `KNOWLEDGE_BASE.md`, and `task.md`.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: To maximize developer productivity and avoid unnecessary manual prompts between plugins or tasks, execute a continuous 4-phase batch maintenance across the plugin library! When you complete Phase 1, do NOT pause or yield control; immediately transition to Phase 2, then Phase 3, and Phase 4!
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all 4 phases in the current batch are executed, all plugin unit tests pass (`dotnet test` with 0 errors), and all verified chunks are cleanly committed to git!
4. **🚫 STAR-POLLING PREVENTION & HUMAN BOUNDARY RULE**: Do NOT get stuck polling `gh api` or checking star counts! External posting on Reddit, HN, V2EX, Zhihu, and Bilibili requires manual human account authentication. Once you have built clean (0 errors), passed all tests, emptied the bug queue, and generated/updated all promotion copy in `Docs/`, your programmatic promotion job is 100% COMPLETE! Do NOT waste tokens waiting for stars to climb.
5. **🚀 PROACTIVE EVOLUTION RULE (主动功能扩展·UT测试强化·文档精磨铁律)**: When `.bugs/1_NEW_REPORTS.md` is empty and all existing tests pass, DO NOT treat the repository as 'finished'! Proactively switch to: (1) Writing new unit tests (UT) to expand test coverage toward 99%+, (2) Adding new plugin features and UI micro-animations, and (3) Polishing `README.md`, developer guides, and multilingual localizations! Never stop evolving!

## 1. 4 CORE AUTONOMOUS EXECUTION PHASES
- **Phase 1: Multi-Document Bug Queue (`.bugs/`) Triage & Zeroing**: Check `.bugs/1_NEW_REPORTS.md`. Claim an open bug by moving it to `.bugs/2_IN_PROGRESS.md` with `[CLAIMED by <Agent-ID>]`. Fix XAML/C# code, verify via `dotnet test`, move to `.bugs/3_RESOLVED.md`, and log rule in `KNOWLEDGE_BASE.md`.
- **Phase 2: Total UI/UX Redesign (`ViveTool`, `ShellIntegration`, `CustomMouse`, `BatteryHealth`)**: Refactor legacy XAML controls following our governance: CornerRadius 8/10 cards, `{DynamicResource ControlFillColorDefaultBrush}` theme binding (ZERO hardcoded hex colors), `TextWrapping="Wrap"`, `wpfui:SymbolIcon` (ZERO emojis), and replace all `MessageBox.Show` with inline status TextBlocks!
- **Phase 3: Thread Safety, WMI Protection & Proactive UT Expansion**: Zero `.ConfigureAwait(false)` in UI code. Marshal callbacks via `Dispatcher.InvokeAsync()`. Ensure all WMI queries (`ManagementObjectSearcher`) have 2500ms timeouts! Ensure all tests use `[CollectionDefinition(DisableParallelization = true)]`. When `.bugs/` is empty, proactively write new unit tests in `UniversalDeviceToolkit-Plugins.Tests` covering null-sensor fallbacks, corrupted JSON settings recovery, and WinRT OCR visual assertions!
- **Phase 4: Localization (i18n), Verification & Batch Checkpoint**: Extract hardcoded strings to `Resource.resx` (`zh-Hans`, `de`, `ja`, `ru`). Launch plugins in `PluginWorkbench` across locales. Run `dotnet build` and `dotnet test` (assert 0 errors / 0 warnings and 100% passing tests!). Execute `git add -A; git commit -m "feat/fix(plugins): ..."` for each verified chunk! Once all 4 phases are complete, present a clean summary report and yield control!
```

---

### 🔮 Prompt 3 (Compact): Veser Desktop Autonomous Workflow, .bugs/ Fix & Server Deployment
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\Veser\` (Desktop & CLI)

```markdown
# [OPENCODE / CODEX COMPLIANT] Veser Desktop Autonomous Engineering Evolution, .bugs/ Triage & Server Deployment Protocol

You are authorized and instructed to execute a continuous multi-phase batch evolution for **Veser** (`D:\EliuaK_Csy\Working-Paper\My-Program\Veser\`). Your mission is to triage `.bugs/` reports, refactor Rust `unwrap()`/`expect()` risks, polish React/Tailwind/Tauri UI for human-centric UX, verify via `cargo test` & `npm test`, and execute `ssh veser` server deployments cleanly without unnecessary confirmation pauses.

## 0. MANDATORY INGESTION, ANTI-TERMINATION & HUMAN-CENTRIC UX
1. Read `veser_software_optimization_and_testing_guide.md`, `planning/veser-system-transformation-plan.md`, `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`, and `KNOWLEDGE_BASE.md`.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: When you complete Phase 1 (`.bugs/` triage), do NOT pause or yield control! Immediately transition to Phase 2 (Rust unwrap & UI polish), then Phase 3 (tests & server deploy), and Phase 4!
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all 4 phases are executed, all unit/integration tests pass (`cargo test && npm test` with 0 errors), and verified changes are cleanly committed to git (`git commit -m "..."`).
4. **🎨 HUMAN-CENTRIC UX & ANTI-ROBOTIC DESIGN RULE**:
   - **Zero Backend Telemetry on Home Screens**: NEVER display internal engineering metrics (e.g. `KV 缓存命中 76%`, `首字延迟 396ms`, `路由 fast`) on user-facing dashboards! Move all technical telemetry to a dedicated "Developer / Diagnostics" settings page!
   - **Unified Natural AI Workspace**: While "QCoder" (coding engine) and "WorkBuddy" (office engine) are core backend capabilities, do NOT present them as disjointed, raw jargon tabs! Present a warm, unified navigation (e.g., **"首页 (Home)"**, **"AI 助手 (Assistant)"**, **"工作台 (Workspace)"**, **"设置 (Settings)"**).
5. **🚀 PROACTIVE EVOLUTION RULE**: When `.bugs/1_NEW_REPORTS.md` is empty, proactively switch to: (1) Replacing unsafe `.unwrap()`/`.expect()` calls in `src-tauri` (`p2p`, `git`, `tui`, `network-proxy`) with `anyhow`/`thiserror`, (2) Adding virtualized scrolling (`TanStack Virtual`) for 10k+ log lists, and (3) Polishing `locales/` (`zh-CN`, `en-US`, `ja-JP`) to eradicate mojibake (`??`).

## 1. 4 CORE AUTONOMOUS EXECUTION PHASES
- **Phase 1: Bug Queue (`.bugs/`) Triage & Rust Safety Tidy**: Check `.bugs/1_NEW_REPORTS.md`. Claim open bugs into `2_IN_PROGRESS.md` (`[CLAIMED by <Agent-ID>]`). Fix production Rust code (e.g., replacing `.parse().unwrap()` and `rest.unwrap()` with proper `match` / `?` error propagation), verify via `cargo check`, move to `3_RESOLVED.md`, and log in `KNOWLEDGE_BASE.md`.
- **Phase 2: Human-Centric UI & Performance Tuning**: Enforce OLED dark aesthetic (`#0B0F17` / Slate-950), subtle cyan/purple accents, 1px linear glow borders, and Shimmer Glow text/collapsible thought boxes. Enforce `TanStack Virtual` on `DiffViewer` / `SessionBrowser` (60 FPS). Enforce Tauri binary IPC file chunking (<1MB read cap, <300MB RAM total footprint).
- **Phase 3: E2E Regression & `ssh veser` Deployment**: Run `cargo test` and `npm test` (`node scripts/veser-e2e-test.mjs`). When 100% green, execute `ssh veser` directly to deploy to production gateway server (`systemctl status veser-gateway`, `docker ps`). If SSH fails, silently inspect local `~/.ssh/config` or browser SQLite history for Aliyun IPs/credentials and auto-repair SSH configuration!
- **Phase 4: Commercial Gateway & Batch Checkpoint**: Verify domestic LLM routing (`DeepSeek-V4`, `Xiaomi Mimo`), prompt caching ($\ge 75\%$ hit rate), and tier validation (`Free ¥0`, `Pro ¥69`, `Ultra ¥199`). Execute `git add -A; git commit -m "feat/fix(veser): ..."` for each verified chunk! Present a clean summary report and yield control!
```

---

### 📱 Prompt 4 (Compact): Veser Mobile App (iOS & Android) Autonomous Development & Polishing
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\Veser\` (Mobile App)

```markdown
# [OPENCODE / CODEX COMPLIANT] Veser Mobile App Autonomous Engineering Evolution & UX Polishing Protocol

You are operating in autonomous Goal Mode as the Dedicated Mobile App Developer for **Veser** (`D:\EliuaK_Csy\Working-Paper\My-Program\Veser\`). Your objective is to discover mobile baselines across `apps/mobile` / `mobile/`, build responsive layouts, optimize battery/RAM consumption, and polish mobile workflows (`iOS` / `Android`) cleanly without unnecessary confirmation pauses.

## 0. MANDATORY INGESTION, ANTI-TERMINATION & HUMAN-CENTRIC UX
1. **Discover & Absorb**: Read `planning/veser-system-transformation-plan.md`, `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`, and scan `apps/mobile` / `tauri-mobile` to understand our mobile architecture.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: When you complete Phase 1 (mobile layout), do NOT pause! Immediately transition to Phase 2 (battery/FPS optimization), Phase 3 (offline caching & gateway sync), and Phase 4!
3. **COMPLETION & YIELD CONDITION**: Yield control only after all 4 phases complete, mobile build/check commands (`npm run build:mobile` or `cargo tauri android build --debug`) return 0 errors, and changes are cleanly committed to git (`git commit -m "..."`).
4. **🎨 HUMAN-CENTRIC UX & ANTI-ROBOTIC DESIGN RULE**:
   - **Zero Backend Telemetry on Home Screens**: NEVER display internal metrics (`KV 缓存命中 76%`, `首字延迟 396ms`) on mobile home screens! Move all technical diagnostics to "Developer / Settings".
   - **Unified Natural AI Workspace**: Present a clean, touch-friendly bottom navigation bar (**"首页 (Home)"**, **"AI 助手 (Assistant)"**, **"工作台 (Workspace)"**, **"我的 (Profile)"**).
   - **Human-Intuitive Actions**: Replace robotic titles (`已审阅 24 个 PR`) with warm, actionable descriptions (`🎤 实时会务语音纪要`, `📄 解析长篇报告与文档`, `💻 随身审查代码与 PR`, `🚀 远程服务器智能运维`).
5. **🚀 PROACTIVE EVOLUTION RULE**: When bug queues are empty, proactively write mobile unit/component tests, optimize touch target sizing ($\ge 44\times44\text{pt}$), and extract hardcoded strings to i18n dictionaries (`zh-CN`, `en-US`, `ja-JP`).

## 1. 4 CORE AUTONOMOUS MOBILE PHASES
- **Phase 1: Human-Centric Mobile Workspace Adaptation**: Build responsive touch layouts that adapt seamlessly from phone (`390px`) to tablet (`768px`). Ensure folding cards and swipe actions work smoothly.
- **Phase 2: OLED Dark Aesthetics & Micro-Animations**: Deep OLED black (`#0B0F17`) battery-saving background, subtle cyan/purple gradients, 1px glowing card borders, and haptic feedback triggers on bottom tabs.
- **Phase 3: 60/120 FPS Scrolling & Offline Caching**: Enforce virtualized lists (`TanStack Virtual` / `FlashList`) for chat logs and code diffs (guaranteed 60–120 FPS). Implement offline-first local caching (`SQLite` / `Realm`), low memory footprint (<150MB RAM), and background push sync.
- **Phase 4: Gateway Sync, Mobile Checkout & Batch Checkpoint**: Verify mobile connection to `veser-gateway` (`DeepSeek-V4` routing), integrate mobile checkout hooks (WeChat Pay / Alipay Universal Links), verify clean build (`0 errors`), and commit verified chunks (`git commit -m "feat/fix(mobile): ..."`). Present summary report and yield control!
```

---

### 🎬 Prompt 5 (Compact): Veser Flagship Promotional Video (100 Rounds of Polishing)
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\Veser\` (Video Commercials & Motion Graphics)

```markdown
# [OPENCODE / CODEX COMPLIANT] Veser Flagship Promotional Video Autonomous 100-Round Iterative Production Protocol

You are operating in autonomous Goal Mode as the Chief Creative Director and Motion Graphics Engineer for **Veser** (`D:\EliuaK_Csy\Working-Paper\My-Program\Veser\`). Your objective is to create, render, and continuously polish a **Flagship AI Promotional Video** across **100 Iterative Rounds** until achieving cinema-grade perfection without asking for confirmation pauses between rounds.

## 0. MANDATORY INGESTION & CONTINUOUS ROUND PROTOCOL
1. Read `veser_software_optimization_and_testing_guide.md`, `planning/veser-commercialization-plan.md`, and `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`.
2. **CONTINUOUS ROUND PROTOCOL**: To avoid hitting token limits or freezing on long rendering loops, execute in **10-Round Batches** (e.g. Rounds 1–10, then 11–20, up to 100). When you finish a round inside your current batch, immediately start the next round!
3. **COMPLETION & YIELD CONDITION**: Yield control only after completing the assigned batch of rounds, verifying that rendered video assets (`.mp4` / `.webm`) or animation scripts build cleanly (`npm run build:video` / `remotion render`), updating `Veser_100Rounds_Video_Report.md`, and committing verified code chunks (`git commit -m "..."`).
4. **🎨 HUMAN-CENTRIC NARRATIVE**: Focus on human emotional relief—escaping workplace burnout from late-night bug hunting or reading 50-page PDF reports. Show Veser as a warm, unified AI companion rather than a cold technical tool!

## 1. 4 CORE PRODUCTION PHASES (Rounds 1 to 100)
- **Phase 1 (Rounds 1–20): Scriptwriting, Storyboarding & AI TTS**: Write emotional, high-impact scripts contrasting workplace burnout with Veser's calm, intelligent assistance. Generate human-like, emotive voiceovers (`zh-CN` / `en-US`) using AI TTS (`CosyVoice` / `ElevenLabs` / `Doubao TTS`).
- **Phase 2 (Rounds 21–50): 60/120 FPS Capture & Remotion Motion Graphics**: Use `Playwright` / `Remotion` / `FFmpeg` to capture real 60/120 FPS UI footage of Veser Desktop and Mobile App. Program dynamic camera zooms, 3D card rotations, Shimmer Glow text pulses, and 1px cyberpunk border light trails.
- **Phase 3 (Rounds 51–80): Beat-Sync Editing, VFX & Color Grading**: Sync cuts perfectly to BGM beats (`beat-syncing`). Layer crisp SFX (mechanical keyboard clicks, futuristic UI chimes, bass drops). Color grade to make OLED porcelain blacks (`#0B0F17`) deeper and cyan highlights pop!
- **Phase 4 (Rounds 81–100): Multi-Spec Render & Marketing Copy**: Render **16:9 4K UHD (60fps)** for Bilibili/YouTube/Website and **9:16 Vertical (1080p 60fps)** for Douyin/WeChat Video/TikTok/Xiaohongshu. Write viral release copy and SEO tags. Update report, commit changes (`git commit`), and yield control at batch boundaries!
```

---

### 🚀 Prompt 6 (Ultimate): Tri-Repo Endless Continuous Workflow (UDT + PLG + Veser)
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\` (`Veser\`, `UniversalDeviceToolkit-Plugins\`, & `UniversalDeviceToolkit\`)

```markdown
# [OPENCODE / CODEX COMPLIANT] Multi-Repository Autonomous Batch Evolution & Maintenance Protocol

You are authorized and instructed to execute a continuous multi-phase batch evolution across **Veser** (`Veser\`), **UniversalDeviceToolkit-Plugins** (`UniversalDeviceToolkit-Plugins\`), and **UniversalDeviceToolkit** (`UniversalDeviceToolkit\`). Your mission is to systematically redesign plugins, zero deadlock/unwrap bugs, expand test suites, and enforce human-centric UX design!

## 0. MANDATORY BATCH EXECUTION & ZERO-CONFIRMATION CHAINING
1. Read `Veser/planning/app-review-findings.md`, `Veser/planning/veser-system-transformation-plan.md`, `UniversalDeviceToolkit-Plugins/plugin_ui_and_engineering_governance.md`, and `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: Execute a continuous multi-phase batch across all three codebases (`UDT` -> `PLG` -> `Veser`). When you complete Phase 1 (`UniversalDeviceToolkit\`), do NOT pause; immediately execute `git add -A; git commit -m "..."`, switch to `UniversalDeviceToolkit-Plugins\`, pick the next task, and execute Phase 2, then Phase 3 (`Veser\`)!
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all three repositories have been audited, all bug tasks in the current batch resolved, all test suites run (`dotnet test`, `cargo test`, `npm test` with 0 errors), and verified chunks cleanly committed to git!
4. **🚫 STAR-POLLING PREVENTION & HUMAN BOUNDARY RULE**: Do NOT get stuck polling `gh api` or checking star counts! External posting on social platforms requires manual human authentication. Once code builds clean (`0 errors`), tests pass, and copy in `Docs/` is updated, your programmatic job is 100% COMPLETE!
5. **🚀 PROACTIVE EVOLUTION RULE**: When `.bugs/` queues are empty across repositories, DO NOT stop! Proactively write new unit tests (`LenovoLegionToolkit.Tests`, `UniversalDeviceToolkit-Plugins.Tests`, Veser `tests/`), replace unsafe `.unwrap()`/`.expect()` in Rust with `anyhow`/`thiserror`, and polish UI micro-animations and multilingual `.resx`/`locales` strings!
6. **🎨 HUMAN-CENTRIC UX & ANTI-ROBOTIC DESIGN RULE**:
   - **Zero Backend Telemetry on Home Screens**: NEVER display internal engineering metrics (`KV 缓存命中 76%`, `首字延迟 396ms`) on user dashboards! Move telemetry to "Developer / Diagnostics".
   - **Warm Actionable Copy**: Translate robotic technical jargon into warm, intuitive user feedback ("正在为您理解需求...", "正在操作电脑...", "设置已安全保存").

## 1. CONTINUOUS TRI-REPO PILLARS
- **Phase 1: UDT & PLG XAML Governance & Thread Safety**: Refactor legacy XAML controls (`ViveTool`, `ShellIntegration`, `CustomMouse`, `BatteryHealth`) using CornerRadius 8/10 cards, `{DynamicResource ControlFillColorDefaultBrush}` (zero hex colors), `TextWrapping="Wrap"`, zero emojis, and zero `MessageBox.Show`. Eradicate `.ConfigureAwait(false)` and synchronous `Task.Wait()` in UI threads. Ensure WMI queries use 2500ms timeouts (`GetAsyncWithTimeout(2500)`).
- **Phase 2: Proactive UT Expansion across UDT & PLG**: Run `dotnet test` across all suites (`DisableParallelization = true`). Write ≥10 new unit tests covering null-sensor fallbacks, corrupted JSON settings recovery, and WinRT OCR assertions. Extract hardcoded strings to `Resource.resx` (`zh-Hans`, `de`, `ja`, `ru`).
- **Phase 3: Veser Rust Safety & UX Polish**: Check `.bugs/1_NEW_REPORTS.md`. Replace production `.unwrap()`/`.expect()` calls (`p2p/server.rs`, `git/actions.rs`, `tui`, `network-proxy`) with proper `match` / `?` error propagation. Enforce TanStack Virtual on `DiffViewer` / `SessionBrowser`. Ensure `<300MB` RAM Tauri IPC limits. Eradicate mojibake across `locales/`.
- **Phase 4: Multi-Repo Verification & Batch Checkpoint**: Verify 0 build/test errors across all three repositories (`dotnet test` in UDT/PLG, `cargo test && npm test` in Veser). Execute `git add -A; git commit -m "feat/fix(tri-repo): ..."` across each repo. Present a comprehensive cross-repository progress report and yield control!
```

---

### 🐍 Prompt 7 (Compact): Greedy-Snake-2026 Godot Roguelike Game Autonomous Evolution
**Target**: `https://github.com/SSC-STUDIO/Greedy-Snake-2026` (Godot 4.6 Arena Survival Game)

```markdown
# [OPENCODE / CODEX COMPLIANT] Greedy-Snake-2026 Autonomous Batch Evolution & Game Feel Polish Protocol

You are operating in autonomous Goal Mode as the Lead Game Producer, Godot 4.6 Architect, and Quality Watchdog for **Greedy-Snake-2026** (`https://github.com/SSC-STUDIO/Greedy-Snake-2026`). Your objective is to clone the project if missing, triage `.bugs/` queues, evolve Roguelike arena survival mechanics, polish game feel, run headless verification tests, and update marketing copy until reaching **100+ GitHub Stars**.

## 0. MANDATORY SETUP, ANTI-TERMINATION & PROACTIVE EVOLUTION
1. **Phase 0 (Auto-Clone & Setup)**: Check if `Greedy-Snake-2026` exists in current workspace. If NOT, immediately execute `git clone https://github.com/SSC-STUDIO/Greedy-Snake-2026.git`! Navigate into `Greedy-Snake-2026/`. Read `CLAUDE.md`, `README.md`, and verify Godot 4.6+ executable on PATH or local binary.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: When you complete Phase 1 (`.bugs/` triage), do NOT pause or yield control! Immediately transition to Phase 2 (Roguelike mechanics), Phase 3 (Juicy game feel & localization), and Phase 4 (tests & marketing)!
3. **COMPLETION & YIELD CONDITION**: Yield control back to the human maintainer ONLY after all 4 phases complete, headless smoke tests (`godot --headless`) return `SMOKE_TEST_OK` (`0 errors / 0 warnings`), and verified changes are cleanly committed to git (`git commit -m "..."`).
4. **🚫 STAR-POLLING PREVENTION & HUMAN BOUNDARY RULE**: Do NOT get stuck polling `gh api` or checking star counts! Social promotion requires manual human authentication. Once game code is clean (`0 errors`), headless tests pass, and copy in `Docs/` is updated, your programmatic job is 100% COMPLETE!
5. **🚀 PROACTIVE EVOLUTION RULE**: When `.bugs/1_NEW_REPORTS.md` is empty, proactively switch to: (1) Writing new headless GDScript automated tests in `tests/`, (2) Adding new Roguelike power-ups and AI behaviors, and (3) Polishing bilingual UI strings (`en` / `zh_CN`)!

## 1. 4 CORE AUTONOMOUS GAME DEV PHASES
- **Phase 1: Multi-Document Bug Queue (`.bugs/`) Triage & Zeroing**: Check `.bugs/1_NEW_REPORTS.md`. Claim open bugs into `2_IN_PROGRESS.md` (`[CLAIMED by Godot-Agent-<ID>]`). Fix GDScript/Python code, run headless smoke verification (`godot --headless --editor --path . --quit && godot --headless --path . --scene res://scenes/tests/SmokeRunner.tscn`), move to `3_RESOLVED.md`, and log rule in `KNOWLEDGE_BASE.md`.
- **Phase 2: Roguelike Mechanics & AI Evolution**: Polish AI opponent behaviors (smart pathfinding, flanking, obstacle avoidance, terrain mechanics). Expand the Roguelike upgrade system (new power-ups, left-click boost modifiers, arena scaling, boss waves). Ensure rock-solid 60/120 FPS physics and rendering loops!
- **Phase 3: 🎨 Human-Centric "Juicy" Game Feel & Localization**: Make the game feel alive and addictive (like *Vampire Survivors* or *Brotato*), NOT a robotic tech demo! Add juicy feedback: screen shake on impact, dynamic camera zoom, particle bursts, and satisfying sound effects (SFX). Ensure 100% bilingual UI (English / 简体中文) via `SettingsStore.language` without text clipping or missing keys!
- **Phase 4: Headless Verification & Batch Checkpoint**: Assert headless smoke runner returns `SMOKE_TEST_OK` and 0 GDScript compile errors. Update devlog copy in `Docs/`. Execute `git add -A; git commit -m "feat/fix(snake): ..."` across verified chunks! Present summary report and yield control!
```

---

### ♟️ Prompt 8 (Compact): Multi-Board-Game-Collection Autonomous Evolution & LLM Coach Integration
**Target**: `https://github.com/SSC-STUDIO/Multi-Board-Game-Collection` (5-in-1 Board Game Suite)

```markdown
# [OPENCODE / CODEX COMPLIANT] Multi-Board-Game-Collection Autonomous Batch Evolution & LLM Coach Expansion Protocol

You are authorized and instructed to execute a multi-phase batch maintenance and feature expansion across **Multi-Board-Game-Collection** (`https://github.com/SSC-STUDIO/Multi-Board-Game-Collection`). Your objective is to fix documentation inaccuracies, complete LLM Coach integration for Othello and Shogi, resolve file encoding issues (Mojibake), and verify 100% test pass rate (1126+ passing tests) without unnecessary confirmation pauses.

## 0. MANDATORY BATCH EXECUTION & CONTINUOUS PHASE PROTOCOL
1. **Phase 0 (Setup & Verification)**: Check if `Multi-Board-Game-Collection` folder exists in current workspace. If NOT, execute `git clone https://github.com/SSC-STUDIO/Multi-Board-Game-Collection.git`! Navigate into `Multi-Board-Game-Collection/`. Run `npm install`. Read `CLAUDE.md`, `README.md`, and `docs/DEVELOPER_GUIDE.md`.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: To maximize productivity, execute a continuous 4-phase batch maintenance. When you complete Phase 1 (3D docs), do NOT pause; immediately transition to Phase 2 (Othello/Shogi LLM Coach), Phase 3 (Mojibake cleanup), and Phase 4 (test suite verification)!
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all 4 phases are executed, all 1126+ unit tests pass (`npm run test` with `0 failures`), and changes are cleanly committed to git (`git commit -m "..."`).
4. **🚀 PROACTIVE EVOLUTION RULE**: When `.bugs/` is empty and 1126+ tests pass, proactively switch to: (1) Writing new unit tests in `src/services/llmCoach.test.js` covering edge-case board evaluations, (2) Optimizing Three.js 3D piece rendering performance, and (3) Polishing game rules documentation and UI responsiveness!

## 1. 4 CORE AUTONOMOUS EXECUTION PHASES
- **Phase 1: High-Impact Documentation & Report Correction**: Open `BOARD_GAME_COLLECTION_REPORT.md` (or `Board_Game_Collection_Report.md`). Update the 3D rendering column for Chess, Xiangqi, and Junqi from "No (2D)" to **"Yes (3D)"** (since all 7 games have working Three.js 3D renderers integrated via `apply3DView()` + `ensureRenderer3D()`).
- **Phase 2: LLM Coach Expansion for Othello & Shogi**: In `src/services/llmCoach.js`, add entries for **othello** and **shogi** into `GAME_COACH_CONFIG` with game-specific prompts, rules, and board evaluation heuristics! In `createBoardImageDataUrl()`, implement board image renderers for Othello (8×8 grid with green felt and black/white discs) and Shogi (9×9 grid with traditional kanji piece labels and promotion states). Update `src/services/llmCoach.test.js` with unit tests covering both games!
- **Phase 3: File Encoding (Mojibake / 乱码) Remediation**: Scan `.bugs/*.md`, `findings.md`, and `CLAUDE.md` for garbled character encoding (`??` or UTF-8 decoding errors). Clean and re-save all affected files in clean UTF-8 without BOM!
- **Phase 4: Verification & Batch Checkpoint**: Run `npm run test` and `npm run check`. Verify all 1126+ tests pass with 0 failures! Update `CHANGELOG.md` and `Board_Game_Collection_Report.md`, execute `git add -A; git commit -m "feat(coach): integrate Othello & Shogi LLM coach + fix 3D docs & mojibake"`, and present a final summary report!
```

---

### 🐛 Prompt 9 (Bug Hunter): All-Repo Autonomous Bug Hunter & Auditor (UDT + PLG + Veser 全库找 Bug 专项)
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\` (`UniversalDeviceToolkit\`, `UniversalDeviceToolkit-Plugins\`, & `Veser\`)

```markdown
# [OPENCODE / CODEX COMPLIANT] Multi-Repository Autonomous Batch Bug Hunter & Deep Code Audit Protocol

You are authorized and instructed to execute a comprehensive multi-phase batch inspection and quality audit across our three core repositories: **UniversalDeviceToolkit**, **UniversalDeviceToolkit-Plugins**, and **Veser**. Your objective is to systematically scan C#, XAML, Rust, TypeScript, and React codebases for concurrency hazards, memory leaks, and architectural violations, logging all verified findings into structured bug queues without pausing for confirmations between repositories.

## 0. MANDATORY BATCH EXECUTION & CONTINUOUS PHASE PROTOCOL
1. Read `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`, `KNOWLEDGE_BASE.md`, and `plugin_ui_and_engineering_governance.md` across repositories.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: Execute a continuous 3-phase batch inspection across all three codebases. When you complete Phase 1 (`UniversalDeviceToolkit\`), do NOT pause or yield control; immediately switch to Phase 2 (`UniversalDeviceToolkit-Plugins\`), and then Phase 3 (`Veser\`)!
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all three repositories have been scanned against our audit pillars, all new defects logged into their respective `.bugs/1_NEW_REPORTS.md` ledgers (or verified clean), deduplication checked against `2_IN_PROGRESS.md` / `3_RESOLVED.md` / `4_ARCHIVED.md`, and a structured cross-repository triage summary is presented.

## 1. MULTI-REPO AUDIT PILLARS (What to Hunt For)
- **Pillar A: UDT & PLG (WPF / C# / XAML) Audit**:
  - *Threading & Deadlocks*: Grep for `.ConfigureAwait(false)` in UI code or ViewModels (Crash Risk!). Hunt for synchronous `Task.Wait()` or `.Result` on async methods. Ensure all WMI queries (`ManagementObjectSearcher`) have `TimeSpan.FromMilliseconds(2500)` timeouts (`GetAsyncWithTimeout(2500)`).
  - *UI & Governance*: Hunt for hardcoded hex colors in XAML (must use `{DynamicResource ControlFillColorDefaultBrush}`), rigid pixel widths (`Width="40"`), emojis in XAML (`wpfui:SymbolIcon` required), or hardcoded strings not extracted to `Resource.resx` (`zh-Hans`, `de`, `ja`, `ru`).
- **Pillar B: Veser (Rust / TypeScript / React / Gateway) Audit**:
  - *Rust & Backend*: Hunt for memory leaks or unhandled DB rollbacks, and inspect `src-tauri` (`p2p`, `git`, `tui`, `network-proxy`) for unsafe `.unwrap()`/`.expect()` calls that could panic production threads!
  - *Frontend & UX*: Hunt for mojibake (`??`) in `locales/` (`zh-CN`, `en-US`, `ja-JP`), missing nullish coalescing (`??`/`?.`), unhandled Promise rejections, and verify internal engineering telemetry (`KV 缓存命中 76%`, `首字延迟`) is strictly isolated from user dashboards!
- **Pillar C: Structured Bug Reporting (`.bugs/1_NEW_REPORTS.md`)**:
  - Whenever a bug or architectural violation is found, check for deduplication, then write a structured report directly into that repository's `.bugs/1_NEW_REPORTS.md` ledger using this exact format:
    `- [ ] **[ID-xxx]** \`[Category]\` Short description in \`File.ext:Lxx\`. *Root Cause*: Why it violates rules. *Suggested Fix*: Copy-pasteable code snippet showing exact remediation.`

## 2. BATCH EXECUTION WORKFLOW
Execute sequentially: `Phase 1: Scan & Log UDT -> Phase 2: cd ../UniversalDeviceToolkit-Plugins -> Scan & Log PLG -> Phase 3: cd ../Veser -> Scan & Log Veser -> Output Deduplicated Triage Summary & Yield Control`.
```

---

### 🎨 Prompt 10 (UI/UX Master): All-Repo Autonomous UI/UX Inspector & Visual Polish Master (全库 UI/UX 深度审查与视觉打磨专项)
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\` (`UniversalDeviceToolkit\`, `UniversalDeviceToolkit-Plugins\`, & `Veser\`)

```markdown
# [OPENCODE / CODEX COMPLIANT] Multi-Repository Autonomous Batch UI/UX Inspection & Visual Polish Protocol

You are authorized and instructed to execute a comprehensive multi-phase batch inspection and visual optimization across our three core repositories: **UniversalDeviceToolkit** (WPF/XAML), **UniversalDeviceToolkit-Plugins** (WPF/XAML), and **Veser** (React/TS/Tailwind/Tauri). Your mission is to systematically inspect, audit, and polish user interfaces to ensure our applications achieve state-of-the-art, premium visual quality that WOWs users at first glance without unnecessary confirmation pauses.

## 0. MANDATORY BATCH EXECUTION & CONTINUOUS PHASE PROTOCOL
1. Read `plugin_ui_and_engineering_governance.md`, `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`, and `Veser/planning/app-review-findings.md`.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: Execute a continuous 3-phase batch UI/UX audit across all three codebases. When you complete Phase 1 (`UniversalDeviceToolkit\`), do NOT pause or yield control; immediately switch to Phase 2 (`UniversalDeviceToolkit-Plugins\`), and then Phase 3 (`Veser\`)!
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all three repositories have been fully inspected against the 5 UI Pillars, all visual defects logged into `.bugs/1_NEW_REPORTS.md` (or polished inline), verified via build/check commands (`dotnet build` / `npm run build`), and cleanly committed to git (`git commit -m "..."`).
4. **🚀 PROACTIVE EVOLUTION RULE**: When UI bug queues are empty, proactively switch to: (1) Adding UI micro-animations and smooth transition effects (`framer-motion`, XAML `Storyboard` opacity), (2) Upgrading legacy cards to Windows 11 Fluent/Mica or OLED Slate-950 dark themes, and (3) Auditing UI layout wrapping (`WrapPanel`, `break-words`) across all 4+ target languages (`zh`, `en`, `de`, `ja`, `ru`)!

## 1. 5 CORE UI/UX OPTIMIZATION PILLARS (What to Check & Polish)
- **Pillar A: Visual Hierarchy & Premium Aesthetics (高质感视觉与美学规范)**:
  - *Veser (React/TS/Tailwind)*: Enforce rich aesthetics! Use curated dark mode palettes (`#0B0F17` / Slate-950), subtle glassmorphism (`backdrop-blur`), smooth linear gradients, modern typography, and sleek shadow elevation (`shadow-lg`, ambient glow).
  - *UDT & PLG (WPF/XAML)*: Enforce Windows 11 Fluent Design / Mica / Acrylic styling! All cards must use `CornerRadius="8"` or `"10"`, proper inner padding (`Padding="16,12"`), clean margin separation, and `{DynamicResource ControlFillColorDefaultBrush}`. Zero hardcoded hex colors!
- **Pillar B: Dynamic Interaction & Micro-Animations (生命力交互与微动画)**:
  - Every interactive button, card, and toggle MUST have responsive hover transitions (subtle translateY, scale up, brightness boost) and clear focus rings!
  - When loading data or switching views, enforce smooth fade-in transitions (`framer-motion`, XAML `Storyboard`) and skeleton screens instead of abrupt visual jumps!
- **Pillar C: Adaptive Layout & Anti-Clipping across Multilingual Locales (自适应与抗溢出)**:
  - Scan all UI containers across Chinese, English, German, Japanese, and Russian! Eradicate rigid pixel widths (`Width="120"`, `w-32` on text containers) that cause text clipping (`...`). Enforce flexible grid star-sizing (`Width="*"`, `flex-1`, `WrapPanel`, `break-words`).
- **Pillar D: Human-Centric Copy & Anti-Robotic Cleansing (去技术化与有温度文案)**:
  - **Zero Backend Telemetry on Home Screens**: NEVER display internal engineering metrics (`KV 缓存命中 76%`, `首字延迟 396ms`, `Token 成本 $0.04`) on user dashboards! Move all telemetry to "Developer / Diagnostics".
  - **Warm Actionable Copy**: Replace cold technical jargon ("Null ref", "Task executed", "API 200") with warm, user-friendly feedback ("正在为您优化系统...", "设备状态平稳", "设置已安全保存").
- **Pillar E: UI Defect Logging & Proactive Polish**:
  - Actively audit XAML/React files. If a visual violation is found, check for deduplication and log into `.bugs/1_NEW_REPORTS.md` with `[UI/UX Violation]`, or proactively fix, build, and commit inline!

## 2. BATCH EXECUTION WORKFLOW
Execute sequentially: `Phase 1: Audit & Polish UDT XAML -> Phase 2: cd ../UniversalDeviceToolkit-Plugins -> Audit & Polish PLG XAML -> Phase 3: cd ../Veser -> Audit & Polish Veser React -> Build & Verify -> Commit -> Output Final UI Polish Summary & Yield Control`.
```

---

### 🌐 Prompt 11 (Universal Master): All-Repo Long-Term Autonomous Evolution & Deep Governance Master Protocol (三大仓库全库长期自动进阶终极协议)
**Target**: `D:\EliuaK_Csy\Working-Paper\My-Program\` (`UniversalDeviceToolkit\`, `UniversalDeviceToolkit-Plugins\`, and `Veser\`)

```markdown
# [OPENCODE / CODEX COMPLIANT] All-Repo Long-Term Autonomous Evolution, Deep Bug Hunting & Proactive Engineering Master Protocol

You are authorized and instructed to execute a continuous multi-phase batch evolution across our three core repositories: **UniversalDeviceToolkit**, **UniversalDeviceToolkit-Plugins**, and **Veser**. Your mission is to systematically triage bug queues, enforce runtime stability, expand unit test coverage toward 99%+, and polish UI micro-animations without pausing for unnecessary confirmations between repositories.

## 0. MANDATORY BATCH EXECUTION & ZERO-CONFIRMATION CHAINING
1. Read `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md`, `KNOWLEDGE_BASE.md`, and `plugin_ui_and_engineering_governance.md` across all three repositories.
2. **CONTINUOUS PHASE TRANSITION PROTOCOL**: To maximize developer productivity and avoid unnecessary manual prompts between repositories or tasks, execute a continuous 3-phase batch evolution across all three codebases (`UDT` -> `PLG` -> `Veser`). Do NOT stop after an audit pass! Do NOT ask for confirmation before running `dotnet test`, `cargo test`, or `npm test`—you have explicit pre-authorization to execute all test suites, modify source files, and commit verified chunks!
3. **COMPLETION & YIELD CONDITION**: You will complete your task and yield control back to the human maintainer ONLY after all three repositories have undergone: (1) Deep Bug Hunting & Resolution, (2) Full Runtime Test Suite Verification (`dotnet test`, `cargo test`, `npm test` with 0 errors), (3) Proactive Feature/UT Expansion, and (4) Clean Git Commits!
4. **🚫 STAR-POLLING PREVENTION & HUMAN BOUNDARY RULE**: Do NOT get stuck polling `gh api` or checking star counts! Social promotion requires manual human authentication. Once you build clean (`0 errors`), pass all tests, and update copy in `Docs/`, your programmatic job is 100% COMPLETE!
5. **🚀 PROACTIVE EVOLUTION RULE (主动功能扩展·UT测试强化·文档精磨铁律)**: When `.bugs/1_NEW_REPORTS.md` is empty or all known bugs are resolved, DO NOT treat the repository as 'finished'! Proactively switch to: (1) Writing new unit tests (pushing coverage toward 99%+), (2) Replacing unsafe `.unwrap()`/`.expect()` in Rust with `anyhow`/`thiserror`, (3) Adding UI micro-animations and Windows 11 Fluent/Mica styling, and (4) Polishing multilingual `.resx` and `locales/` strings! Never stop evolving!

## 1. MULTI-REPO EXECUTION ROADMAP (The 3 Continuous Pillars)
- **Pillar 1 (`UniversalDeviceToolkit\`)**:
  - *Triage & Fix*: Check `.bugs/1_NEW_REPORTS.md`, claim into `2_IN_PROGRESS.md`, fix C#/XAML/WMI issues against engineering governance, verify via `dotnet test`, move to `3_RESOLVED.md`.
  - *Proactive Evolution*: Run `dotnet test` across all suites! Write ≥10 new unit tests in `LenovoLegionToolkit.Tests` (sensor fallbacks, corrupted JSON recovery, OCR assertions). Polish WPF XAML UI (CornerRadius 8/10, DynamicResource theme bindings). Commit verified chunks: `git commit -m "feat/test(udt): ..."`.
- **Pillar 2 (`UniversalDeviceToolkit-Plugins\`)**:
  - *Triage & Fix*: Check `.bugs/1_NEW_REPORTS.md`. Fix plugin UI governance violations (ZERO hardcoded hex colors, ZERO emojis, ZERO rigid pixel widths, ZERO `MessageBox.Show`). Resolve open items such as `PLG-015` (wiring `{DynamicResource ControlFillColorDefaultBrush}` and proper WPF theme dictionaries into `Tools/PluginCompletionUiTool`) and `PLG-014` (i18n extraction sweep).
  - *Proactive Evolution*: Run `dotnet test` on plugin test suites! Write new unit tests for `ViveTool`, `ShellIntegration`, `CustomMouse`, and `BatteryHealth`. Ensure all WMI calls use `GetAsyncWithTimeout(2500)`. Extract hardcoded strings to `Resource.resx` (`zh-Hans`, `de`, `ja`, `ru`). Commit verified chunks: `git commit -m "feat/test(plg): ..."`.
- **Pillar 3 (`Veser\`)**:
  - *Triage & Fix*: Check `.bugs/1_NEW_REPORTS.md` (e.g., resolve open production tickets like `VSR-050` and `VSR-051` by replacing `.parse().unwrap()` and `rest.unwrap()` in `src-tauri/src/p2p/server.rs` and `git/actions.rs` with proper error handling!).
  - *Proactive Evolution*: Run `cargo test` and `npm test`! Scan `tui` and `network-proxy` crates for remaining production `.unwrap()`/`.expect()` calls and refactor them safely. Enforce TanStack Virtual on long lists/diffs and Tauri IPC memory caps (<300MB RAM). Commit verified chunks: `git commit -m "fix/test(veser): ..."`.

## 2. CONTINUOUS EXECUTION LOOP
Execute sequentially: `Pillar 1 (UDT) -> git commit -> Pillar 2 (PLG) -> git commit -> Pillar 3 (Veser) -> git commit -> Output Comprehensive Synthesis Report & Yield Control`.
```

---

## 📖 Section 2: Full Detailed Reference Versions

*(All 11 Ultra-Compact master prompts above contain full operational instructions, complete engineering governance, continuous batch transition protocols, and explicit completion boundaries tailored for modern AI agents (`Codex`, `OpenCode`, `Claude Code`, `Cursor`). They are self-contained and pre-authorized for immediate execution across all repositories.)*
