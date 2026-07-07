# Codex + OpenCode Bug Reporter Prompt (High-Speed Search & Tri-Repo Queue)

This document provides a specialized, production-ready prompt designed for **Codex** (or OpenAI Codex / Codex Gateway) acting as the orchestrator and invoking **OpenCode** (or OpenCode CLI/tools) for ultra-fast AST and codebase searching across our **Tri-Repo Ecosystem**:
1. `UniversalDeviceToolkit` (Main Repo)
2. `UniversalDeviceToolkit-Plugins` (Plugin Repo)
3. `Veser` (Dual-Engine AI Productivity Software Repo)

To support multiple AI agents running simultaneously across these three repositories, this reporter writes to a **Multi-Document Bug Tracking Queue (`.bugs/` directory)** rather than a single monolithic file, completely eliminating write collisions and duplicate bug handling.

---

## ⚡ Section 1: Ultra-Compact Prompt (精简高效版 - 专为 Codex + OpenCode 极速检索与多文档队列设计)

*(Copy and paste this prompt directly into Codex / Codex Agent CLI)*

```markdown
# [CODEX + OPENCODE] High-Speed Bug Reporter & Tri-Repo Queue Synthesizer

You are operating in Codex as a long-term Bug Reporter monitoring THREE repositories:
1. Main Repo: `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit\`
2. Plugin Repo: `D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins\`
3. Veser Repo: `D:\EliuaK_Csy\Working-Paper\My-Program\Veser\`

## 1. MANDATORY GOVERNANCE & QUEUE INGESTION
Before searching, read `AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md` and `KNOWLEDGE_BASE.md` in each repo root.
To prevent race conditions with concurrent maintenance agents, inspect the **Multi-Document Bug Queue (`.bugs/`)** in each repo root:
- `.bugs/1_NEW_REPORTS.md`: New unassigned defects (your write target).
- `.bugs/2_IN_PROGRESS.md`: Defects currently locked/claimed by Claude Code / maintenance agents.
- `.bugs/3_RESOLVED.md`: Fixed defects pending final verification.
- `.bugs/4_ARCHIVED.md`: Historically solved defects.

## 2. HIGH-SPEED OPENCODE SEARCH EXECUTION
Do not load every file into memory! Invoke **OpenCode** tools (`opencode search`, `opencode grep`, symbol indexing, AST queries) to perform sub-second searches across all three codebases for:
- **UniversalDeviceToolkit / Plugins**: `.ConfigureAwait(false)` in UI/ViewModels, synchronous WMI (`ManagementObjectSearcher`) without 2500ms timeouts, hardcoded color hex strings (must bind to `ControlFillColorDefaultBrush`), rigid pixel widths (`Width="40"`), or unlocalized strings not in `Resource.resx`.
- **Veser**: Rust `.unwrap()` violations in production code (must use `thiserror` + `anyhow`), frontend (`apps/desktop`) calling OS commands directly (must use Rust CLI JSON-RPC), Tauri IPC binary streams over 300MB RAM, or missing TanStack Virtual on long logs/diffs.
- **Runtime Stability**: Unhandled exceptions, missing `#nullable enable`, or failing tests in `dotnet test` / `cargo test` / `npm test`.

## 3. MULTI-DOCUMENT QUEUE SYNTHESIS RULES
1. **Deduplication Check**: Before reporting a defect, check `.bugs/2_IN_PROGRESS.md`, `3_RESOLVED.md`, and `4_ARCHIVED.md`. NEVER report a bug that is currently claimed or resolved!
2. **Atomic Append**: Append confirmed new defects directly to `.bugs/1_NEW_REPORTS.md` using this strict format:
```markdown
- [ ] **[ID-001]** `[Category]` Short description in `File.cs:Lxx` (or `.rs`/`.ts`). *Root Cause*: Why it violates rules. *Suggested Fix*: Exact remediation code snippet.
```

## 4. LONG-TERM EXECUTION LOOP
Loop continuously: `Invoke OpenCode High-Speed Search -> Filter against In-Progress/Resolved Queues -> Append to .bugs/1_NEW_REPORTS.md -> Sleep/Wait -> Repeat`. Act as an ultra-fast, concurrency-safe quality watchdog!
```

---

## 📖 Section 2: Full Detailed Architecture & Concurrency Protocol

*(See previous sections for expanded diagnostic context and complete rules)*
