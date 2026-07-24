# Universal Device Toolkit Deployment Guide

## Overview

This document describes the build, test, and deployment processes for Universal Device Toolkit (UDT, formerly Lenovo Legion Toolkit). It covers development workflows, CI/CD pipelines, and release procedures.

Public release copy should use Universal Device Toolkit. Repository paths, assembly names, installer asset names, winget identifiers, and Scoop manifest names may still contain `UniversalDeviceToolkit` during the compatibility transition so existing Lenovo Legion Toolkit users can upgrade directly.

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
UniversalDeviceToolkit.sln
├── UniversalDeviceToolkit.WPF/           # Main application
├── UniversalDeviceToolkit.Lib/            # Core library (assembly: UniversalDeviceToolkit.Lib)
├── UniversalDeviceToolkit.Lib.Automation/ # Automation features
├── UniversalDeviceToolkit.Lib.Macro/      # Macro system
├── UniversalDeviceToolkit.CLI/            # Command-line tool
├── UniversalDeviceToolkit.CLI.Lib/        # CLI core
├── UniversalDeviceToolkit.Tests/          # Unit tests
├── UniversalDeviceToolkit.PerformanceTest/ # Performance benchmarks
└── UniversalDeviceToolkit.SpectrumTester/ # Hardware testing
```

### Build Properties

Key configurations in `Directory.Build.props`:

```xml
<TargetFramework>net10.0-windows</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<OutputType>WinExe</OutputType>
<AssemblyName>UniversalDeviceToolkit</AssemblyName>
<Version>4.x.x</Version>
```

## Build Commands

### Local Development Build

```bash
# Debug build (development)
dotnet build UniversalDeviceToolkit.sln --configuration Debug

# Release build (production)
dotnet build UniversalDeviceToolkit.sln --configuration Release

# Clean rebuild
dotnet clean UniversalDeviceToolkit.sln
dotnet build UniversalDeviceToolkit.sln --configuration Release --no-incremental
```

### Specific Project Build

```bash
# Build main application only
dotnet build UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj \
    --configuration Release

# Build CLI tool
dotnet build UniversalDeviceToolkit.CLI/UniversalDeviceToolkit.CLI.csproj \
    --configuration Release

# Build and run tests
dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj
```

### Release Build with Publish

```bash
# Framework-dependent deployment (requires .NET runtime)
dotnet publish UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj \
    --configuration Release \
    --output ./Build/framework-dependent

# Self-contained deployment (no runtime required)
dotnet publish UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj \
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
dotnet run --project UniversalDeviceToolkit.PerformanceTest/ \
    --configuration Release
```

### README Screenshot Refresh

Regenerate repository README screenshots on an interactive Windows desktop session after major UI changes.

**Target resolution**

| Setting | Value |
|---------|-------|
| Main window (logical) | 1300×850 px — enforced by `Tools/VisualRegression.Smoke` (`WindowWidth` / `WindowHeight`) |
| Capture method | Smoke-owned screen capture from the normalized app window |
| Expected pixel size | Logical size × Windows display scale (1300×850 at 100% DPI; ~1625×1063 at 125% DPI) |
| README display width | 700 px (`width="700"` in README markdown) |

<a id="readme-screenshots"></a>

All README screenshots must use the same window size and capture method so aspect ratio and UI density stay consistent.
Brand binaries (icons/logos) live only under repo-root [`Assets/`](../Assets/README.md).

```powershell
# Build smoke tooling (also builds the app with EnableUdtTestHooks for sandboxed captures)
dotnet build Tools/MainAppPluginUi.Smoke/MainAppPluginUi.Smoke.csproj -c Release -p:Platform=x64

$smoke = "Tools/MainAppPluginUi.Smoke/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64/MainAppPluginUi.Smoke.dll"
$appDir = "UniversalDeviceToolkit.WPF/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64"

# English main shell
dotnet exec $smoke --repo-root . --app-dir $appDir --scenario dashboard --theme dark `
  --lang en --screenshots always --screenshot-dir Build/readme-screenshots-en --disable-animations
Copy-Item Build/readme-screenshots-en/*main-shell-home*.png Assets/Screenshot_main.png -Force

# Simplified Chinese main shell (sandbox lang + WindowSize 1300×850)
dotnet exec $smoke --repo-root . --app-dir $appDir --scenario dashboard --theme dark `
  --lang zh-hans --screenshots always --screenshot-dir Build/readme-screenshots-zh --disable-animations
