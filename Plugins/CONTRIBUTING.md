# Contributing to UniversalDeviceToolkit-Plugins

Thank you for your interest in contributing! ??

This guide will help you get started with the Universal Device Toolkit plugin ecosystem.

## ?? Table of Contents

- [Ways to Contribute](#ways-to-contribute)
- [Development Setup](#development-setup)
- [Plugin Development Guide](#plugin-development-guide)
- [Coding Standards](#coding-standards)
- [UI Guidelines](#ui-guidelines)
- [Pull Request Process](#pull-request-process)
- [Community](#community)

## Ways to Contribute

There are many ways to contribute ¡ªno contribution is too small!

### ?? Report Bugs

- Use the [GitHub Issues](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/issues) page
- Check if the bug has already been reported
- Include: OS version, .NET version, steps to reproduce, expected vs actual behavior

### ?? Suggest Features

- Open a [Feature Request](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/issues/new?labels=enhancement)
- Describe the use case and why it would be valuable

### ?? Submit Code

- Fix bugs, implement features, improve documentation
- Follow the coding standards below
- Test your changes before submitting

### ? Improve Documentation

- Fix typos, clarify wording, add examples
- Translate to other languages (we currently support English + Chinese)

### ??§© Create a Plugin

- Build a new plugin using our SDK
- Two paths: **Contributor Path** (community plugins) or **Official Store Path** (official plugins)

## Development Setup

### Prerequisites

- **Windows 10/11** (x64)
- **.NET 10 SDK** or later
- **Visual Studio 2022** (17.8+) or **Visual Studio Code**
- **Git**

### Building the Project

```bash
# Clone the repository
git clone https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins.git
cd UniversalDeviceToolkit-Plugins

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Or use the provided build script
.\Make.bat
```

### Running Plugins in the Workbench

```bash
# Launch the standalone Plugin Workbench
cd Tools/PluginWorkbench
dotnet run

# In the Workbench:
# 1. Click "Load Plugin"
# 2. Select the plugin's build output ZIP or DLL
# 3. Switch between Light/Dark themes to verify UI
# 4. Test feature and settings pages
```

## Plugin Development Guide

### Quick Start

```bash
# Use the plugin tooling CLI to scaffold a new plugin
cd Tools/PluginTooling.Cli
dotnet run -- init MyPlugin
```

This creates a new plugin at `Plugins/MyPlugin/` with the standard structure.

### Contribution Paths

This repository supports two paths:

#### 1. Contributor Path (Community Plugins)

Use this path for community-contributed plugins.

1. Run `doctor` to check environment
2. Scaffold with `new`
3. Build with `build`
4. Preview with `preview`
5. Validate with `validate --profile contributor`
6. Pack with `pack`

You do **not** need `store-entry.json` for this path.

#### 2. Official Store Path

Only use this path when the plugin is meant to ship from the official repository.

Additional requirements:
- `store-entry.json`
- Plugin `CHANGELOG.md`
- Root `CHANGELOG.md` entry
- Successful `official-candidate` validation

### Plugin Structure

```
Plugins/MyPlugin/
©À©¤©¤€ MyPlugin.cs                          # Plugin entry point (implements IPlugin)
©À©¤©¤€ MyPluginPlugin.cs                    # Plugin attribute and metadata
©À©¤©¤€ MyPluginPage.xaml                    # Feature page UI (optional)
©À©¤©¤€ MyPluginPage.xaml.cs                 # Feature page code-behind
©À©¤©¤€ MyPluginSettingsControl.xaml         # Settings page UI
©À©¤©¤€ MyPluginSettingsControl.xaml.cs      # Settings page code-behind
©À©¤©¤€ Resources/
â”?  ©¸©¤©¤€ Resource.resx                    # Localized strings
©À©¤©¤€ plugin.manifest.json                 # Plugin manifest (authoring)
©À©¤©¤€ store-entry.json                     # Store metadata (official only)
©¸©¤©¤€ LenovoLegionToolkit.Plugins.MyPlugin.csproj
```

### Implementing IPlugin

```csharp
using LenovoLegionToolkit.Plugins.SDK;

[Plugin(
    Id = "my-plugin",
    Name = "My Plugin",
    Description = "Does awesome things",
    Author = "Your Name",
    Version = "1.0.0"
)]
public class MyPlugin : IPlugin
{
    public string Id => "my-plugin";
    public string Name => "My Plugin";
    public string Description => "Does awesome things";
    public string Author => "Your Name";
    public string Version => "1.0.0";

    public void Initialize(IPluginHostContext context)
    {
        // Initialize your plugin
    }

    public void Shutdown()
    {
        // Clean up resources
    }
}
```

## UI Guidelines

### ??Do

1. **Use DynamicResource for all colors** ¡ªzero hardcoded brushes:
   ```xaml
   <Border Background="{DynamicResource ControlFillColorDefaultBrush}"
           BorderBrush="{DynamicResource ControlStrokeColorDefaultBrush}">
   ```

2. **Use x:Static for all text** ¡ªzero hardcoded strings:
   ```xaml
   <TextBlock Text="{x:Static resources:Resource.MyPlugin_Title}" />
   ```

3. **Provide fallback UI** ¡ªimplement `BuildFallbackUi()`:
   ```csharp
   public MyPluginPage()
   {
       WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);
   }

   private void BuildFallbackUi()
   {
       // Construct UI programmatically with same structure as XAML
   }
   ```

4. **Add AutomationIds** for testing:
   ```csharp
   AutomationProperties.SetAutomationId(myButton, "MyPluginActionButton");
   ```

5. **Use Grid star-sizing** for responsive layouts

### ??Don't

- Don't hardcode colors (e.g., `Foreground="White"` ¡ªuse DynamicResource instead)
- Don't hardcode strings in XAML or code-behind
- Don't skip fallback UI ¡ªalways implement `BuildFallbackUi()`
- Don't forget to test in both Light and Dark themes

## Coding Standards

### C# Style

- Follow [Microsoft's C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use **4 spaces** for indentation (no tabs)
- Use **PascalCase** for public members, **camelCase** for private fields
- Use **meaningful names** ¡ªavoid abbreviations
- Add XML documentation for public APIs

### Commit Messages

Follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <subject>
```

Examples:
```
feat(network-acceleration): add peak traffic card to dashboard
fix(vive-tool): resolve feature flag status parsing for Windows 11 24H2
docs(readme): update installation instructions
chore(build): bump .NET SDK to 10.0.100
```

### Pull Request Checklist

- [ ] Code builds without errors (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Plugin Workbench verified (Light + Dark themes)
- [ ] `CHANGELOG.md` updated (under `[Unreleased]`)
- [ ] `plugin.manifest.json` updated (if plugin metadata changed)
- [ ] Commit messages follow conventional commits
- [ ] PR description explains what and why

### Pull Request Guidelines

PRs should include:
- Plugin source code
- Test project (for official plugins)
- `plugin.manifest.json`
- Plugin `CHANGELOG.md` (for official candidates)
- `store-entry.json` (only for official store path)

### Things to Avoid

- Do not start new authoring by editing root `store.json` directly
- Do not add source references back to the sibling main repo
- Do not bypass `PluginWorkbench` when checking host-facing UI

## Community

- **GitHub Issues**: [Bug reports & feature requests](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/issues)
- **GitHub Discussions**: [Q&A and community chat](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/discussions)
- **Releases**: [GitHub Releases page](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases)

## Recognition

Contributors are recognized in:
- Release notes (for significant contributions)
- The project README (for plugin authors)
- The [CHANGELOG.md](CHANGELOG.md)

Thank you for contributing to UniversalDeviceToolkit-Plugins! ??
