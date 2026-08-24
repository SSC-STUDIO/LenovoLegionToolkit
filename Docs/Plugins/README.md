# Documentation index

Current author-facing docs for **Universal Device Toolkit Plugins**.
Published host baseline: **Universal Device Toolkit v5.0.2** (see `../../Plugins/HostBaseline/host-release.json`) until a v6 ZIP exists. Official 2.x plugins require host **v6.0.0** and publish stable **2.0.0** packages to `plugin-catalog`.
CLI entry: **`../../Plugins/udt-plugin.cmd`** (`llt-plugin.cmd` is a compatibility alias).

## Start here

| Doc | Audience | Notes |
|-----|----------|-------|
| [PLUGIN_QUICKSTART.md](./PLUGIN_QUICKSTART.md) | New authors | Shortest path: doctor → init → dev → package |
| [PLUGIN_DEVELOPMENT.md](./PLUGIN_DEVELOPMENT.md) | Authors / reviewers | Paths, validation profiles, versioning, Workbench |
| [HOST_INTEGRATION.md](./HOST_INTEGRATION.md) | Host/plugin maintainers | ABI, lifecycle, host-side UI integration |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Authors / maintainers | Layout, SDK layers, Shared, tooling |
| [CODING_STANDARDS.md](./CODING_STANDARDS.md) | Everyone writing C# / plugin web UI | Style and anti-patterns |
| [SDK_CHANGELOG.md](./SDK_CHANGELOG.md) | Authors tracking host/SDK | Compatibility matrix |
| [BUILD_SMOKE.md](./BUILD_SMOKE.md) | CI / local gates | Minimal smoke sequence |
| [RELEASE_AND_MIGRATION.md](./RELEASE_AND_MIGRATION.md) | Maintainers / release operators | Monorepo topology, rolling release assets, and legacy-client upgrade |
| [AI_AGENT_WORKFLOW.md](./AI_AGENT_WORKFLOW.md) | Automation / AI agents | JSON reports under `artifacts/agent/` |

## Root docs (repo)

| Doc | Notes |
|-----|-------|
| [../../README.md](../../README.md) / [../../README_zh-hans.md](../../README_zh-hans.md) | Product overview + catalog |
| [../../CONTRIBUTING.md](../../CONTRIBUTING.md) | Contribution process |
| [../../CHANGELOG.md](../../CHANGELOG.md) | Project changelog |
| [../SECURITY.md](../SECURITY.md) | Vulnerability reporting |
| [../../Plugins/KNOWLEDGE_BASE.md](../../Plugins/KNOWLEDGE_BASE.md) | Durable engineering rules (ABI, migration paths) |
| [../../Plugins/CHANGELOG.md](../../Plugins/CHANGELOG.md) | Plugin history and migration notes |

When updating product facts, edit **README / PLUGIN_*** / manifests first.

## Historical (not current SoT)

Retired WPF plugin visual notes live under [../archive/Plugins/](../archive/Plugins/): [VISUAL_AUDIT_20260718.md](../archive/Plugins/VISUAL_AUDIT_20260718.md), [VISUAL_DESIGN_RECOMMENDATIONS.md](../archive/Plugins/VISUAL_DESIGN_RECOMMENDATIONS.md).
