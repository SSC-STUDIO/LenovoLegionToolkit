# Shared AI Bootstrap

This repository intentionally keeps agent instructions thin.

The machine-wide bootstrap source of truth is:

- Windows: `C:\Users\96152\.agents\skills\workstation-context\SKILL.md`
- WSL: `/mnt/c/Users/96152/.agents/skills/workstation-context/SKILL.md`

Before development, runtime, debugging, release, packaging, or file-editing work:

1. Read that skill.
2. Follow its `Start here (REQUIRED)` section.
3. Treat the shared skill as authoritative for sub-agents too.

If this file and the shared skill disagree, the shared skill wins.

## Repository Rules

- Work in the WSL ext4 checkout by default.
- Do not overwrite dirty local changes unless the user explicitly asks for that exact file or operation.
- Use `Tools/PluginTooling.Cli` as the standard automation entry point.
- Treat root `store.json` as generated release output. Official plugin metadata belongs in `Plugins/<Plugin>/store-entry.json`.
- Update root `CHANGELOG.md` and plugin `CHANGELOG.md` for user-visible plugin UI, workflow, or packaging changes.
- For agent-oriented checks and JSON evidence, follow [Docs/AI_AGENT_WORKFLOW.md](Docs/AI_AGENT_WORKFLOW.md).
