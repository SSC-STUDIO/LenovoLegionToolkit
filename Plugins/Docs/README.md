# Documentation index

Current author-facing docs for **Universal Device Toolkit Plugins**.
Host baseline: **Universal Device Toolkit v5.0.0** (see `../Dependencies/Host/host-release.json`).
CLI entry: **`../udt-plugin.cmd`** (`llt-plugin.cmd` is a compatibility alias).

## Start here

| Doc | Audience | Notes |
|-----|----------|-------|
| [PLUGIN_QUICKSTART.md](./PLUGIN_QUICKSTART.md) | New authors | Shortest path: doctor → init → dev → package |
| [PLUGIN_DEVELOPMENT.md](./PLUGIN_DEVELOPMENT.md) | Authors / reviewers | Paths, validation profiles, versioning, Workbench |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Authors / maintainers | Layout, SDK layers, Shared, tooling |
| [CODING_STANDARDS.md](./CODING_STANDARDS.md) | Everyone writing C# / XAML | Style and anti-patterns |
| [SDK_CHANGELOG.md](./SDK_CHANGELOG.md) | Authors tracking host/SDK | Compatibility matrix |
| [BUILD_SMOKE.md](./BUILD_SMOKE.md) | CI / local gates | Minimal smoke sequence |
| [AI_AGENT_WORKFLOW.md](./AI_AGENT_WORKFLOW.md) | Automation / AI agents | JSON reports under `artifacts/agent/` |

## Root docs (repo)

| Doc | Notes |
|-----|-------|
| [../README.md](../README.md) / [../README.zh-CN.md](../README.zh-CN.md) | Product overview + catalog |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | Contribution process |
| [../CHANGELOG.md](../CHANGELOG.md) | Project changelog |
| [../SECURITY.md](../SECURITY.md) | Vulnerability reporting |
| [../KNOWLEDGE_BASE.md](../KNOWLEDGE_BASE.md) | Durable engineering rules (ABI, migration paths) |
| [../BUGS.md](../BUGS.md) | Known issues tracker |

## Historical / campaign material

The following were produced during launch and sprint days. **Version numbers and plugin counts may be stale**; do not treat them as source of truth. Prefer manifests + docs above.

| Doc | Kind |
|-----|------|
| `AUTONOMOUS_DAY4_SUMMARY.md`, `SPRINT_DAY3_UPDATE.md`, `DAY5_HANDOVER.md` | Sprint notes |
| `HANDOVER_SUMMARY_2026-07-07.md`, `../HANDOVER_SUMMARY_2026-07-06.md` | Session handovers |
| `PROMOTION.md`, `PROMOTION_CHECKLIST.md`, `PROMOTION_COPIES.md` | Marketing drafts |
| `REDDIT_POSTS.md`, `REDDIT_PUBLISHING_CHECKLIST.md`, `DAY6_REDDIT_PUBLISHING_GUIDE.md` | Social drafts |
| `DEVTO_POST.md`, `HN_POST.md`, `CHIPELL_POST.md` | Blog drafts |
| `STAR_CAMPAIGN_STATUS.md`, `USER_ACTION_GUIDE.md` | Campaign trackers |

When updating product facts, edit **README / PLUGIN_*** / manifests first, then regenerate marketing copy if needed.
