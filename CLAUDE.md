# Lenovo Legion Toolkit

## Project Overview
Open-source Lenovo Legion laptop management toolkit. Large C# .NET solution with CLI, WPF UI, and multiple library projects.

## Tech Stack
- Language: C# (.NET)
- Build: Visual Studio (.sln), Directory.Build.props, Directory.Packages.props
- Installer: BuildInstaller/, InnoDependencies/

## Development Rules
- Branch convention: work on `codex/ai-LenovoLegionToolkit` branch
- Always update CHANGELOG.md on releases
- Follow CONTRIBUTING.md guidelines
- Build with Visual Studio or `dotnet build`
- Run Clean.bat before fresh builds

## Code Style
- Follow .NET/C# conventions per CONTRIBUTING.md
- Use centralized package management (Directory.Packages.props)
- Keep CLI and UI logic separated

## Key Paths
- `LenovoLegionToolkit.CLI/` — CLI application
- `LenovoLegionToolkit.CLI.Lib/` — CLI library
- `Assets/` — UI assets
- `Build/` — build output
- `Docs/` — documentation
- `CONTRIBUTING.md` — contribution guidelines
