# Universal Device Toolkit

## Project Overview
Open-source Universal Device Toolkit for Windows device management. Large C# .NET solution with CLI, WPF UI, and multiple library projects.

## Tech Stack
- Language: C# (.NET)
- Build: Visual Studio (.sln), Directory.Build.props, Directory.Packages.props
- Installer: BuildInstaller/, InnoDependencies/

## Development Rules
- Branch naming: follow team or [CONTRIBUTING.md](CONTRIBUTING.md) conventions (avoid hard-coding a single branch name in docs).
- **Changelog**: Record **user-visible** changes that will ship in the next release under `## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md); when cutting a release, move that section under the version heading. Pre-release or pre-merge self-corrections (bugs fixed before the behavior ever shipped, or iterative fixes on the same unreleased feature) do **not** need a separate line each time—see the bilingual **「更新日志维护指南」** / changelog section in [AGENTS.md](AGENTS.md).
- Follow [CONTRIBUTING.md](CONTRIBUTING.md) guidelines
- Build with Visual Studio or `dotnet build`
- Run `Make.bat` for release builds (includes a full clean); use `Make.bat -clean` or `Clean.bat` for clean-only

## Code Style
- Follow .NET/C# conventions per CONTRIBUTING.md
- Use centralized package management (Directory.Packages.props)
- Keep CLI and UI logic separated

## Key Paths
- `UniversalDeviceToolkit.sln` — solution file
- `UniversalDeviceToolkit.WPF/` — WPF UI application
- `UniversalDeviceToolkit.CLI/` — CLI application
- `UniversalDeviceToolkit.Lib/` — core library (assembly: `LenovoLegionToolkit.Lib.dll` for plugin ABI)
- `Assets/` — UI assets
- `Build/` — build output
- `Docs/` — documentation
- `CONTRIBUTING.md` — contribution guidelines
