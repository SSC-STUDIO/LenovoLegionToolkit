# UDT Directory and Awesome-List Submissions

Track where UDT is submitted for discovery beyond social posts. A listing on a
curated list converts well because the visitor already wants a tool like this.

## Status tracker

| Venue | Type | Status | Date | Notes |
|-------|------|--------|------|-------|
| awesome-dotnet | awesome-list PR | Submitted | 2026-07-07 | PR #1466 https://github.com/quozd/awesome-dotnet/pull/1466 ; under `## Tools` |
| awesome-windows | awesome-list PR | Skipped | 2026-07-07 | canonical repo 0PandaDEV/awesome-windows; maintainer hostile to AI PRs (hidden anti-AI README comment + visible CAUTION rejecting vibecoded slop). Revisit if stance softens |
| AlternativeTo | database entry | Not started | - | suggest as alt to "Lenovo Vantage" |
| Slant | list entry | Not started | - | add to a "Lenovo Vantage alternatives" question |
| HelloGitHub | CN feature | Submitted | 2026-07-05 | see .github/outreach/hellogithub-issue.md |
| winstall.app | winget mirror | Listed | - | winstall.app/apps/SSC-STUDIO.UniversalDeviceToolkit |
| winget-pkgs | package manager | Not published | 2026-07-07 | id reserved (`SSC-STUDIO.UniversalDeviceToolkit`); `manifests/s/SSC-STUDIO/SSC-STUDIO.UniversalDeviceToolkit` returns 404 as of 2026-07-07 -- use Scoop or Releases installer until a clean PR lands |
| Microsoft Store | store listing | Not planned | - | optional; GPL-3.0, Electron + .NET Host, big lift |
| Scoop bucket | package manager | Verify | - | confirm whether a manifest exists |

## Weekly star tracker

| Week ending | Stars | Delta | Notes |
|-------------|-------|-------|-------|
| 2026-08-23 | 28 | +10 | README hero + trailer + retaken console screenshots; PROMOTION_* restored |
| 2026-07-07 | 18 | 0 | OpenAI Founders Hub 申请 [VERIFY] 用真实数据填默认（[CONFIRM] 等你核）；SUBMISSIONS winget-pkgs 错误修正；awesome-dotnet PR #1466 open+mergeable+0 comments；首发三站文案齐备，等你今晚或明天首发 |
| 2026-07-06 | 18 | - | baseline, v5.0.0-preview published |

(add a row each Sunday; pair with PILLAR_D_PROMOTION_PLAN.md)

---

## 1. awesome-dotnet  -- SUBMITTED (2026-07-07)

- Repo: https://github.com/quozd/awesome-dotnet
- PR: https://github.com/quozd/awesome-dotnet/pull/1466  (Add Universal Device Toolkit under Tools)
- Section: `## Tools`, appended at end of the section (matches the list's
  chronological addition style). One link per PR, per CONTRIBUTING.
- Body lives at `_awesome_prs/body_dotnet.md` (quality-bar rationale: actively
  maintained v5.0.0-preview, 2,343 tests, docs, winget/scoop distribution).
- Watch for maintainer feedback; address any wording/section requests promptly.

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

## 3. AlternativeTo

- Where: https://alternativeto.net
- How: submit UDT as a software entry, then tag it as an alternative to
  "Lenovo Vantage".

Entry copy:

```
Universal Device Toolkit (UDT): open-source Lenovo hardware-control + device-plugin
toolkit for Windows. Fn+Q, RGB, fan curves, dGPU, battery threshold -- without
Vantage's background service, account, or telemetry. GPL-3.0, C# Host + Electron UI, winget.
https://github.com/SSC-STUDIO/UniversalDeviceToolkit
```

## 4. Slant

- Where: https://www.slant.co
- How: find or create a "Best Lenovo Vantage alternatives" question, add UDT, write
  a one-line pro. Pros from a couple of independent users help, but never
  coordinate mass votes.

## 5. HelloGitHub (already drafted)

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

Last updated: 2026-07-07 (winget-pkgs 行从 Listed 修正为 Not published，附 manifest 目录 404 验证；新增 2026-07-07 周行)
