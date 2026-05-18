# Lenovo Legion Toolkit Deployment Guide

## Overview

This document describes the build, test, and deployment processes for Lenovo Legion Toolkit (LLT). It covers development workflows, CI/CD pipelines, and release procedures.

## Prerequisites

### Development Environment

- **Operating System**: Windows 10 (1809+) or Windows 11
- **.NET SDK**: .NET 10.0 or later
- **Runtime**: .NET 10.0 Desktop Runtime (x64)
- **IDE**: Visual Studio 2022 or VS Code
- **Git**: Latest version with Git LFS support

### Required Tools

```bash
# Install .NET 10.0 SDK
winget install Microsoft.DotNet.SDK.10

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
<TargetFramework>net10.0-windows</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<OutputType>WinExe</OutputType>
<AssemblyName>LenovoLegionToolkit</AssemblyName>
<Version>3.x.x</Version>
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
    --output ./Build/framework-dependent

# Self-contained deployment (no runtime required)
dotnet publish LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output ./Build/self-contained
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

#### Build Pipeline (`Build.yml`)

```yaml
# Triggers
on:
  push:
    branches: [master, develop]
  pull_request:
    branches: [master]

# Jobs (current repository workflow)
jobs:
  build:
    runs-on: windows-2022
    steps:
      - uses: actions/checkout@v6
      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x
      - name: Build
        run: .\Make.bat
      - name: Upload artifact
        uses: actions/upload-artifact@v7
        with:
          name: installer
          path: BuildInstaller/LenovoLegionToolkitSetup.exe
```

#### CI Tests Pipeline (`Ci-tests.yml`)

```yaml
jobs:
  build-and-test:
    runs-on: windows-2022
    steps:
      - uses: actions/checkout@v6
      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore
      - run: dotnet build --configuration Release --no-restore
```

#### Release Pipeline (`Release.yml`)

```yaml
on:
  push:
    tags:
      - "v*.*.*"

