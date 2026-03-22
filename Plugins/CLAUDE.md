# Lenovo Legion Toolkit Plugins

## Project Overview
Official plugins for Lenovo Legion Toolkit (LLT). C# .NET solution with SDK and plugin architecture.

## Tech Stack
- Language: C# (.NET)
- Build: Visual Studio (.sln), Make.bat, Directory.Build.props
- Structure: Plugins/, SDK/, Dependencies/, Build/

## Development Rules
- Branch convention: work on `codex/ai-LenovoLegionToolkit-Plugins` branch
- Always update CHANGELOG.md on releases
- Build with `Make.bat` or Visual Studio
- Follow SDK interfaces when creating new plugins
- Update store.json when adding/modifying plugins

## Code Style
- Follow .NET/C# conventions
- Use Directory.Build.props for shared build settings
- Keep plugins self-contained in Plugins/ subdirectories

## Key Paths
- `Plugins/` — individual plugin projects
- `SDK/` — plugin SDK interfaces
- `Dependencies/` — shared dependencies
- `Build/` — build output
- `Scripts/` — utility scripts
- `store.json` — plugin registry
