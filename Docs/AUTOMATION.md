# Star-Growth Automation

Two recurring jobs keep the 1,000-star campaign on schedule so nobody has to remember by hand. Both are read-only (queries and comments) and can be paused at any time.

## 1. In-thread weekly monitor (Codex heartbeat)

- Runs every Sunday, Asia/Shanghai local, in the Codex goal thread.
- Reports a one-line digest: current stars, delta vs the last logged row in `PILLAR_D_PROMOTION_PLAN.md`, percent to 1,000, a small progress bar, the current phase, and the single most important not-yet-done outreach action.
- Read-only: it does not edit files. It re-reads the PILLAR weekly table for the previous count.
- Pause or edit: use Codex automation management (search "automation"). Name: `UDT star-growth weekly monitor`.

## 2. Public weekly digest issue (GitHub Actions)

- File: `.github/workflows/star-growth.yml`
- Runs every Monday 06:00 UTC, and on demand via Actions tab -> "Run workflow".
- Finds or creates one open issue titled "Star Growth Tracker" and posts a weekly comment: stars (with delta since the previous comment), forks/watchers/latest release, percent to 1,000, a milestone checklist, the current phase, and the next priority action.
- Permissions: `issues: write` only. It never commits to the repo.
- Disable: Actions tab -> "Star Growth Tracker" -> "..." menu -> "Disable workflow".

## How they complement

The heartbeat nudges whoever is driving the campaign; the public issue is the visible-on-repo momentum signal that visitors and awesome-list reviewers can see. They run on different days (Sun and Mon) and touch different artifacts, so they never conflict.

## Alignment with the plan

`PILLAR_D_PROMOTION_PLAN.md` says "every Sunday, log a row". The Sunday heartbeat surfaces that row in-thread; committing it to the PILLAR weekly table (or asking Codex to) closes the loop. Both jobs point at the same phase ladder and next-action list, so the output stays consistent.
