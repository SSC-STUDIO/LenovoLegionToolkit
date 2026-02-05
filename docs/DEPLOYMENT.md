# Lenovo Legion Toolkit Deployment Guide

## Overview

This document describes the build, test, and deployment processes for Lenovo Legion Toolkit (LLT). It covers development workflows, CI/CD pipelines, and release procedures.

## Prerequisites

### Development Environment

- **Operating System**: Windows 10 (1809+) or Windows 11
- **.NET SDK**: .NET 8.0 or later
- **Runtime**: .NET 8.0 Desktop Runtime (x64)
- **IDE**: Visual Studio 2022 or VS Code
- **Git**: Latest version with Git LFS support

### Required Tools

```bash
# Install .NET 8.0 SDK
winget install Microsoft.DotNet.SDK.8

# Verify installation
dotnet --list-sdks
dotnet --info
```

## Build Configuration

### Solution Structure

```
LenovoLegionToolkit.sln
├── LenovoLegionToolkit.WPF/           # Main application
├── LenovoLegionToolkit.Lib/            # Core library
├── LenovoLegionToolkit.Lib.Automation/ # Automation features
├── LenovoLegionToolkit.Lib.Macro/      # Macro system
├── LenovoLegionToolkit.CLI/            # Command-line tool
├── LenovoLegionToolkit.CLI.Lib/        # CLI core
├── LenovoLegionToolkit.Tests/          # Unit tests
├── LenovoLegionToolkit.PerformanceTest/ # Performance benchmarks
└── LenovoLegionToolkit.SpectrumTester/ # Hardware testing
```

### Build Properties

Key configurations in `Directory.Build.props`:

```xml
<TargetFramework>net8.0-windows</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<OutputType>WinExe</OutputType>
<AssemblyName>LenovoLegionToolkit</AssemblyName>
<Version>2.x.x</Version>
```

## Build Commands

### Local Development Build

```bash
# Debug build (development)
dotnet build LenovoLegionToolkit.sln --configuration Debug

# Release build (production)
dotnet build LenovoLegionToolkit.sln --configuration Release

# Clean rebuild
dotnet clean LenovoLegionToolkit.sln
dotnet build LenovoLegionToolkit.sln --configuration Release --no-incremental
```

### Specific Project Build

```bash
# Build main application only
dotnet build LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj \
    --configuration Release

# Build CLI tool
dotnet build LenovoLegionToolkit.CLI/LenovoLegionToolkit.CLI.csproj \
    --configuration Release

# Build and run tests
dotnet test LenovoLegionToolkit.Tests/LenovoLegionToolkit.Tests.csproj
```

### Release Build with Publish

```bash
# Framework-dependent deployment (requires .NET runtime)
dotnet publish LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj \
    --configuration Release \
    --output ./publish/framework-dependent

# Self-contained deployment (no runtime required)
dotnet publish LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output ./publish/self-contained
```

## Testing

### Unit Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "TestCategory=Unit"
dotnet test --filter "TestCategory=Integration"
```

### Performance Testing

```bash
# Run performance benchmarks
dotnet run --project LenovoLegionToolkit.PerformanceTest/ \
    --configuration Release
```

### Manual Testing Checklist

- [ ] Application launches successfully
- [ ] Power mode changes apply correctly
- [ ] Fan curves save and load
- [ ] RGB controls respond
- [ ] Plugin system loads correctly
- [ ] CLI commands work
- [ ] Automation rules execute
- [ ] Settings persist across restarts

## CI/CD Pipeline

### GitHub Actions Workflow

Located in `.github/workflows/`:

#### Build Pipeline (`build.yml`)

```yaml
# Triggers
on:
  push:
    branches: [master, develop]
  pull_request:
    branches: [master]

# Jobs
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - name: Build
        run: dotnet build --configuration Release
      - name: Test
        run: dotnet test --no-build --verbosity normal
      - name: Publish
        run: dotем publish artifacts
```

#### Release Pipeline

```yaml
on:
  release:
    types: [created]

