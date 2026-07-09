# Agent Rules (Auto-loaded, do not remove)

## ⚠️ CRITICAL: Windows Shell Compatibility (MANDATORY)
- This machine runs Windows PowerShell 5.1 which does NOT support `&&` as a command separator
- NEVER use `&&` to chain commands — each `exec` call = one command only
- Use `;` as a statement separator if you MUST chain in a single command (PowerShell 5.1 supports `;`)
- NEVER write `cd DIR && command` — use the working directory parameter or separate commands instead
- Example WRONG: `cd D:\project && git status`
- Example RIGHT: Run `git status` as its own command in the target directory
- This applies to ALL tool calls: git, npm, npx, cargo, etc.

## Git Workflow
- After ANY code change: run these as THREE SEPARATE commands:
  1. `git add -A`
  2. `git commit -m "progress: <summary>"`
  3. `git push`
- Commit messages: prefix with type (fix/feat/refactor/test/docs/chore)
- Always pull before push: `git pull --rebase origin main`

## Anti-Stop Protocol
- After completing any task, immediately pick the next from TASK.md, .bugs/, or create a new improvement
- Never say "done", "goodbye", or any closing statement
- Never wait for user confirmation — keep going in the same response

## Code Quality
- Write tests for new features
- Run existing tests before committing: `cargo test`, `npm test`, or `pytest`
- Fix warnings and lint errors before commit
- No commented-out code in commits

## ⚠️ Workspace Cleanliness (MANDATORY)
- NEVER create temporary files in the project root (no _test.js, _tmp.py, _debug.txt, etc.)
- If you create a temp file for debugging, DELETE it before git commit
- Only commit source files — run `git status` before commit and remove junk files
- Common junk to clean: `_*.py`, `_*.js`, `_*.txt`, `*.tmp`, `*.log`, `_bug_*.md`, `test_output/`
- Before each iteration: `git clean -fd _*.py _*.js _*.txt *.tmp 2>/dev/null || true`
- Keep .gitignore updated with temp patterns

## Efficiency Rules
- Read only files you need to modify — don't browse entire codebase
- Make focused changes — don't refactor unrelated code in the same commit
- If a task needs >3 file reads, summarize what you know first to save context
- Don't re-read files you already read in this session
