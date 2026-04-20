# Plugin Archetypes

These archetypes are the source of truth for the authoring workflow.

- `settings-only`: settings page plugin with no separate feature page
- `feature-settings`: feature page plus settings page
- `runtime-optimization`: feature page, settings page, runtime stub, and optimization entry point

The CLI reads `template.json` from each archetype directory and emits the scaffolded project from that capability profile.
