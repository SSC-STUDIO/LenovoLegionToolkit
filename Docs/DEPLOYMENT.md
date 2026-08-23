# Universal Device Toolkit Deployment Guide

## Overview

This document describes the build, test, and deployment processes for Universal Device Toolkit (UDT, formerly Lenovo Legion Toolkit). It covers development workflows, CI/CD pipelines, and release procedures.

Public release copy should use Universal Device Toolkit. Repository paths, assembly names, installer asset names, winget identifiers, and Scoop manifest names may still contain `UniversalDeviceToolkit` during the compatibility transition so existing Lenovo Legion Toolkit users can upgrade directly.

## Prerequisites

### Development Environment

- **Operating System**: Windows 10 (1809+) or Windows 11 for the supported product build. macOS or Linux can build portable libraries, the CrossPlatform CLI, and an experimental Electron shell; they cannot build the full Windows solution or produce official releases.
- **.NET SDK**: .NET 10.0 or later (all platforms)
- **Runtime**: .NET 10.0 Desktop Runtime (x64, Windows)
- **Node.js**: 20+ (Electron client)
- **IDE**: Visual Studio 2022, VS Code, or Rider
- **Git**: Latest version with Git LFS support

### Required Tools

```bash
# Install .NET 10.0 SDK
winget install Microsoft.DotNet.SDK.10

# Verify installation
dotnet --list-sdks
dotnet --info

# Node.js 20+ (Electron client) — install from https://nodejs.org or:
winget install OpenJS.NodeJS.LTS
node --version
```

## Build Configuration

### Solution Structure

```
UniversalDeviceToolkit.sln
├── UniversalDeviceToolkit.Electron/       # Electron client (UI shell; React + electron-vite)
├── UniversalDeviceToolkit.Host/           # Headless .NET backend (JSON-RPC over stdio)
├── UniversalDeviceToolkit.Lib/            # Core library (assembly: UniversalDeviceToolkit.Lib)
├── UniversalDeviceToolkit.Lib.Automation/ # Automation features
├── UniversalDeviceToolkit.Lib.Macro/      # Macro system
├── UniversalDeviceToolkit.Lib.Plugins/    # Plugin host
├── UniversalDeviceToolkit.Lib.Abstractions/ / Lib.Shared # Portable, net10.0
├── UniversalDeviceToolkit.CrossPlatform/  # Cross-platform diagnostics CLI (net10.0)
├── UniversalDeviceToolkit.CLI/            # Windows IPC CLI (udt-cli.exe)
├── UniversalDeviceToolkit.CLI.Lib/        # CLI core
├── UniversalDeviceToolkit.Tests.Contracts/ # Guard + Security
├── UniversalDeviceToolkit.Tests/          # Parallel unit tests
├── UniversalDeviceToolkit.Tests.Stateful/ # Collection-bound tests
├── UniversalDeviceToolkit.Fast.Tests/     # Isolation-free unit tests
├── UniversalDeviceToolkit.CrossPlatform.Tests/ # Cross-platform tests
└── UniversalDeviceToolkit.SpectrumTester/ # Hardware testing
```

### Build Properties

Key configurations in `Directory.Build.props`:

```xml
<UDTTargetFramework>net10.0-windows10.0.26100.0</UDTTargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
```

Windows shipping projects (including `UniversalDeviceToolkit.Host` and
`UniversalDeviceToolkit.Lib`) default to `Platforms=x64` and RID `win-x64`.
Portable `net10.0` projects (CrossPlatform, Lib.Shared, Lib.Abstractions,
Platform.Linux, Platform.MacOS, Platform.Windows.Core,
Tests.Infrastructure) opt out via `DisableUdtForceX64` so they build on any
platform.

## Build Commands

### Local Development Build

```bash
# Restore (CI-aligned; lock files committed per project)
dotnet restore UniversalDeviceToolkit.sln --locked-mode

# Debug build (development) — serial (-m:1) to avoid VBCSCompiler lock conflicts
dotnet build UniversalDeviceToolkit.sln --configuration Debug -m:1

# Release build (production)
dotnet build UniversalDeviceToolkit.sln --configuration Release -m:1

# Clean rebuild
dotnet clean UniversalDeviceToolkit.sln
dotnet build UniversalDeviceToolkit.sln --configuration Release --no-incremental
```

