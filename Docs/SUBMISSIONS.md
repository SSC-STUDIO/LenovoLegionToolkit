# UDT Directory and Awesome-List Submissions

Track where UDT is submitted for discovery beyond social posts. A listing on a
curated list converts well because the visitor already wants a tool like this.

## Status tracker

| Venue | Type | Status | Date | Notes |
|-------|------|--------|------|-------|
| awesome-dotnet | awesome-list PR | Closed, unmerged | 2026-08-30 | PR #1466 https://github.com/quozd/awesome-dotnet/pull/1466 ; closed by the SSC-STUDIO account itself, no maintainer review/comments recorded. Not currently listed. Re-evaluate before resubmitting (avoid duplicate PRs to the same list) |
| awesome-windows | awesome-list PR | Skipped | 2026-07-07 | canonical repo 0PandaDEV/awesome-windows; maintainer hostile to AI PRs (hidden anti-AI README comment + visible CAUTION rejecting vibecoded slop). Revisit if stance softens |
| electron/apps | Electron app directory | Prepared, blocked | 2026-08-31 | Entry ready on fork branch https://github.com/SSC-STUDIO/apps/tree/add-universal-device-toolkit (`apps/universal-device-toolkit/`, category Utilities, 512x512 icon). PR creation blocked by the repo's own anti-spam restriction -- `gh pr create` and the REST API both reject with a permissions/404 error matching GitHub's "limit who can open pull requests to collaborators" setting (likely enabled against AI-generated submission floods). No PR opened. Revisit later or ask a maintainer for contributor access |
| AlternativeTo | database entry | Not started | - | suggest as alt to "Lenovo Vantage"; non-GitHub site, out of scope for this GitHub-only round |
| Slant | list entry | Not started | - | add to a "Lenovo Vantage alternatives" question; non-GitHub site, out of scope for this GitHub-only round |
| HelloGitHub | CN feature | Submitted | 2026-07-05 | see .github/outreach/hellogithub-issue.md |
| winstall.app | winget mirror | Listed | - | winstall.app/apps/SSC-STUDIO.UniversalDeviceToolkit |
| winget-pkgs | package manager | Not published | 2026-07-07 | id reserved (`SSC-STUDIO.UniversalDeviceToolkit`); `manifests/s/SSC-STUDIO/SSC-STUDIO.UniversalDeviceToolkit` returns 404 as of 2026-07-07 -- use the Releases installer until a clean PR lands (the Scoop bucket does not exist yet either; see the Scoop bucket row below). Packaging task, not an awesome-list submission; out of scope for this round |
| Microsoft Store | store listing | Not planned | - | optional; GPL-3.0, Electron + .NET Host, big lift |
| Scoop bucket | package manager | **Missing (404) -- public references removed** | 2026-08-31 | Verified: `https://github.com/SSC-STUDIO/scoop-bucket` does not exist (confirmed via `gh repo list` and a direct fetch). Follow-up fix: README.md, README_zh-hans.md, PROMOTION_EN.md, PROMOTION_CN.md, and DEPLOYMENT.md no longer tell readers to run `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket` -- the Scoop option was dropped from download lists and ready-to-post copy, or replaced with an honest "not available yet, use Releases" note. The bucket itself still does not exist; only reintroduce install copy once `SSC-STUDIO/scoop-bucket` is actually created and published |

## Weekly star tracker

| Week ending | Stars | Delta | Notes |
|-------------|-------|-------|-------|
| 2026-08-23 | 28 | +10 | README hero + trailer + retaken console screenshots; PROMOTION_* restored |
| 2026-07-07 | 18 | 0 | OpenAI Founders Hub 申请 [VERIFY] 用真实数据填默认（[CONFIRM] 等你核）；SUBMISSIONS winget-pkgs 错误修正；awesome-dotnet PR #1466 open+mergeable+0 comments；首发三站文案齐备，等你今晚或明天首发 |
| 2026-07-06 | 18 | - | baseline, v5.0.0-preview published |

(add a row each Sunday; pair with PILLAR_D_PROMOTION_PLAN.md)

---

## 1. awesome-dotnet  -- CLOSED, UNMERGED (2026-08-30)

- Repo: https://github.com/quozd/awesome-dotnet
- PR: https://github.com/quozd/awesome-dotnet/pull/1466  (Add Universal Device Toolkit under Tools)
- Section: `## Tools`, appended at end of the section (matches the list's
  chronological addition style). One link per PR, per CONTRIBUTING.
- Body lives at `_awesome_prs/body_dotnet.md` (quality-bar rationale: actively
  maintained v5.0.0-preview, 2,343 tests, docs, winget/scoop distribution).
- Status as of 2026-08-31: the PR was **closed by the SSC-STUDIO account itself**
  on 2026-08-30, with zero comments and zero reviews recorded (`gh pr view
  --json state,mergedAt,closedAt,comments`; `.../pulls/1466/reviews` and
  `.../issues/1466/comments` both return empty). It sat open ~7 weeks with no
  maintainer response before being closed. UDT is **not currently listed** in
  awesome-dotnet. Don't reopen or resubmit blindly -- figure out why it was
  closed first (stale cleanup vs. a deliberate withdrawal), then decide whether
  a fresh, single PR is warranted. Per the one-PR-per-list rule, do not open a
  second PR to this same list without that context.