jobs:
  release:
    runs-on: windows-2022
    steps:
      - name: Build and Package
        run: |
          dotnet build --configuration Release
          dotnet publish -c Release -o ./publish
      - name: Create Installer
        run: iscc MakeInstaller.iss
      - name: Create Release
        uses: softprops/action-gh-release@v3
        with:
          files: |
            installer/*.exe
            publish/*.zip
```

## Installer Creation

### Using Inno Setup

The project uses Inno Setup (`MakeInstaller.iss`) to create Windows installers:

```bash
# Build installer (requires Inno Setup installed)
iscc MakeInstaller.iss

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
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z

# Create GitHub Release
gh release create vX.Y.Z \
    --title "Lenovo Legion Toolkit vX.Y.Z" \
    --notes "$(cat CHANGELOG.md | head -n 50)"
```

## Localization Delivery (Crowdin)

LLT translations are managed by `crowdin.yml` at repository root.

```bash
# Upload base source strings from all resource modules
crowdin upload sources --config crowdin.yml

# Upload existing local translations
crowdin upload translations --config crowdin.yml

# Download translated resources
crowdin download --config crowdin.yml
```

After downloading translations:
1. Run structural audit (`missing/extra/placeholder`) across all `Resource*.resx`.
2. Build WPF project and full solution.
3. Update `CHANGELOG.md` under `[Unreleased]` for user-visible localization fixes.

## Distribution Channels

### Primary Channels

1. **GitHub Releases**
   - Latest stable releases
   - Manual installation required
   - Auto-updater support

2. **winget Package Manager**
   - Target package ID: `SSC-STUDIO.LenovoLegionToolkit`
   - After acceptance: `winget install SSC-STUDIO.LenovoLegionToolkit`
   - Automatic updates via Windows Package Manager

3. **Scoop**
   - `scoop install extras/lenovolegiontoolkit`
   - Community maintained bucket

### Alternative Channels

- **Chocolatey**: Community maintained
- **Ninite**: Managed deployments
- **MSI Wrapper**: Enterprise deployments

### Winget Submission

The maintainer-side manifest draft lives under `Packaging/winget`. The canonical submission target is the upstream `microsoft/winget-pkgs` repository.

Before submitting a new version:

1. Publish a stable GitHub Release with `LenovoLegionToolkit_vX.Y.Z_Setup.exe` and `LenovoLegionToolkit_vX.Y.Z_SHA256.txt`.
2. Do not draft a new version manifest until the release asset URL and installer SHA256 are final.
3. Copy the installer SHA256 from the release checksum file into the winget installer manifest.
4. Keep `PackageIdentifier` as `SSC-STUDIO.LenovoLegionToolkit` unless winget review requires a rename before first acceptance.
5. Validate locally on Windows:
   ```powershell
   winget validate manifests\s\SSC-STUDIO\LenovoLegionToolkit\X.Y.Z
   winget install --manifest manifests\s\SSC-STUDIO\LenovoLegionToolkit\X.Y.Z
   winget uninstall SSC-STUDIO.LenovoLegionToolkit
   ```
6. Submit the version folder to `microsoft/winget-pkgs` and wait for automated validation.

Use the GitHub Release URL as the winget installer source. Do not use mirror URLs in winget manifests.

### Scoop Submission

The maintainer workflow for Scoop lives under `Packaging/scoop`. The canonical submission target is the upstream `ScoopInstaller/Extras` bucket.

Before submitting a new version:

1. Publish a stable GitHub Release with the final installer and checksum file.
2. Do not draft or submit a Scoop manifest update until the installer URL and SHA256 are final.
3. Update the community `lenovolegiontoolkit` manifest in `ScoopInstaller/Extras` with the new version, URL, and hash.
4. Validate on a clean machine:
   ```powershell
   scoop bucket add extras
   scoop install extras/lenovolegiontoolkit
   scoop update extras/lenovolegiontoolkit
   scoop uninstall lenovolegiontoolkit
   ```
5. Submit the manifest update to `ScoopInstaller/Extras`.

### High-Traffic Release Readiness

When promoting a release on Chinese social platforms or after winget acceptance:

- Pin the current GitHub Release URL, winget command, and SHA256 file in all announcement posts.
- Link to the active `SSC-STUDIO/LenovoLegionToolkit` repository in all promotion content.
- Keep mirrors optional and checksum-backed; GitHub Releases and winget remain the authoritative download channels.
- Mention Scoop as a community-maintained channel, not the canonical release source.
- Watch GitHub Issues for recurring reports: antivirus false positives, missing .NET 10 Desktop Runtime, unsupported machines, Lenovo Vantage conflicts, RGB/Vanguard conflicts, and plugin download failures.
- Confirm `Build`, `CI Tests`, `CodeQL`, and release packaging workflows are green before pushing a promotional post.
- Reuse `Docs/PROMOTION_CN.md` for platform copy so public claims stay consistent with the README and release notes.

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
   gh release delete vX.Y.Z --yes
   gh release create vA.B.C \
       --title "Lenovo Legion Toolkit vA.B.C (Hotfix)" \
       --notes "Emergency rollback from vX.Y.Z"
   ```

2. **winget Update**
   ```bash
   # Users will automatically get previous version
   winget upgrade --manifest manifest.yaml
   ```

### Version Recovery

```bash
# Checkout previous stable tag
git checkout vA.B.C
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
- **Crash Reports**: Local log files provided manually by users

## Security Considerations

### Build Security

- Signed assemblies (code signing certificate)
- NuGet package verification
- Dependency vulnerability scanning (Dependabot)

### Dependency Audit

Before a release candidate, verify the centrally managed package set from the repository root:

```bash
dotnet list LenovoLegionToolkit.sln package --outdated --include-transitive --no-restore
dotnet list LenovoLegionToolkit.sln package --vulnerable --include-transitive --no-restore
```

On Windows builds from a WSL UNC path, prefer `--no-restore` after a successful restore/build to avoid repeating slow restore work over `\\wsl.localhost`. CsWin32 metadata packages may appear as transitive packages whose latest version is not found in the configured sources; treat those as generated-tool metadata, not as direct application dependencies.

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