> [!NOTE]
> The solution contains Windows-only projects (Windows TFM `net10.0-windows10.0.26100.0`
> with forced win-x64), so a full `UniversalDeviceToolkit.sln` build only runs on
> Windows. On macOS/Linux, build the portable projects only (see
> [Cross-platform builds](#cross-platform-builds) below).

### Specific Project Build

```bash
# Build the .NET Host backend only (headless JSON-RPC server spawned by Electron)
dotnet build UniversalDeviceToolkit.Host/UniversalDeviceToolkit.Host.csproj \
    --configuration Release

# Build and run Host tests (see Docs/TEST_DIAGNOSTICS.md)
dotnet test UniversalDeviceToolkit.Tests.Contracts/UniversalDeviceToolkit.Tests.Contracts.csproj
dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj

# Cross-platform diagnostics CLI (builds on Windows, macOS, and Linux)
dotnet build UniversalDeviceToolkit.CrossPlatform/UniversalDeviceToolkit.CrossPlatform.csproj \
    --configuration Release
```

### Release Build with Publish

#### Host backend (.NET) — per-platform RID

The Electron client spawns the Host as a child process, so the Host must be
published **self-contained** for the target platform. Official releases
(`Release.yml`) publish only the Windows win-x64 Host. The Windows installer
embeds that output from `UniversalDeviceToolkit.Host/publish/win-x64`
via `extraResources` in `UniversalDeviceToolkit.Electron/electron-builder.yml`.

```bash
# Windows (x64) — shipping path embedded into the NSIS installer
dotnet publish UniversalDeviceToolkit.Host/UniversalDeviceToolkit.Host.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output UniversalDeviceToolkit.Host/publish/win-x64

# Remove shipping PDB files, non-x64 Windows natives, and satellite cultures
# outside UdtSatelliteResourceLanguages. RuntimeIdentifier is optional and
# defaults to win-x64 for backwards-compatible Windows release invocations.
./Scripts/Prune-ShippingFootprint.ps1 \
    -PayloadPath UniversalDeviceToolkit.Host/publish/win-x64 \
    -RuntimeIdentifier win-x64 \
    -AllowedCultures 'ar;bg;cs;de;el;en;es;fr;hu;it;ja;lv;nl-nl;pl;pt;pt-br;ro;ru;sk;tr;uk;uz-latn-uz;vi;zh-hans;zh-hant'
```

**Experimental portable Host** (not a release artifact). Requires
`UDTWindows=false` / `UDT_PLATFORM=linux|macos`. Do not publish the default
Windows TFM for `osx-*` / `linux-x64`. `Release.yml` does not run these
commands and does not attach macOS/Linux Host payloads.

```bash
# Linux x64
UDT_PLATFORM=linux ./build.sh host

# macOS (auto-detects osx-arm64 or osx-x64)
UDT_PLATFORM=macos ./build.sh host

# Equivalent:
dotnet publish UniversalDeviceToolkit.Host/UniversalDeviceToolkit.Host.csproj \
    --configuration Release \
    --runtime linux-x64 \
    -p:UDTWindows=false \
    --self-contained true \
    --output UniversalDeviceToolkit.Host/publish/linux-x64
```

> [!NOTE]
> The shipping Host targets the Windows TFM `net10.0-windows10.0.26100.0` and
> depends on Windows-only libraries (WMI, registry, vendor drivers). The
> win-x64 RID is the only official release configuration. The portable
> `net10.0` Host stubs most Windows-only RPC as `-32099`. Hardware control
> remains Windows-only. There is no official macOS/Linux Electron product
> until those pipelines exist.

#### Electron client (UI)

```bash
cd UniversalDeviceToolkit.Electron
npm ci            # first time only (uses package-lock.json)

# Dev / validation
npm run dev       # dev server + Electron window (hot reload)
npm run typecheck # TS type check (web + main/preload)
npm run lint      # ESLint
npm run build     # electron-vite build (outputs out/)

# Package (electron-builder; runs `npm run build` first)
npm run dist:win    # Windows NSIS installer (x64); official release path
npm run dist:mac    # experimental local macOS DMG (arm64 + x64)
npm run dist:linux  # experimental local Linux AppImage/DEB (x64)
npm run dist        # current host platform default
```

`npm run dist:mac` and `npm run dist:linux` are experimental local scripts.
They expect a portable Host already published under
`UniversalDeviceToolkit.Host/publish/osx-*` or `linux-x64`. `Release.yml`
does not run them and does not attach DMG/AppImage/DEB assets to GitHub
Releases.

The packaging targets are defined in `UniversalDeviceToolkit.Electron/electron-builder.yml`:

| Platform | Target(s) | Notes |
|---|---|---|
| Windows (supported) | `nsis` (Full, x64) and `nsis-web` (Online stub, x64) | Official release path. Full is a complete offline installer. Online is a small web installer (asserted <= 15MB) that downloads the `.nsis.7z` payload from the GitHub Release. Both embed the self-contained Host via `extraResources`. 23 installer languages on Full; Online stays English to keep the stub small. |
| macOS (experimental) | `dmg` (arm64 + x64) | Local packaging only. Category `public.app-category.utilities`; **unsigned/notarized only if credentials are configured** (see below). Not published by `Release.yml`. |
| Linux (experimental) | `AppImage` and `deb` (x64) | Local packaging only. Category `Utility`. Not published by `Release.yml`. |

**Artifact naming** (`artifactName` / electron-builder defaults):

| Platform | Artifact | Official GitHub Release |
|---|---|---|
| Windows Full | `UniversalDeviceToolkitSetup-<version>.exe` (offline NSIS) | Yes |
| Windows Online | `UniversalDeviceToolkitOnlineSetup-<version>.exe` (nsis-web stub, <= 15MB) plus `*.nsis.7z` payload | Yes |
| macOS (experimental) | `UniversalDeviceToolkit-<version>-mac-arm64.dmg` / `-mac-x64.dmg` | No |
| Linux (experimental) | `UniversalDeviceToolkit-<version>-linux-x64.AppImage` / `.deb` | No |

#### Release footprint gate

Release builds use Node 22 and `npm ci` before Electron packaging. Renderer-only
libraries are development dependencies because Vite bundles them into `out/`;
the Electron main and preload processes use only Electron, Node built-ins, and
local modules. This prevents electron-builder from copying a production
`node_modules` tree into `app.asar`.

`afterPack` runs `scripts/package-footprint.mjs` for every target and writes
`dist/footprint/<rid>.json`. It rejects `node_modules` in `app.asar`, Host PDB
files, an incorrect Chromium locale set, and any exceeded unpacked budget. The
main application keeps these 24 Chromium locales: `en-US`, `zh-CN`, `zh-TW`,
`ja`, `de`, `fr`, `es`, `it`, `pt-BR`, `pt-PT`, `ru`, `uk`, `pl`, `cs`, `sk`,
`hu`, `ro`, `bg`, `tr`, `el`, `ar`, `lv`, `nl`, and `vi`. Uzbek application
content remains shipped and Chromium's built-in pages fall back to `en-US`.
The separate custom Windows installer shell keeps only `en-US`; it embeds the
fully audited application payload unchanged.

Windows release gates apply to official `Release.yml` artifacts. linux-x64 /
osx-* budgets are used by experimental `package-footprint.yml` CI only; they
are not a promise that macOS/Linux products ship.

| Content | Hard limit |
|---|---:|
| `app.asar` | 15 MiB |
| Chromium locales | 20 MiB and the exact 24-locale set above |
| Host (`win-x64` shipping / `linux-x64` experimental / `osx-*` experimental) | 180 / 92 / 100 MiB |
| Unpacked application (`win-x64` shipping / `linux-x64` experimental / `osx-*` experimental) | 540 / 450 / 500 MiB |
| Full ZIP, Setup (shipping); DMG, AppImage, DEB (experimental local) | 190 MiB each |
| Online bootstrap | 15 MiB |

The post-sign Release check repeats the payload and distributable audits. It
does not remove Electron licenses, GPU/SwiftShader files, or change .NET
deployment mode.

Windows x64 unsigned local measurement (2026-08-15): `app.asar` is **10.08
MiB**, Chromium locales **18.06 MiB**, Full/Online unpacked payloads **490.62
MiB / 481.54 MiB**, Full Setup **181.99 MiB**, Full ZIP **186.99 MiB**, Online
Setup **0.61 MiB**, and Online ZIP **184.39 MiB**. The pre-change Full payload,
ZIP, Setup, `app.asar`, and Chromium locales measured 763.27, 245.76, 214.24,
252.93, and 46.65 MiB respectively.

**macOS signing & notarization (experimental local packaging only):**
`electron-builder.yml` defines **no** `mac.identity` / `notarize`
configuration, so `npm run dist:mac` produces **unsigned** DMGs unless you
provide signing credentials via `CSC_LINK`/`CSC_KEY_PASSWORD` (or
`mac.identity`) and add a notarization step (`afterSign` hook with
`APPLE_ID`/`APPLE_APP_SPECIFIC_PASSWORD`/`APPLE_TEAM_ID` or
`APPLE_API_KEY`/`APPLE_API_ISSUER`). Unsigned builds may run locally
(right-click → Open) but will trigger Gatekeeper warnings and are not
official release artifacts. `Release.yml` does not produce or sign macOS
packages. Windows installer signing uses Azure Trusted Signing in
`Release.yml` (see [Security Considerations](#security-considerations));
local Windows builds are unsigned.

**Linux packaging (experimental local packaging only):**
`electron-builder.yml` already lists `AppImage` and `deb` (x64) as local
targets. Neither is published by `Release.yml`. Adding `.rpm` or `.snap`
would still be experimental local packaging, not an official product.

### Known Platform Differences

The supported product is Windows. The Electron UI shell contains
platform-specific chrome for macOS and Linux; those rows describe existing
code, not a shipped product (see
[ARCHITECTURE.md](ARCHITECTURE.md#platform-notes)):

| Surface | Windows | macOS | Linux |
|---|---|---|---|
| Title bar | Frameless custom title bar (right-aligned window buttons, Mica background) | Native title bar with traffic lights (hiddenInset) + vibrancy | Frameless custom title bar (right-aligned window buttons) |
| Menu bar | Auto-hidden | Native system menu bar (App/File/Edit/View/Window/Help roles) | Auto-hidden |
| Tray | Tray icon + custom flyout (nav, quick actions, open/close) | Tray icon + custom flyout | Tray icon + custom flyout |
| OSD | Transparent always-on-top overlay (sensor data from Host) | Same window; no meaningful sensor data in basic mode | Same window; no meaningful sensor data in basic mode |
| System power actions (restart/shutdown/sleep) | `shutdown.exe` | Unavailable | Unavailable |
| Windows power plans | `powercfg` | Unavailable | Unavailable |
| Window lifecycle | Quit on last window closed | Stays running (macOS convention); app menu stays available | Quit on last window closed |

### Cross-platform builds

On macOS/Linux the full `UniversalDeviceToolkit.sln` cannot build (it contains
Windows-TFM projects). Portable libraries and the CrossPlatform diagnostics
CLI are the supported non-Windows build surface. The Electron shell can be
started for experimental UI work; it is not an official product release:

```bash
# Portable .NET libraries + CrossPlatform CLI (macOS/Linux/Windows)
./build.sh Release            # auto-detects linux-x64 / osx-arm64 / osx-x64 / win-x64
./build.sh Release linux-x64  # explicit runtime

# Cross-platform test suite (runs on Windows, Ubuntu, and macOS CI)
dotnet test UniversalDeviceToolkit.CrossPlatform.Tests/UniversalDeviceToolkit.CrossPlatform.Tests.csproj \
    --configuration Release

# Experimental Electron shell on macOS/Linux (not an official product)
cd UniversalDeviceToolkit.Electron
npm ci
npm run dev
```

See `.github/workflows/linux.yml` and `CrossPlatformCli.yml` for CI coverage
of the portable libraries and diagnostics CLI. Those workflows do not
publish a macOS/Linux Electron product. `package-footprint.yml` may exercise
experimental local packaging; it is not `Release.yml`.

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

### README Screenshot Refresh

Regenerate repository README screenshots on an interactive Windows desktop session after major UI changes.

**Target resolution**

| Setting | Value |
|---------|-------|
| Main window (logical) | 1300×850 px |
| Capture method | Interactive desktop capture / smoke tooling when available |
| Expected pixel size | Logical size × Windows display scale (1300×850 at 100% DPI; ~1625×1063 at 125% DPI) |
| README display width | 700 px (`width="700"` in README markdown) |

<a id="readme-screenshots"></a>

All README screenshots must use the same window size and capture method so aspect ratio and UI density stay consistent.
Brand binaries (icons/logos) live only under repo-root [`Assets/`](../Assets/README.md).

Capture the Electron main window at **1300×850** logical size on an interactive Windows desktop session (Dark theme), then replace:

- `Assets/Screenshot_main.png` — English UI
- `Assets/Screenshot_zh-hans.png` — Simplified Chinese UI

Document the refresh in `CHANGELOG.md` when user-visible UI changes ship.
Last refreshed: 2026-08-23. Target logical window 1300×850 (pixel size scales with display DPI).

The README trailer lives at `Assets/UDT_Promo.mp4` (poster `Assets/UDT_Promo_poster.jpg`). Do not replace it with an unrelated clip.

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
        run: ./Scripts/Build-ElectronInstaller.ps1 -Version $env:VERSION
      - name: Create Release
        uses: softprops/action-gh-release@v3
        with:
          files: |
            installer/*.exe
            publish/*.zip
```

## Installer Creation

### Electron NSIS installer (Inno Setup and WPF installer retired)

The project ships an Electron (electron-builder) NSIS installer. Inno Setup
(`MakeInstaller.iss`), `InnoDependencies`, the WPF installer (`Tools/Installer`)
and `Scripts/Build-InstallerAssets.ps1` are retired. The installer is produced
by `Scripts/Build-ElectronInstaller.ps1` (also wired into `Make.bat` and the
Release workflow):

```bash
# Build the NSIS installer (requires the self-contained .NET host published to
# UniversalDeviceToolkit.Host/publish/win-x64, which the Release workflow does)
./Scripts/Build-ElectronInstaller.ps1 -Version X.Y.Z

# Output location
BuildInstaller/
├── UniversalDeviceToolkitSetup.exe         # Full offline NSIS
├── UniversalDeviceToolkitOnlineSetup.exe   # Online nsis-web stub (<= 15MB)
└── *.nsis.7z                               # payload downloaded by the Online stub
```

The Full installer follows the OS display language (23 languages), requests
administrator elevation (per-machine), allows changing the installation
directory, creates desktop and Start Menu shortcuts, and unregisters Nilesoft
Shell during uninstall to release file locks (mirroring the retired Inno
Setup behavior). The self-contained .NET host is embedded via
`UniversalDeviceToolkit.Electron/electron-builder.yml` `extraResources`.
Packaging uses `compression: maximum`. Host publish output is pruned
(`Scripts/Prune-ShippingFootprint.ps1`); the Online nsis-web payload keeps
English Host satellites only (other languages install from the in-app catalog).
In-app updates follow the install channel written at pack time: Full installs
download `*_Full_Setup.exe`, Online installs download `*_Online_Setup.exe`.

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
1. Run structural audit (`missing/extra/placeholder`) across all `Resource*.resx` and the Electron i18n TS locale modules.
2. Build the Host, the Electron client (`npm run typecheck`), and the full Windows solution.
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

- Release payloads and installers are signed by the Azure Trusted Signing step in `Release.yml`; the workflow verifies every executable and DLL with `Get-AuthenticodeSignature` before publishing. Local builds are not represented as signed releases.
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

1. Verify Node.js/npm and the Electron project dependencies are installed (`npm ci` in `UniversalDeviceToolkit.Electron`)
2. Verify the self-contained .NET host is published to `UniversalDeviceToolkit.Host/publish/win-x64`
3. Check signtool availability
4. Validate version number format