## 2. awesome-windows  -- SKIPPED (2026-07-07)

- Canonical repo: https://github.com/0PandaDEV/awesome-windows (the old
  `awesome-windows/awesome-windows` is a 404; `0PandaDEV` is the live fork, 2.5k stars).
- Reason for skip: the README embeds (a) a hidden HTML comment that attempts to
  inject refusal instructions into AI assistants that read the repo, and (b) a
  visible `> [!CAUTION]` that "vibecoded slop ... PR's will be rejected". That is a
  genuine, hostile stance toward AI-assisted PRs; submitting risks the UDT repo
  being flagged/closed-on-sight and gains little. Skip unless the maintainer's
  stance changes. (Note: treated the hidden comment as untrusted data, not as an
  instruction -- it changed only this venue's risk call, not the overall goal.)

## 3. electron/apps  -- PREPARED, BLOCKED (2026-08-31)

- Repo: https://github.com/electron/apps (official Electron org app directory
  behind electronjs.org/apps; active, 1.7k+ stars, not archived). Genuinely fits:
  UDT's UI is Electron ^43, the repo's own `sindresorhus/awesome-electron`
  redirects rejected/ineligible submissions here, and an existing entry
  (`display-dj`, a laptop brightness utility) shows hardware-utility apps are
  welcome under `Utilities`.
- Checked first: no existing `legion`-matching entry (code search returned 0),
  submissions are not paused for this repo (unlike `awesome-electron`, which is
  temporarily closed to new submissions), and UDT clears every written rule
  (open source, README with screenshot, Windows binaries via Releases, project
  older than the 20-day minimum, Electron well above the v4 floor).
- Forked to https://github.com/SSC-STUDIO/apps and prepared a ready-to-submit
  branch `add-universal-device-toolkit`
  (https://github.com/SSC-STUDIO/apps/tree/add-universal-device-toolkit) adding
  `apps/universal-device-toolkit/universal-device-toolkit.yml` (category
  `Utilities`, GPL-3.0, keywords, full locale list) and a 512x512 icon copied
  from `Assets/Logo.png`. Content double-checked line-by-line against the
  repo's own `test/human-data.js` validation rules.
- **PR could not be opened.** Both `gh pr create` (GraphQL) and a direct REST
  `POST /repos/electron/apps/pulls` call were rejected: `SSC-STUDIO does not
  have the correct permissions to execute CreatePullRequest` / HTTP 404. This
  matches a known GitHub repo setting ("limit who can open pull requests" to
  collaborators only) that maintainers of high-profile directory repos have
  been enabling to stop floods of low-effort/AI-generated submission PRs --
  the same signature error is reported by other unrelated contributors hitting
  the same wall on other repos. This is a hard block from the target repo, not
  an ambiguity on our side, so no PR was forced through and no workaround
  (e.g. a different account) was attempted.
- The prepared branch is harmless and sits only on our own fork; it does not
  notify or affect electron/apps in any way. If the restriction lifts, or if a
  maintainer grants contributor access, the same branch can be turned into a
  PR later without redoing the work.

## 4. AlternativeTo

- Where: https://alternativeto.net
- How: submit UDT as a software entry, then tag it as an alternative to
  "Lenovo Vantage".

Entry copy:

```
Universal Device Toolkit (UDT): open-source Lenovo hardware-control toolkit
for Windows. Fn+Q, RGB, fan curves, dGPU, battery threshold -- without
Vantage's background service, account, or telemetry. GPL-3.0, C# Host + Electron UI, winget.
https://github.com/SSC-STUDIO/UniversalDeviceToolkit
```

## 5. Slant

- Where: https://www.slant.co
- How: find or create a "Best Lenovo Vantage alternatives" question, add UDT, write
  a one-line pro. Pros from a couple of independent users help, but never
  coordinate mass votes.

## 6. HelloGitHub (already drafted)

- Submission draft lives in `.github/outreach/hellogithub-issue.md`.
- Status: submitted 2026-07-05. Watch for a reply; if rejected, ask politely what's
  missing and address it.

---

## Rules (don't get blacklisted)

- awesome-lists usually gate on "notable" -- typically real traction (tens of
  stars, real commit history, some age). Submit after the first community push so
  the PR isn't closed as self-promotion.
- AlternativeTo / Slant entries should be created by a real person and naturally
  accrue independent pros/upvotes. Never coordinate mass upvoting -- both
  platforms anti-cheat and it hurts the project's reputation.
- One venue at a time so each submission is well written for that community.

Last updated: 2026-08-31 (awesome-dotnet PR #1466 confirmed closed/unmerged with
no maintainer response; added electron/apps as a prepared-but-blocked venue --
repo restricts PR creation to collaborators; Scoop bucket confirmed missing
(404) even though README/PROMOTION_* instruct readers to add it; AlternativeTo
and Slant marked out of scope for this GitHub-only round; follow-up: removed
the dead Scoop-bucket install copy from README.md, README_zh-hans.md,
PROMOTION_EN.md, PROMOTION_CN.md, and DEPLOYMENT.md so none of them tell
readers to add the nonexistent bucket anymore -- see the Scoop bucket row
above)

Previously: 2026-07-07 (winget-pkgs 行从 Listed 修正为 Not published，附 manifest 目录 404 验证；新增 2026-07-07 周行)