Copy-Item Build/readme-screenshots-zh/*main-shell-home*.png Assets/Screenshot_zh-hans.png -Force

# Alternate: VisualRegression.Smoke --readme-screenshots --lang zh-hans
# (expects bin/Release/.../win-x64; create a junction from bin/x64/Release if needed).
```

Document the refresh in `CHANGELOG.md` when user-visible UI changes ship.
Last refreshed: 2026-07-13 (`Screenshot_main.png` EN main-shell home; `Screenshot_zh-hans.png` zh-hans main-shell home via MainAppPluginUi.Smoke `--lang`). Target logical window 1300×850 (pixel size scales with display DPI).

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

**PR gate (required):**

| Workflow | Purpose |
|----------|---------|
| `Ci-tests.yml` | Restore, Release build, unit fail-fast, full Windows tests, coverage upload, NuGet vulnerability gate, CLI smoke |
| `CrossPlatformCli.yml` | Cross-platform diagnostics tests on Windows, Ubuntu, and macOS |

**Supplementary (non-blocking on PR):**

| Workflow | Purpose |
|----------|---------|
| `Build.yml` | Full installer build via `Make.bat` on `master` push or manual dispatch |
| `windows.yml` | Weekly/manual Debug+Release parity check |
| `Release.yml` | Tag-driven release packaging |
| `CodeQL.yml` | Security analysis |

Configure branch protection on `master` to require **`CI Tests`** and **`Cross-Platform CLI`** status checks before merge.

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
          path: BuildInstaller/UniversalDeviceToolkit_v*_Full_Setup.exe
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
      - run: dotnet restore UniversalDeviceToolkit.sln --locked-mode
      - run: dotnet build --configuration Release --no-restore
```

CI restores with `--locked-mode` so the committed per-project `packages.lock.json` files (enabled by `RestorePackagesWithLockFile` in `Directory.Build.props`) must match `Directory.Packages.props` / project graphs. Local scripts and `Make.bat` do not pass `--locked-mode` on implicit restore during `dotnet publish`/`dotnet build`, so offline or lock-refresh workflows stay flexible; prefer the locked form when matching CI.

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
        run: ./Scripts/Build-InstallerAssets.ps1 -Version $env:VERSION
      - name: Create Release
        uses: softprops/action-gh-release@v3
        with:
          files: |
            installer/*.exe
            publish/*.zip
```

## Installer Creation

### Self-built WPF installer (Inno Setup retired)

The project ships its own WPF installer (`Tools/Installer`) — Inno Setup and
`MakeInstaller.iss` are retired. Both flavors are produced by
`Scripts/Build-InstallerAssets.ps1` (also wired into `Make.bat` and the Release
workflow):

```bash
# Build both installers (requires the payload zips in release-assets/)
./Scripts/Build-InstallerAssets.ps1 -Version X.Y.Z

# Output location
BuildInstaller/
├── UniversalDeviceToolkitSetup-Full.exe     # offline: payload zip embedded
└── UniversalDeviceToolkitSetup-Online.exe   # ~0.3 MB: downloads payload at install time
```

The installer follows the OS display language and Windows light/dark mode.
Its wizard includes language and device-model pages whose answers are written
to the app's first-run state (`lang` / `device-setup`), so the app does not
ask again on first launch; non-bundled language packs are downloaded from the
stable resource catalog during setup. It supports `--uninstall`, `--silent`,
`--dir=<path>`, `--lang=<culture>`, `--device-pack=<id>` and `--delete-data`.

### Installer Contents

The installer packages:
- Main application executable
- Core libraries and dependencies
- Plugin SDK
- Documentation (README, LICENSE)
- Uninstaller configuration

## Version Management

### Semantic Versioning

UDT follows SemVer format: `MAJOR.MINOR.PATCH`

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
    --title "Universal Device Toolkit vX.Y.Z" \
    --notes-file release-notes.md
```

## Localization Delivery (Crowdin)

UDT translations are managed by `crowdin.yml` at repository root.

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
   - Target package ID: `SSC-STUDIO.UniversalDeviceToolkit` (kept under the old name during the Universal Device Toolkit rename)
   - After acceptance: `winget install SSC-STUDIO.UniversalDeviceToolkit`
   - Automatic updates via Windows Package Manager

3. **Scoop**
   - `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket`
   - `scoop install ssc-studio/lenovolegiontoolkit`
   - Manifest name remains `lenovolegiontoolkit` for now so existing installs keep upgrading cleanly

### Alternative Channels

- **Chocolatey**: Community maintained
- **Ninite**: Managed deployments
- **MSI Wrapper**: Enterprise deployments

### Winget Submission

The maintainer-side manifest draft lives under `Packaging/winget`. The canonical submission target is the upstream `microsoft/winget-pkgs` repository.

Before submitting a new version:

1. Publish a stable GitHub Release with Full and Online assets (`UniversalDeviceToolkit_vX.Y.Z_Full_Setup.exe`, `UniversalDeviceToolkit_vX.Y.Z_Online_Setup.exe`, portable ZIPs as needed), the compatibility alias `UniversalDeviceToolkit_vX.Y.Z_Setup.exe`, and `UniversalDeviceToolkit_vX.Y.Z_SHA256.txt`.
2. Do not draft a new version manifest until the release asset URL and installer SHA256 are final.
3. Generate the versioned winget folder and Scoop draft from the final release metadata:
   ```powershell
   .\Packaging\Prepare-PackageManifests.ps1 -Version X.Y.Z -ReleaseDate YYYY-MM-DD -InstallerSha256 <SHA256>
   ```
4. Keep `PackageIdentifier` as `SSC-STUDIO.UniversalDeviceToolkit` during the Universal Device Toolkit transition unless winget review requires a coordinated rename.
5. Validate the package metadata against the release checksum manifest, then validate locally on Windows:
   ```powershell
   .\Packaging\Test-PackageManifests.ps1 -Version X.Y.Z -HashManifestPath path\to\UniversalDeviceToolkit_vX.Y.Z_SHA256.txt
   winget validate manifests\s\SSC-STUDIO\UniversalDeviceToolkit\X.Y.Z
   winget install --manifest manifests\s\SSC-STUDIO\UniversalDeviceToolkit\X.Y.Z
   winget uninstall SSC-STUDIO.UniversalDeviceToolkit
   ```
6. Submit the version folder to `microsoft/winget-pkgs` and wait for automated validation.

Use the GitHub Release URL as the winget installer source. Do not use mirror URLs in winget manifests.

### Scoop Submission

The maintainer workflow for Scoop lives under `Packaging/scoop`. The authoritative distribution target is the custom `SSC-STUDIO/scoop-bucket` repository.

Before submitting a new version:

1. Publish a stable GitHub Release with the final installer and checksum file.
2. Do not draft or submit a Scoop manifest update until the installer URL and SHA256 are final.
3. Update the `lenovolegiontoolkit` manifest in `SSC-STUDIO/scoop-bucket` with the new version, URL, and hash. Do not rename the manifest during the Universal Device Toolkit transition.
4. Validate the repo copy against the release checksum manifest:
   ```powershell
   .\Packaging\Test-PackageManifests.ps1 -Version X.Y.Z -HashManifestPath path\to\UniversalDeviceToolkit_vX.Y.Z_SHA256.txt
   ```
5. Validate on a clean machine:
   ```powershell
   scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket
   scoop install ssc-studio/lenovolegiontoolkit
   scoop update ssc-studio/lenovolegiontoolkit
   scoop uninstall lenovolegiontoolkit
   ```
6. Push the manifest update to `SSC-STUDIO/scoop-bucket`.

### High-Traffic Release Readiness

When promoting a release on Chinese social platforms or after winget acceptance:

- Pin the current GitHub Release URL, winget command, and SHA256 file in all announcement posts.
- Link to the active `SSC-STUDIO/UniversalDeviceToolkit` repository in all promotion content.
- Use Universal Device Toolkit as the public product name, and mention that former Lenovo Legion Toolkit users can upgrade directly.
- Keep mirrors optional and checksum-backed; GitHub Releases and winget remain the authoritative download channels.
- Mention that winget and Scoop commands temporarily retain the old UniversalDeviceToolkit identifiers for compatibility.
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
       --title "Universal Device Toolkit vA.B.C (Hotfix)" \
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
dotnet list UniversalDeviceToolkit.sln package --outdated --include-transitive --no-restore
dotnet list UniversalDeviceToolkit.sln package --vulnerable --include-transitive --no-restore
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

# Restore packages (same flags as CI; fails if lock files are out of date)
dotnet restore UniversalDeviceToolkit.sln --locked-mode

# After intentionally updating Directory.Packages.props / PackageReferences,
# regenerate locks without --locked-mode, then commit the updated packages.lock.json files:
#   dotnet restore UniversalDeviceToolkit.sln
#   git add '**/packages.lock.json'

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
