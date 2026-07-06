# UDT Directory and Awesome-List Submissions

Track where UDT is submitted for discovery beyond social posts. A listing on a
curated list converts well because the visitor already wants a tool like this.

## Status tracker

| Venue | Type | Status | Date | Notes |
|-------|------|--------|------|-------|
| awesome-dotnet | awesome-list PR | Not started | - | C# / .NET app; verify curator guidelines |
| awesome-windows | awesome-list PR | Not started | - | confirm canonical repo via the Awesome index |
| AlternativeTo | database entry | Not started | - | suggest as alt to "Lenovo Vantage" |
| Slant | list entry | Not started | - | add to a "Lenovo Vantage alternatives" question |
| HelloGitHub | CN feature | Submitted | 2026-07-05 | see .github/outreach/hellogithub-issue.md |
| winstall.app | winget mirror | Listed | - | winstall.app/apps/SSC-STUDIO.LenovoLegionToolkit |
| winget-pkgs | package manager | Listed | - | SSC-STUDIO.LenovoLegionToolkit published |
| Microsoft Store | store listing | Not planned | - | optional; GPL-3.0 WPF, big lift |
| Scoop bucket | package manager | Verify | - | confirm whether a manifest exists |

## Weekly star tracker

| Week ending | Stars | Delta | Notes |
|-------------|-------|-------|-------|
| 2026-07-06 | 18 | - | baseline, v5.0.0-preview published |

(add a row each Sunday; pair with PILLAR_D_PROMOTION_PLAN.md)

---

## 1. awesome-dotnet

- Repo: https://github.com/quozd/awesome-dotnet (verify the curator + CONTRIBUTING)
- How: open a PR adding UDT under the relevant section (likely "GUI" / "Hardware").
  Read the guidelines first; awesome-lists often want "notable" projects (commits,
  age, some stars). Screenshot helps.

PR body:

```
Adding Universal Device Toolkit (UDT), a GPL-3.0 WPF/.NET 10 app for Lenovo laptop
hardware control with a first-class plugin system. No telemetry, no background
service, winget-distributed. Actively maintained (v5.0.0-preview out this week).

Repo: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
```

## 2. awesome-windows

- Find the canonical repo via the Awesome index (https://github.com/sindresorhus/awesome)
  and follow its CONTRIBUTING. Verify before opening a PR.
- UDT fits a "Utilities" / "System" section.

## 3. AlternativeTo

- Where: https://alternativeto.net
- How: submit UDT as a software entry, then tag it as an alternative to
  "Lenovo Vantage".

Entry copy:

```
Universal Device Toolkit (UDT): open-source Lenovo hardware-control + device-plugin
toolkit for Windows. Fn+Q, RGB, fan curves, dGPU, battery threshold -- without
Vantage's background service, account, or telemetry. GPL-3.0, C#/WPF, winget.
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

Last updated: 2026-07-06
