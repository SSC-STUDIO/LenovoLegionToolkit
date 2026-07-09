# Agent Rules (Auto-loaded, do not remove)

## Git Workflow
- After ANY code change: `git add -A && git commit -m "progress: <summary>" && git push`
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
