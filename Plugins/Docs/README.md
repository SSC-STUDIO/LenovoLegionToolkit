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
| [VISUAL_AUDIT_20260718.md](./VISUAL_AUDIT_20260718.md) | Maintainers / reviewers | 屏幕适配与视觉效果审计报告（事实与风险） |
| [VISUAL_DESIGN_RECOMMENDATIONS.md](./VISUAL_DESIGN_RECOMMENDATIONS.md) | Maintainers / authors | 屏幕适配与视觉效果改进建议（含路线图与验收） |

## Root docs (repo)

| Doc | Notes |
|-----|-------|
| [../README.md](../README.md) / [../README.zh-CN.md](../README.zh-CN.md) | Product overview + catalog |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | Contribution process |
| [../CHANGELOG.md](../CHANGELOG.md) | Project changelog |
| [../SECURITY.md](../SECURITY.md) | Vulnerability reporting |
| [../KNOWLEDGE_BASE.md](../KNOWLEDGE_BASE.md) | Durable engineering rules (ABI, migration paths) |
| [../BUGS.md](../BUGS.md) | Known issues (open items only) |

When updating product facts, edit **README / PLUGIN_*** / manifests first.