jobs:
  release:
    runs-on: windows-latest
    steps:
      - name: Build and Package
        run: |
          dotnet build --configuration Release
          dotnet publish -c Release -o ./publish
      - name: Create Installer
        run: iscc make_installer.iss
      - name: Create Release
        uses: softprops/action-gh-release@v2
        with:
          files: |
            installer/*.exe
            publish/*.zip
```

## Installer Creation

### Using Inno Setup

The project uses Inno Setup (`make_installer.iss`) to create Windows installers:

```bash
# Build installer (requires Inno Setup installed)
iscc make_installer.iss

# Output location
output/
├── LenovoLegionToolkit_VERSION_x64.exe
└── LenovoLegionToolkit_VERSION_x86.exe
```

### Installer Contents

The installer packages:
- Main application executable
- Core libraries and dependencies
- Plugin SDK
- Documentation (README, LICENSE)
- Uninstaller configuration

## Version Management

### Semantic Versioning

LLT follows SemVer format: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes or architecture updates
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes and optimizations

### Version Bump Procedure

```bash
# Update version in Directory.Build.props
# Update CHANGELOG.md with changes
# Create git tag
git tag -a v2.14.0 -m "Release v2.14.0"
git push origin v2.14.0

# Create GitHub Release
gh release create v2.14.0 \
    --title "Lenovo Legion Toolkit v2.14.0" \
    --notes "$(cat CHANGELOG.md | head -n 50)"
```

## Distribution Channels

### Primary Channels

1. **GitHub Releases**
   - Latest stable releases
   - Manual installation required
   - Auto-updater support

2. **winget Package Manager**
   - `winget install BartoszCichecki.LenovoLegionToolkit`
   - Automatic updates via Windows Package Manager

3. **Scoop**
   - `scoop install extras/lenovolegiontoolkit`
   - Community maintained bucket

### Alternative Channels

- **Chocolatey**: Community maintained
- **Ninite**: Managed deployments
- **MSI Wrapper**: Enterprise deployments

## Environment-Specific Configurations

### Development Environment

```xml
<Configuration>Debug</Configuration>
<DebugSymbols>true</DebugSymbols>
<Optimize>false</Optimize>
<DefineConstants>DEBUG;TRACE</DefineConstants>
```

### Staging Environment

```xml
<Configuration>Release</Configuration>
<DebugSymbols>false</DebugSymbols>
<Optimize>true</Optimize>
<DefineConstants>TRACE</DefineConstants>
```

### Production Environment

```xml
<Configuration>Release</Configuration>
<DebugSymbols>false</DebugSymbols>
<Optimize>true</Optimize>
<DefineConstants>RELEASE;TRACE</DefineConstants>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

## Rollback Procedures

### Emergency Rollback

1. **GitHub Release Rollback**
   ```bash
   # Revert to previous version
   gh release delete v2.14.1 --yes
   gh release create v2.14.0 \
       --title "Lenovo Legion Toolkit v2.14.0 (Hotfix)" \
       --notes "Emergency rollback from v2.14.1"
   ```

2. **winget Update**
   ```bash
   # Users will automatically get previous version
   winget upgrade --manifest manifest.yaml
   ```

### Version Recovery

```bash
# Checkout previous stable tag
git checkout v2.13.0
dotnet build --configuration Release
# Deploy as hotfix release
```

## Monitoring and Metrics

### Build Health

- **CI/CD Status**: GitHub Actions badges in README
- **Code Coverage**: Tracked per pull request
- **Static Analysis**: Roslyn analyzers enabled

### Release Metrics

- **Download Count**: GitHub Release analytics
- **Issue Tracker**: Bug reports and feature requests
- **Crash Reports**: Application insights (opt-in)

## Security Considerations

### Build Security

- Signed assemblies (code signing certificate)
- NuGet package verification
- Dependency vulnerability scanning (Dependabot)

### Deployment Security

- HTTPS for all downloads
- Release integrity verification
- No telemetry by default

## Troubleshooting

### Common Build Issues

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore LenovoLegionToolkit.sln

# Clear obj/bin folders
dotnet clean
```

### CI/CD Failures

1. Check GitHub Actions workflow logs
2. Verify .NET SDK version compatibility
3. Ensure all secrets are configured
4. Run builds locally for reproduction

### Installer Issues

1. Verify Inno Setup is installed
2. Check signtool availability
3. Validate version number format
