<img height="128" align="left" src="Assets/Logo.png" alt="Logo">

# Universal Device Toolkit

[![CI Tests](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/actions/workflows/Ci-tests.yml/badge.svg?branch=master)](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/actions/workflows/Ci-tests.yml)
[![GitHub release](https://img.shields.io/github/v/release/SSC-STUDIO/UniversalDeviceToolkit?color=blue)](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest)
[![GitHub stars](https://img.shields.io/github/stars/SSC-STUDIO/UniversalDeviceToolkit?style=social)](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/stargazers)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![GitHub downloads](https://img.shields.io/github/downloads/SSC-STUDIO/UniversalDeviceToolkit/total)](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases)
[![Last commit](https://img.shields.io/github/last-commit/SSC-STUDIO/UniversalDeviceToolkit)](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/commits/master)
[![Contributors welcome](https://img.shields.io/badge/Contributors-welcome-brightgreen.svg)](CONTRIBUTING.md)
<a href="https://hellogithub.com/repository/dd55be3ac0c146208259f17b29d2162f" target="_blank"><img src="https://abroad.hellogithub.com/v1/widgets/recommend.svg?rid=dd55be3ac0c146208259f17b29d2162f&claim_uid=LBbuUlZqTIm1JAP&theme=small" alt="Featured｜HelloGitHub" /></a>

> **Open source · No account · No telemetry**
>
> The lightweight Windows utility for Legion laptops &amp; beyond. Fn+Q, RGB, fan curves, dGPU control — without Vantage bloat. Runs on other PCs too via basic mode.

<div align="center">

**⭐ Star this repo if UDT helps you — it really helps the project grow! ⭐**

[![Download from GitHub Releases](https://img.shields.io/badge/Download-GitHub%20Releases-2ea44f?style=for-the-badge&logo=github&logoColor=white)](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest)
[![Install via Scoop](https://img.shields.io/badge/Install-via%20Scoop-F24E1E?style=for-the-badge&logo=powershell&logoColor=white)](https://github.com/SSC-STUDIO/scoop-bucket)
**Install:** download the installer from [Releases](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest), or use Scoop: `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket && scoop install ssc-studio/universaldevicetoolkit`

> **Goal: 1,000 stars.** If UDT keeps your laptop running lean, a star helps more people find it -- and tells us the plugin model is worth building out.

<a href="https://github.com/SSC-STUDIO/UniversalDeviceToolkit"><img src="Assets/Screenshot_main.png" width="700" alt="UDT Main Interface" /></a>

</div>

### Trailer

<div align="center">

<a href="Assets/UDT_Promo.mp4"><img src="Assets/UDT_Promo_poster.jpg" width="700" alt="Universal Device Toolkit 30-second trailer" /></a>

<sub>A 30-second trailer we cut ourselves — open source, no account, no telemetry. <a href="Assets/UDT_Promo.mp4">Play the MP4</a></sub>

</div>

---

#### Other language versions of this README file:
* [简体中文版简介](README_zh-hans.md)

---

Universal Device Toolkit (UDT, formerly Lenovo Legion Toolkit) is a lightweight Windows device utility that keeps Lenovo hardware control direct on supported machines and still remains useful on other PCs through basic mode. It runs without background services, keeps typical memory around 400MB (Electron UI + .NET Host), contains no telemetry, and is built around plugin extensions for device-specific workflows.

Plugin extensions are a first-class part of this project. You can install, update, configure, open, and remove plugins from the Plugin Extensions page to add CPU, GPU, network, shell, mouse, and other specialized tools without bloating the base application.

UDT is an actively maintained GPL-3.0 project focused on compatibility updates, security hardening, CI/release automation, newer device detection, plugin extensibility, and ongoing Windows support. Existing Lenovo Legion Toolkit users keep their settings, plugins, and data when upgrading; package-manager identities split in 6.x (winget `SSC-STUDIO.UniversalDeviceToolkit`, Scoop `universaldevicetoolkit`), so legacy package IDs do not upgrade in place. The supported product is **Windows-first**: GitHub Releases ship Windows NSIS Full/Online installers with a self-contained win-x64 Host. macOS and Linux are **experimental** (Electron shell, portable Host, and CrossPlatform diagnostics CLI) and have **no official Electron release** until those pipelines exist. Android and mobile companion applications are out of scope and are not supported.

> [!NOTE]
> **What "Universal" means**
> UDT is a Windows utility platform: **full hardware control** targets supported Lenovo Legion, LOQ, and IdeaPad Gaming machines; **basic mode** on other Lenovo models and non-Lenovo PCs still provides plugins, system optimization, themes, updates, and logs while hiding unsupported hardware toggles. The name reflects extensibility and basic-mode coverage, not a promise of Vantage-class control on every PC brand.

### Why choose UDT?

| | UDT | Lenovo Vantage |
|---|:---:|:---:|
| Background service | **None** | Required |
| Telemetry / Lenovo account | **None** | Required |
| Open source (GPL-3.0) | **Yes** | No |
| Plugin extensions | **Yes** | Limited |
| CLI & automation | **Yes** | No |
| Useful on non-Lenovo PCs | **Basic mode** | No |

**Who it's for**

- Legion / LOQ owners who want to drop Vantage but keep Fn+Q, RGB, and dGPU controls
- Anyone on Windows who just wants plugins and general tools (basic mode)
- Tinkerers: `udt-cli.exe` CLI, macros, GPL source you can actually read

Promotion copy (conversational): [PROMOTION_EN.md](Docs/PROMOTION_EN.md) · [COMMUNITY_OUTREACH.md](Docs/COMMUNITY_OUTREACH.md)

<details>
<summary>🎮 Want to see more screenshots?</summary>

| English (Dark) | Chinese Simplified (Dark) |
|---|---|
| ![Main](Assets/Screenshot_main.png) | ![Chinese](Assets/Screenshot_zh-hans.png) |

</details>

### Key Features at a Glance

| Feature | Description |
|:---|:---|
| 🔥 **Power & Performance** | Fn+Q power modes, Custom Mode with fan curves, CPU/GPU power limits |
| 🌈 **RGB & Lighting** | Spectrum per-key RGB, 4-zone RGB, white backlight, boot logo |
| 🎮 **GPU Control** | dGPU/discrete GPU toggle, MUX switch, overclock, deactivate dGPU |
| 🔌 **Battery Care** | Conservation mode, 60%/80% charge thresholds, real-time wear rate & health rating |
| 🧹 **Deep Cache Cleanup** | One-click cleanup for DirectX/Vulkan shader caches, WeChat/QQ caches & dev package manager caches |
| 🎛️ **Quick Tray Control** | Instant flyout with power mode segment pills & battery badge right from the system tray |
| ⚡ **Actions & Macros** | Presets for Mobile Eco, AC High Performance, Game Boost; JSON & clipboard sharing |
| 🖥️ **Sensors** | Real-time CPU/GPU temp, fan speed, clock monitoring |
| 🔧 **Plugin Extensions** | CPU tools, GPU tools, network acceleration, shell integration, mouse |
| 🌍 **78+ Languages** | Full localization with community translations |
| 📦 **Ultra-Lean Footprint** | Deep tray sleeping, zero background services, no telemetry, no account |

### ⚡ Why Electron? Dispelling the "Bloat" Myth

Many users and developers associate web-based desktop frameworks (Electron / Chromium) with bloated memory usage, sluggish startup, and frame drops.

However, **architectural discipline and deep engineering optimizations make all the difference**. UDT uses a decoupled **Modern Electron Frontend + Headless .NET 10 Backend** architecture with stringent performance controls:

#### 1. Clear Separation of Concerns
- **Frontend (Electron + React 19 + TypeScript)**: Focused solely on high-DPI responsive layout, Windows 11 Mica material styling, smooth animations, and dynamic 78+ language switching.
- **Backend (.NET 10 / C# 13 Headless Host)**: All low-level hardware control (WMI/ACPI, kernel driver communication, power policy dispatch, real-time sensor polling, automation pipelines, and isolated plugins) executes in high-performance native .NET and communicates with the UI over lightweight stdio JSON-RPC.

#### 2. Five Tailored Performance Optimizations
- 🍃 **Zero-Memory Tray Sleeping**:
  When minimized or closed to the tray, UDT **completely destroys the main window and Chromium renderer DOM tree** rather than just hiding it. The tray popup itself is auto-unloaded on idle. Idle background footprint stays minimal.
- ⚡ **Sub-400ms Median Ready Latency**:
  Automated UI benchmark testing (`Tools/UiPerformance.Smoke`) confirms that all page transition and ready latencies stay within **≤ 400ms**, earning the highest *Excellent* performance rating.
- 🎯 **Hot-Path Zero Allocation**:
  High-frequency sensor graphs and UI refresh loops reuse static ECharts options and mappings via `useMemo` and static caches, streaming data incrementally to eliminate garbage collection pauses.
- 📦 **Strict Dependency Graph Pruning**:
  Built with `electron-vite` with graph-based code splitting and tree-shaking; 7,000+ Fluent UI icons are strictly imported per glyph.
- 🛡️ **Zero Windows Services & Zero Telemetry**:
  No persistent background Windows services installed, no telemetry data collected or transmitted.

#### 3. Real-World Benchmark Comparison (UDT 6.0 vs Legacy WPF vs Vendor Software)

| Metric | Legacy WPF Client | Lenovo Vantage / Legion Zone | UDT 6.0 (Tuned Electron + .NET 10) | Evaluation |
|---|:---:|:---:|:---:|:---|
| **Background Services** | 0 | 3~5 persistent services | **0 (Zero Services)** | Never burdens background gaming or startup |
| **Cold Startup Ready Latency** | 1.8s ~ 2.5s | 4.0s ~ 8.0s+ | **≤ 400ms (Median)** | **VS Code-level instant launch** |
| **Tray Idle Memory Footprint** | 150MB ~ 250MB | 300MB ~ 600MB+ | **30MB ~ 60MB (DOM Destroyed)** | **Far lower than WPF (~70% reduction)** |
| **Active Peak Working Set** | 180MB ~ 300MB | 500MB ~ 1.2GB | **80MB ~ 120MB (Tuned Heap Limit)** | **Strictly bounded memory ceiling** |
| **UI Scaling & High-DPI** | Blurry text / layout clipping | Poor | **Vector Pixel-Perfect (80%~150%)** | Crisp on OLED, 2K & 4K displays |
| **Dynamic i18n Switching** | Requires app restart | Requires reload | **Instant live hot-switch (78+ locales)** | Seamless multi-language experience |

&nbsp;

---

# Table of Contents
  - [Trailer](#trailer)
  - [Key Features at a Glance](#key-features-at-a-glance)
  - [Why choose UDT?](#why-choose-udt)
  - [Disclaimer](#disclaimer)
  - [Download](#download)
  - [Quick Start](#quick-start)
  - [Compatibility](#compatibility)
  - [Features](#features)
  - [Donate](#donate)
  - [Credits](#credits)
  - [FAQ](#faq)
  - [Arguments](#arguments)
  - [How to collect logs?](#how-to-collect-logs)
  - [Localization](#localization)
  - [Documentation](#documentation)
  - [Contribution](#contribution)

## Disclaimer

**The tool comes with no warranty. Use at your own risk.**

Please be patient and read through this readme carefully - it contains important information.

> [!TIP]
> If you are looking for a Vantage alternative for Linux, check [LenovoLegionLinux](https://github.com/johnfanv2/LenovoLegionLinux) project out.

## Download

Use the current `SSC-STUDIO/UniversalDeviceToolkit` releases for maintained builds. 6.x is a package-manager breaking change: winget moves to `SSC-STUDIO.UniversalDeviceToolkit` and Scoop to `universaldevicetoolkit`; the legacy package IDs are not upgraded in place.

> [!NOTE]
> **Current stable release: v5.0.2.** Application releases use the normal `vX.Y.Z` tags. Official 1.x plugin packages stay in the non-Latest rolling [`plugin-catalog` release](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/tag/plugin-catalog). Source is now the **6.0** train (`Directory.Build.props` `6.0.0`); the first preview tag is `v6.0.0-preview.1`, which reads [`plugin-catalog-preview`](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/tag/plugin-catalog-preview) for 2.x packages. Do not upload 2.x ZIPs to `plugin-catalog`.
> **Note on winget:** the 6.x package id `SSC-STUDIO.UniversalDeviceToolkit` is reserved but not yet published to winget-pkgs, so the winget install command will not resolve until that submission ships, and the legacy Lenovo Legion Toolkit package does not upgrade in place. Use Releases or Scoop in the meantime.

- **GitHub Releases**: Download the latest Full or Online installer from [Releases](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest). **Full** is a complete offline NSIS installer (Electron + self-contained .NET Host). **Online** is a small stub (about 15MB or less) that downloads the same runtime during setup; language and device packs still install from the in-app catalog. Always install the newest version from the latest release page; settings, plugins, and data migrate automatically, while package-manager installs must switch to the new 6.x IDs.

#### Language packs (Full vs Online) & privacy

- **Full** ships many satellite languages offline. **Online** ships English by default and downloads optional language packs only when you choose them (startup language window or Settings).
- Offline / `--safe-start` / no network: the app continues in English — it does **not** phone home for language packs unless you start an install.
- Catalog downloads use HTTPS (or your configured catalog URL). Packages are verified with **SHA-256** before install. No account, no telemetry.
- Corporate proxy: set system proxy as usual, or point `UDT_RESOURCE_CATALOG_URL` at an internal catalog mirror for air-gapped installs. See `Docs/LanguagePacks.md`.
 - ~~**winget** (pending)~~: the 6.x `PackageIdentifier` is `SSC-STUDIO.UniversalDeviceToolkit`; the manifest has not been submitted to microsoft/winget-pkgs yet, so `winget install` fails until it ships. The legacy Lenovo Legion Toolkit package does not upgrade in place. Use Releases or Scoop today; this bullet becomes a one-line install again once the winget-pkgs PR merges.
- **Scoop**: `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket && scoop install ssc-studio/universaldevicetoolkit`. The 6.x manifest consumes the Full portable ZIP (`innosetup: false`); the legacy `lenovolegiontoolkit` package stays on 5.x — uninstall it before installing `universaldevicetoolkit`.
- **Checksum**: Each GitHub release includes a `SHA256.txt` file. Verify downloaded installers before sharing mirrors.

#### Naming and upgrade compatibility

During the rename from Lenovo Legion Toolkit, runtime identifiers intentionally stay compatible so existing settings and plugins carry over. Package-manager identities are the exception — 6.x introduces new IDs and no in-place upgrade:

| What you see | Legacy identifier | Why it remains |
|---|---|---|
| Product name in UI / Releases | Universal Device Toolkit (UDT) | Current public branding |
| winget ID (pending) / Scoop ID | `SSC-STUDIO.UniversalDeviceToolkit` (new 6.x ID, not yet in winget-pkgs) / `universaldevicetoolkit` (new 6.x Scoop name) | 6.x package break; legacy `SSC-STUDIO.LenovoLegionToolkit` / `lenovolegiontoolkit` do not upgrade in place |
| CLI executable | `udt-cli.exe` | Scripts and automation compatibility |
| Data directory | `%LOCALAPPDATA%\UniversalDeviceToolkit` | Settings/plugins migrate automatically |
| Action env vars | `LLT_*` + `UDT_*` (dual-write) | Existing user scripts; UDT aliases available |
| Plugin/core assemblies | `UniversalDeviceToolkit.Lib*` (primary) | Phase 3 ABI; legacy plugin prefixes still load |

Repository folders use `UniversalDeviceToolkit.*`. New users install UDT from Releases; legacy names above are compatibility aliases, not a separate product.

#### Next steps

UDT works best when it's running in the background, so go to Settings and enable _Autorun_ and _Minimize on close_. Next thing is to either disable Vantage and Hotkeys or just uninstall them. After that UDT will always run on startup and will take over all functions that were handled by Vantage and Hotkeys.

> [!WARNING]
> If you close UDT completely some functions will not work, like synchronizing Windows Power Modes or Windows Power Plans with current Power Mode, Macros or Actions. This is due to the fact that UDT does not run any background services and won't be able to respond to changes.

## Quick Start

1. **Install UDT** - Download from [Releases](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest) or upgrade directly from an existing Lenovo Legion Toolkit installation
2. **Configure Settings** - Enable "Autorun" and "Minimize on close" in Settings
3. **Disable Conflicts** - Uninstall or disable Lenovo Vantage and Hotkeys
4. **Explore Features** - Supported Lenovo hardware controls, plugin extensions, system optimization, language packs, themes, and logs

> [!TIP]
> First time? Check out the [User Guide](Docs/ARCHITECTURE.md#quick-start) for detailed walkthroughs.

#### Required drivers

If you installed UDT on a clean Windows install and want Lenovo hardware controls, make sure to have necessary Lenovo drivers installed. If drivers are missing, hardware-specific options might not be available. Especially make sure that these two are installed on supported Lenovo systems:
1. Lenovo Energy Management
2. Lenovo Vantage Gaming Feature Driver

#### Problems with .NET?

If for whatever reason the UDT installer did not setup .NET properly:
1. Go to https://dotnet.microsoft.com/en-us/download/dotnet/10.0
2. Find section ".NET Desktop Runtime"
3. Download x64 Windows installer
4. Run the installer

> [!NOTE]
> If you installed UDT from Scoop, the required .NET runtime should have been installed automatically as a dependency. If anything fails, use `scoop update` to update all packages and try to reinstall UDT with `--force` argument.

After following these steps, you can open Terminal and type: `dotnet --info`. In the output look for section `.NET runtimes installed`, in this section you should see entries for the installed runtime such as `Microsoft.NETCore.App 10.x.x` and `Microsoft.WindowsDesktop.App 10.x.x` under `C:\Program Files\dotnet\shared`.

## Compatibility

Universal Device Toolkit now uses catalog-backed device support. Supported Lenovo gaming and creator machines get hardware controls; unsupported Lenovo models and non-Lenovo PCs enter basic mode so unavailable hardware entries stay hidden while plugins, system optimization, language, theme, update, log, and safety workflows remain available.

Hardware-control families:
- Legion 5, Legion Slim 5, Legion Pro 5
- Legion 7, Legion Pro 7, Legion 9
- Legion Go
- LOQ
- IdeaPad Gaming, ThinkBook, YOGA, and selected legacy Lenovo gaming families
- Chinese variants such as R7000/R7000P/R9000/Y7000/Y7000P/Y9000, including Y7000P 2020H

Basic-mode families:
- Lenovo ThinkPad, ThinkCentre, ThinkStation, IdeaCentre, Legion desktop, XiaoXin, V series, Slim, and other unmatched Lenovo models
- Motorola, ASUS, Dell, HP, Acer, MSI, Microsoft Surface, GIGABYTE/AORUS, Razer, Samsung Galaxy Book, Apple Mac, HUAWEI MateBook, Xiaomi/RedmiBook, realme Book, Infinix INBook, HONOR MagicBook, LG gram, Framework, Panasonic TOUGHBOOK, Dynabook/Toshiba, Fujitsu, VAIO, Gateway, CHUWI, TECLAST, Jumper, MEDION/ERAZER, XMG/SCHENKER, System76, Star Labs, Slimbook, Hasee, THUNDEROBOT, MACHENIKE, COLORFUL, MAIBENBEN, MECHREVO, Clevo/Tongfang barebones, handheld PCs such as Steam Deck/GPD/AYANEO/ONEXPLAYER, mini PCs such as MINISFORUM/Beelink/GEEKOM/ZOTAC, and generic PCs

Hardware-control matching is driven by `UniversalDeviceToolkit.Lib/DeviceSupport/LenovoDeviceSupportProvider.cs` and online data-only device packs. Generations 6 (MY2021), 7 (MY2022), 8 (MY2023), 9 (MY2024) and newer are the primary Lenovo hardware-control target. Some features may also work on selected 5th generation (MY2020) devices. Basic-mode vendor matching normalizes common BIOS/DMI formatting differences, so punctuation, casing, spacing, diacritics, and company suffix variants do not usually block a match.

If UDT starts in basic mode, it is doing that intentionally to avoid showing unsupported hardware controls. You can still use plugins and general system tools, and you can contribute logs or device-pack data for broader support.

### macOS and Linux (experimental)

UDT is a **Windows-first** product. Official GitHub Releases publish Windows
NSIS Full/Online installers with a self-contained win-x64 Host
(`Release.yml`). There is **no official macOS or Linux Electron release**
until those pipelines exist.

What exists today for macOS/Linux is experimental developer surface, not a
shipped product:

- `UniversalDeviceToolkit.CrossPlatform` diagnostics CLI (CI-tested)
- Portable `net10.0` libraries and a portable Host built with
  `UDTWindows=false` / `UDT_PLATFORM=linux|macos` (`build.sh host`)
- Electron shell code that adapts title bar, menu, tray, and OSD chrome

The portable Host answers most Windows-only RPC names as `-32099`
(`Not supported on this platform.`). Official plugins target Windows TFMs.
Do not treat local `npm run dist:mac` / `npm run dist:linux` output as
official release artifacts.

| Capability | Windows (supported) | macOS / Linux (experimental) |
|---|---|---|
| Lenovo hardware control (Fn+Q, RGB, fan curves, dGPU, battery care) | Yes | No |
| Official plugins and Windows system optimization | Yes | No (portable Host stubs those domains; official plugins are Windows TFMs) |
| Themes, in-app updates, logs UI | Yes | Shell may render; no official update channel or release assets |
| Title bar / menu / tray / OSD chrome | Yes | Shell code exists; not a shipped product |
| Restart / shutdown / sleep actions | Yes | No |
| Windows power plan switching | Yes | No |

> [!NOTE]
> Restart/shutdown/sleep and Windows power-plan switching use Windows-only
> tools (`shutdown.exe`, `powercfg`) in the Electron main process.
> The OSD window itself is Electron chrome; sensor values come from the Host
> and are only meaningful on Windows.

**Build the Electron client (Windows product path)**

```bash
cd UniversalDeviceToolkit.Electron
npm ci              # first time only (uses package-lock.json)
npm run dev         # dev server + Electron window (hot reload)
npm run dist:win    # Windows NSIS installer (x64); used by official releases
```

`npm run dist:mac` and `npm run dist:linux` are **experimental local
packaging scripts**. They expect a portable Host already published under
`UniversalDeviceToolkit.Host/publish/osx-*` or `linux-x64`. `Release.yml`
does not run them and does not attach DMG/AppImage/DEB assets.

**Experimental portable Host** (not a release artifact):

```bash
# Linux x64
UDT_PLATFORM=linux ./build.sh host

# macOS (auto-detects osx-arm64 or osx-x64)
UDT_PLATFORM=macos ./build.sh host
```

Or the equivalent `dotnet publish` with `-p:UDTWindows=false`. Publishing
the default Windows TFM (`net10.0-windows10.0.26100.0`) for `osx-*` /
`linux-x64` is not a supported product path. See
[DEPLOYMENT.md](Docs/DEPLOYMENT.md).

The repository also includes `UniversalDeviceToolkit.CrossPlatform`, a plain
`net10.0` CLI entry point for local macOS/Linux and Windows diagnostics (see
[DEPLOYMENT.md](Docs/DEPLOYMENT.md) for build details):

<details>
<summary>Cross-platform CLI commands (developers)</summary>

```powershell
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- status
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- json
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- hardware
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- telemetry
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- power
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- profile
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- plugins
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- controls
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- elevate set cpu-governor performance
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- support
dotnet run --project UniversalDeviceToolkit.CrossPlatform -- doctor
```

On macOS/Linux this CLI reports platform/runtime information, reads basic hardware identity from Linux DMI (`/sys/class/dmi/id`) or macOS `sysctl`/`system_profiler`, reads safe CPU/memory/frequency/temperature/fan telemetry from Linux procfs/sysfs or macOS `sysctl`, reads battery and external power state from Linux `power_supply` or macOS `pmset`, inspects platform power profiles through Linux `powerprofilesctl` or macOS `pmset`, scans plugin manifests without loading Windows assemblies, matches common vendors to safe basic device packs, and treats the machine as safe basic mode. The `doctor` command aggregates readiness checks into a pass/warn/fail report. Vendor-specific control backends and cross-platform plugin loading are future 5.x expansion points.

</details>

### Lenovo's software

Overall the recommendation is to disable or uninstall Vantage, Hotkeys and Legion Zone while using UDT. There are some functions that cause conflicts or may not work properly when UDT is working along side other Lenovo apps.

> [!TIP]
> Using the disable option in UDT is often the easiest option.

### Other remarks

UDT currently does not support installation for multiple users, so if you need to have multiple users on you laptop you might encounter issues. Same goes for accounts without Administrator rights - UDT needs an account with Administrator rights. If you install UDT on an account without such rights, UDT will not work properly. Multi-user support is on the long-term roadmap.

> [!NOTE]
> **Warranty lookup (China region)**: Warranty status for Chinese Legion models was removed in recent versions due to unreliable upstream APIs. Existing cached data may still display until refreshed or cleared.

## Features

The app allows to:

- **Deep System Junk & Shader Cache Cleaning**: Scan and clean DirectX/Vulkan shader caches, WeChat/QQ temp caches, package manager caches (npm/yarn/pnpm/cargo/pip), and Windows thumbnail/temp files.
- **Battery Health & Wear Level Real-Time Monitoring**: Accurately calculate design capacity, full charge capacity, cycle count, and wear percentage with health ratings.
- **Automation Preset Templates & Cross-Device Sharing**: Built-in presets for battery conservation, gaming performance, and quiet work; full JSON export/import and clipboard sharing.
- **Enhanced Tray Quick Controls Flyout**: Capsule segmented power mode switcher, battery percentage badge, and instant hardware status.
- **Hardware Modes & Fan Curves**: Change performance modes, battery conservation/rapid charge, and customize fan curves on 2022+ models.
- **Full Keyboard & Lighting Control**: Spectrum per-key RGB, 4-zone RGB, white backlight, logo light, and rear port lighting.
- **Dedicated GPU Hybrid Mode & Kill Switch**: Switch between Hybrid/dGPU-only modes, track active GPU processes, and forcefully sleep inactive NVIDIA dGPUs.
- **Automation Pipeline & Macro Sequences**: Automate triggers based on AC power, active applications, external displays, Wi-Fi, time of day, and custom hotkeys.
- **Zero Background Services**: Disable Lenovo Vantage / Legion Zone services cleanly without uninstallation.

### Deep System Junk & Shader Cache Cleaning

Located under **Windows Optimization → System & Storage**:

- 🎮 **DirectX & Vulkan Shader Caches**: Safely purge DirectX D3DSCache and NV_Cache to resolve corrupted shader stutters or reclaim multiple gigabytes of disk space.
- 💬 **Instant Messaging Caches (WeChat / QQ)**: One-click clean temporary media, avatar caches, and discarded file transfers.
- 💻 **Package Manager Caches**: Clean global cache bloat for `npm`, `pnpm`, `yarn`, `cargo`, and `pip` development environments.
- 🧹 **System Thumbnails & Temp Files**: Clear Windows thumbnail database caches and orphaned temporary directories.

### Battery Health & Wear Level Monitoring

Located under **Dashboard → Battery Details**:

- **Real-Time Health Rating**: Dynamically evaluates current battery capacity against original factory design capacity (Excellent ≥ 90%, Good ≥ 80%, Service Recommended < 80%).
- **Multi-Metric Telemetry**: Real-time charge/discharge rate (mW/W), ambient temperature, cycle counts, manufacture date, and first-use timestamp.

### Automation Preset Templates & Macro Sharing

Located under **Automation** and **Keyboard Macro**:

- **Built-in Standard Presets**: Instantly apply tested automation workflows (e.g. "Auto Switch to Quiet & 60Hz on Battery", "High Refresh Rate & Beast Mode on AC").
- **Cross-Device Config Sharing**: Export/import automation pipelines and macro sequences as JSON, or share directly via clipboard copy/paste.

### Enhanced Tray Quick Controls

- Click the taskbar tray icon for an instant, responsive flyout:
  - **Segmented Capsule Controls**: Instant 1-click switching between Quiet, Balance, and Beast modes.
  - **Battery Badge**: Live battery percentage, conservation mode status, and charging rate.
  - **Zero-Resident Idle**: Unloads completely when closed, leaving zero persistent memory footprint.

### Plugin Extensions

The Plugin Extensions page is the primary way to grow UDT beyond the built-in runtime. It lets you browse available plugins, install or update them directly from the online repository, open plugin pages, configure supported plugins, and remove them cleanly when they are no longer needed.

Plugins are used to deliver tools and workflows that used to live in separate sections. This keeps the main app focused while still allowing CPU, GPU, networking, shell integration, mouse customization, and other add-ons to evolve independently.

### Custom Mode

Custom Mode is available on all devices that support it. You can find it in the Power Mode dropdown as it basically is 4th power mode and it allows for adjusting power limits and fans. Custom Mode can't be accessed with Fn+Q shortcut. Not all features of Custom Mode are supported by all devices.

If you have one of the following BIOSes:
* G9CN (24 or higher)
* GKCN (46 or higher)
* H1CN (39 or higher)
* HACN (31 or higher)
* HHCN (20 or higher)

Make sure to update it to at least minimum version mentioned above for Custom Mode to function properly.

### RGB and lighting

Both Spectrum per-key RGB and 4-zone RGB backlight is supported. Vantage and it's services need to be disabled to avoid conflicts when communicating with hardware. If you use other RGB apps that might conflict with UDT, check [FAQ](#faq) for solutions.

Other lighting features like both 1 and 3 level white keyboard backlight, panel logo and rear ports backlight are also supported, however there are some constraints:

* GKCN54WW and lower - some lighting features are disabled due to a bug in these BIOS versions causing BSOD
* some (mostly Gen 6) laptops models might not show all options or show options that aren't there - this is due misconfigured BIOS that doesn't report availability of these features

Lighting that required Corsair iCue is not supported by UDT.

> [!IMPORTANT]
> Riot Vanguard DRM (used in Valorant for example) is known to cause issues with RGB controls. If you don't see RGB settings and have it installed, make sure it doesn't run on startup or uninstall it._

### Hybrid Mode and GPU Working Modes

> [!NOTE]
> Hybrid Mode/GPU Working Mode options _are not_ Advanced Optimus and work separately from it.

There are two main way you can use your dGPU:

1. Hybrid mode on - internal laptop display is connected to integrated GPU, discrete GPU will work when needed and power off when not in use, giving better battery life
2. Hybrid mode off (aka dGPU) - internal laptop display is conenected directly to discreted GPU, giving best performance but also worst battery life

Switching between two modes requires restart.

On Gen 7 and 8 laptops, there are additional 2 settings for Hybrid mode:

1. Hybrid iGPU-only - in this mode dGPU will be disconnected (think of it like ejecting USB drive), so there is no risk of it using power when you want to achieve best battery life
2. Hybrid Auto - similar to the above, but tries to automate the process by automatically disconnecting dGPU on battery power and reconnecting it when you plug in AC adapter

Discrete GPU may not disconnect, and in most cases will not disconnect, when it is used. That includes apps using dGPU, external monitor connected and probably some other cases that aren't specified by Lenovo. If you use the "Deactivate GPU" option in UDT, make sure that it reports dGPU Powered Off and no external screens are connected, before switching between Hybrid Modes in case you encounter problems.

All above settings are using built in functions of the EC and how well they work relies on Lenovo's firmware implementation. From my observations, they are reliable, unless you start switching them frequently. Be patient, because changes to this methods are not instantanous. UDT also attempts to mitigate these issues, by disallowing frequent Hybrid Mode switching and additional attempts to wake dGPU if EC failed to do so. It may take up to 10 seconds for dGPU to reappear when switching to Hybrid Mode, in case EC failed to wake it.

If you encounter issues, you might try to try alternative, experimental method of handling GPU Working Mode - see [Arguments](#arguments) section for more details.

> [!WARNING]
> Disabling dGPU via Device Manager DOES NOT disconnect the device and will cause high power consumption!

### Deactivate discrete NVIDIA GPU

Sometimes discrete GPU stays active even when it should not. This can happen for example, if you work with an external screen and you disconnect it - some processes will keep running on discrete GPU keeping it alive and shortening battery life.

There are two ways to help the GPU deactivate:

1. killing all processes running on dGPU (this one seems to work better),
2. disabling dGPU for a short amount of time, which will force all processes to move to the integrated GPU.

Deactivate button will be enabled when dGPU is active, you have Hybrid mode enabled and there are no screens connected to dGPU. If you hover over the button, you will see the current P state of dGPU and the list of processes running on it.

> [!NOTE]
> Some apps may not like this feature and crash when you use deactivate dGPU option.

### Overclock discrete NVIDIA GPUs

The overclock option is intended for simple overclocking, similar to the one available in Vantage. It is not intended to replace tools like Afterburner. Here are some points to keep in mind:
* Make sure GPU overclocking is enabled in BIOS, if your laptop has such option.
* Overclocking does not work with Vantage or LegionZone running in the background.
* It is not recommended to use the option while using other tools like Afterburner.
* If you edited your Dashboard, you might need to add the control manually.

### Windows Power Plans & Windows Power Mode

First of all, the Power Mode you see in UDT (or toggle with Fn+Q) **is not** the same as Power Plans (that you access from Control Panel) or Power Mode (that you can change from Settings app).

The modern (and recommended) approach is to use Windows Power Modes and only one, default, "Balanced (recommended)" power plan. You should have 3 Power Modes to choose from in Windows Settings app:

* Best power efficiency
* Balanced
* Best performance

You can assign these in UDT settings to each of Legion Power Modes: Quiet, Balance, Performance and Custom. If you choose to do so, respective Windows Power Mode will be automatically set when you change Legion Power Modes.

The legacy approach is to use multiple Power Plans, that some devices had installed from factory. If you decide to use them, or configure your own plans, leave the settings in Windows Settings app on the default "Balanced" setting. You can configure UDT to switch Power Plans automatically whenever you change the "Legion" Power Mode in UDT settings.

If you encounter issues with power mode or plan synchronization, especially when switching between the two approaches, you can reset Windows power settings to default using `powercfg -restoredefaultschemes; shutdown /r /t ` command. This command will reset all power plans to default and reboot your device. All plans except for the default "Balanced (recommended)" will be deleted, so make sure to make a copy, if you plan on using them again.

### Boot Logo

On some laptops, it is possible to change the boot logo (the default "Legion" image you see at boot). Boot logo is *not* stored in UEFI - it is stored on the UEFI partition on boot drive. When setting custom boot logo, UDT conducts basic checks, like resolution, image format and calculates a checksum to ensure compatibility. However, the real verification happens on the next boot. UEFI will attempt to load the image from UEFI partition and show it. If that fails for whatever reason, default image will be used. Exact criteria, except for resolution and image format, are not known and some images might not be shown. In this case, try another image, edited with different image editor.

### Running programs or scripts from actions

You can use "Run" step in Actions to start any program or script from Actions. To configure it, you need to provide path to the executable (`.exe`) or a script (`.bat`). Optionally, you can also provide arguments that the script or program supports - just like running anything from command line.

<details>
<summary>Examples</summary>

_Shutdown laptop_
 - Executable path: `shutdown`
 - Arguments: `/s /t 0`

_Restart laptop_
 - Executable path: `shutdown`
 - Arguments: `/r`

_Runing a program_
 - Executable path: `C:\path\to\the\program.exe` (if the program is on your PATH variable, you can use the name only)
 - Arguments: ` ` (optional, for list of supported argument check the program's readme, website etc.)

_Running a script_
 - Executable path: `C:\path\to\the\script.bat` (if the script is on your PATH variable, you can use the name only)
 - Arguments: ` ` (optional, for list of supported argument check the script's readme, website etc.)

_Python script_
 - Executable path: `C:\path\to\python.exe` (or just `python`, if it is on your PATH variable)
 - Arguments: `C:\path\to\script.py`

 </details>

#### Environment

UDT automatically adds some `LLT_*` compatibility variables to the process environment that can be accessed from within the script. They are useful for more advanced scripts, where context is needed. Depending on what was the trigger, different variables are added.

<details>
<summary>Environment variables</summary>

- When AC power adapter is connected
	- `LLT_IS_AC_ADAPTER_CONNECTED=TRUE`
- When low wattage AC power adapter is connected
	- `LLT_IS_AC_ADAPTER_CONNECTED=TRUE`
	- `LLT_IS_AC_ADAPTER_LOW_POWER=TRUE`
- When AC power adapter is disconnected
	- `LLT_IS_AC_ADAPTER_CONNECTED=FALSE`
- When Power Mode is changed:
	- `LLT_POWER_MODE=<value>`, where `value` is one of: `1` - Quiet, `2` - Balance, `3` - Performance, `255` - Custom
	- `LLT_POWER_MODE_NAME=<value>`, where `value` is one of: `QUIET`, `BALANCE`, `PERFORMANCE`, `CUSTOM`
- When game is running
	- `LLT_IS_GAME_RUNNING=TRUE`
- When game closes
	- `LLT_IS_GAME_RUNNING=FALSE`
- When app starts
	- `LLT_PROCESSES_STARTED=TRUE`
	- `LLT_PROCESSES=<value>`, where `value` is comma separated list of process names
- When app closes
	- `LLT_PROCESSES_STARTED=FALSE`
	- `LLT_PROCESSES=<value>`, where `value` is comma separated list of process names
- Lid opened
	- `LLT_IS_LID_OPEN=TRUE`
- Lid closed
	- `LLT_IS_LID_OPEN=FALSE`
- When displays turn on
	- `LLT_IS_DISPLAY_ON=TRUE`
- When displays turn off
	- `LLT_IS_DISPLAY_ON=FALSE`
- When external display is connected
	- `LLT_IS_EXTERNAL_DISPLAY_CONNECTED=TRUE`
- When external display is disconnected
	- `LLT_IS_EXTERNAL_DISPLAY_CONNECTED=FALSE`
- When HDR is on
	- `LLT_IS_HDR_ON=TRUE`
- When HDR is off
	- `LLT_IS_HDR_ON=FALSE`
- When WiFi is connected
	- `LLT_WIFI_CONNECTED=TRUE`
	- `LLT_WIFI_SSID=<value>`, where `value` is the SSID of the network
- When WiFi is disconnected
	- `LLT_WIFI_CONNECTED=FALSE`
- At specified time
	- `LLT_IS_SUNSET=<value>`, where `value` is `TRUE` or `FALSE`, depending on configuration of the trigger
	- `LLT_IS_SUNRISE=<value>`, where `value` is `TRUE` or `FALSE`, depending on configuration of the trigger
	- `LLT_TIME"`, where `value` is `HH:mm`, depending on configuration of the trigger
	- `LLT_DAYS"`, where `value` is comma separated list of: `MONDAY`, `TUESDAY`, `WEDNESDAY`, `THURSDAY`, `FRIDAY`, `SATURDAY`, `SUNDAY`, depending on configuration of the trigger
- Periodic action
	- `LLT_PERIOD=<value>`, where `value` is the interval in seconds
- On startup
	- `LLT_STARTUP=TRUE`
- On resume
	- `LLT_RESUME=TRUE`

</details>

#### Output

If "Wait for exit" is checked, UDT will capture the output from standard output of the launched process. This output is stored in `$RUN_OUTPUT$` variable and can be displayed in Show notification step.

### CLI

UDT ships two command-line surfaces with different responsibilities:

- `udt-cli.exe` is the Windows IPC remote control client. It requires the Electron app to be running in the background and the CLI option to be enabled.
- `udt` is the independent `UniversalDeviceToolkit.CrossPlatform` diagnostics CLI. It runs without the Electron app and is the Windows/macOS/Linux path for safe platform discovery and diagnostics.

The Windows IPC CLI executable is `udt-cli.exe` and can be found in the install directory.

For CLI to work properly, UDT needs to run in the background and CLI option needs to be enabled in UDT settings. You can also chose to add `udt-cli.exe` to your PATH variable for easier access.

CLI does not need to be ran as Administrator.

<details>
<summary>Features</summary>

* `udt-cli quickAction --list` - list all Quick Actions
* `udt-cli quickAction <name>` - run Quick Action with given `<name>`
* `udt-cli feature --list` - list all supported features
* `udt-cli feature get <name>` - get value of a feature with given `<name>`
* `udt-cli feature set <name> --list` - list all values for a feature with given `<name>`
* `udt-cli feature set <name> <value>` - set feature with given `<name>` to a specified `<value>`
* `udt-cli spectrum profile get` - get current profile Spectrum RGB is set to
* `udt-cli spectrum profile set <profile>` - set Spectrum RGB profile to `<profile>`
* `udt-cli spectrum brightness get` - get current brightness Spectrum RGB is set to
* `udt-cli spectrum brightness set <brightness>` - set Spectrum RGB brightness to `<brightness>`
* `udt-cli rgb get` - get current 4-zone RGB preset
* `udt-cli rgb set <profile>` - set 4-zone RGB to `<preset>`

</details>

## Plugins

UDT supports a comprehensive plugin system that allows extending the functionality of the application. Plugins can be installed, updated, and uninstalled dynamically with full UI support.

Official plugins for UDT live in this repository under [`Plugins/Official`](Plugins/Official). Their source, manifests, release metadata, and integration-specific assets are kept with the host so one pull request can update the complete product.

### Core Features

- **Dynamic Loading**: Plugins load at runtime from the host `plugins` directory (development packages: `Plugins/.build/plugins`; installed: `%LOCALAPPDATA%/UniversalDeviceToolkit/plugins/`)
- **Online Plugin Repository**: Browse and install plugins from an online repository
- **Dependency Management**: Automatic installation and checking of plugin dependencies
- **UI Integration**: Plugins can provide custom UI pages and settings
- **Feature Extensions**: Plugins can extend existing features or add new ones
- **Lifecycle Management**: Complete plugin lifecycle from installation to uninstallation
- **Download Progress**: Real-time download progress for online plugins
- **Executable Support**: Plugins can provide standalone executable files
- **Language Support**: Per-plugin language settings

### Plugin Types

- **System Plugins**: Built-in plugins that provide core functionality
- **Third-party Plugins**: Community-created plugins that extend UDT's capabilities

### Available Plugins

Official 1.x plugins are ZIP assets of the rolling [`plugin-catalog` release](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/tag/plugin-catalog) (v5.0.2 hosts). 6.0 preview hosts read [`plugin-catalog-preview`](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/tag/plugin-catalog-preview) for 2.x packages. The catalogs currently include:

- **Custom Mouse**: Cursor themes, pointer settings, and Windows optimization actions
- **Network Acceleration**: Built into **System Optimization → Network & acceleration** (plugin removed in v5.0.0)
- **Shell Integration**: Context menu and shell styling (system plugin)
- **ViVeTool**: Manage Windows feature flags and experimental features

### Plugin Management UI

UDT provides a comprehensive Plugin Extensions page with the following features:

- **Plugin Browsing**: View all available plugins (local and online)
- **Search & Filter**: Search plugins by name or description, filter by installation status
- **Plugin Details**: View detailed information about each plugin
- **Install/Uninstall**: Easy one-click installation and uninstallation
- **Online Updates**: Check for and install updates from the online repository
- **Permanent Deletion**: Option to permanently delete plugin files
- **Language Settings**: Set per-plugin language preferences

### Installing Plugins

Plugins can be installed in two ways:

1. **Online Installation**:
   - Open the Plugin Extensions page
   - Browse available plugins
   - Click on a plugin to view details
   - Click "Install" to download and install automatically

2. **Manual Installation** (advanced):
   - Download the plugin release ZIP from the main repository's [`plugin-catalog` release](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/tag/plugin-catalog), or build one with `Plugins\udt-plugin.cmd package`
   - Extract the ZIP into the host plugins directory (each plugin in its own subfolder):
     - Installed UDT: `%LOCALAPPDATA%\UniversalDeviceToolkit\plugins\`
     - Local dev build: `Plugins\.build\plugins\`
   - Restart UDT, or use the Plugin Extensions page to refresh installed plugins

### Plugin Development

Develop plugins in this repository under [`Plugins/Official`](Plugins/Official). Start with [`Docs/Plugins/PLUGIN_QUICKSTART.md`](Docs/Plugins/PLUGIN_QUICKSTART.md). Host-side contracts and UI standards are documented in [`Docs/Plugins/PLUGIN_DEVELOPMENT.md`](Docs/Plugins/PLUGIN_DEVELOPMENT.md).



## Credits

Special thanks to:

* [ViRb3](https://github.com/ViRb3), for creating [Lenovo Controller](https://github.com/ViRb3/LenovoController), which was used as a base for this tool
* [falahati](https://github.com/falahati), for creating [NvAPIWrapper](https://github.com/falahati/NvAPIWrapper) and [WindowsDisplayAPI](https://github.com/falahati/WindowsDisplayAPI)
* [SmokelessCPU](https://github.com/SmokelessCPU), for help with 4-zone RGB and Sprectrum keyboard support
* [Mario Bălănică](https://github.com/mariobalanica), for all contributions
* [Ace-Radom](https://github.com/Ace-Radom), for all contributions

Translations provided by:
* Bulgarian - [Ekscentricitet](https://github.com/Ekscentricitet)
* Chinese (Simplified) - [凌卡Karl](https://github.com/KarlLee830), [Ace-Radom](https://github.com/Ace-Radom)
* Chinese (Traditional) - [flandretw](https://github.com/flandretw)
* Czech - J0sef
* Dutch - Melm, [JarneStaalPXL](https://github.com/JarneStaalPXL)
* French - EliotAku, [Georges de Massol](https://github.com/jojo2massol), Rigbone, ZeroDegree
* German - Sko-Inductor, Running_Dead89
* Greek - GreatApo
* Italian - [Lampadina17](https://github.com/Lampadina17)
* Karakalpak - KarLin, Gulnaz, Niyazbek Tolibaev, Shingis Joldasbaev
* Latvian - RJSkudra
* Romanian - [Mario Bălănică](https://github.com/mariobalanica)
* Slovak - Mitschud, Newbie414
* Spanish - M.A.G.
* Polish - Mariusz Dziemianowicz
* Portugese - dvsilva
* Portuguese (Brasil) - Vernon
* Russian - [Edward Johan](https://github.com/younyokel)
* Turkish - Undervolt
* Ukrainian -  [Vladyslav Prydatko](https://github.com/va1dee), [Dmytro Zozulia](https://github.com/Nollasko)
* Vietnamese - Not_Nhan, Kuri, Nagidrop

Many thanks to everyone else, who monitors and corrects translations!

## FAQ

- [Why do I get a message that Vantage is still running, even though I uninstalled it?](#why-do-i-get-a-message-that-vantage-is-still-running-even-though-i-uninstalled-it)
- [Why is my antivirus reporting that the installer contains a virus/trojan/malware?](#why-is-my-antivirus-reporting-that-the-installer-contains-a-virustrojanmalware)
- [Can I customize hotkeys?](#can-i-customize-hotkeys)
- [Can I customize Conservation mode threshold?](#can-i-customize-conservation-mode-threshold)
- [Can I customize fans in Quiet, Balance or Performance modes?](#can-i-customize-fans-in-quiet-balance-or-performance-modes)
- [Why can't I switch to Performance or Custom Power Mode on battery?](#why-cant-i-switch-to-performance-or-custom-power-mode-on-battery)
- [Why does switching to Performance mode seem buggy, when AI Engine is enabled?](#why-does-switching-to-performance-mode-seem-buggy-when-ai-engine-is-enabled)
- [Why am I getting incompatible message after motherboard replacement?](#why-am-i-getting-incompatible-message-after-motherboard-replacement)
- [Why isn't a game detected, even though Actions are configured properly?](#why-isnt-a-game-detected-even-though-actions-are-configured-properly)
- [Can I use other RGB software while using UDT?](#can-i-use-other-rgb-software-while-using-udt)
- [Will iCue RGB keyboards be supported?](#will-icue-rgb-keyboards-be-supported)
- [Can I have more RGB effects?](#can-i-have-more-rgb-effects)
- [Can you add fan control to other models?](#can-you-add-fan-control-to-other-models)
- [Why don't I see the custom tooltip when I hover the UDT icon in tray?](#why-dont-i-see-the-custom-tooltip-when-i-hover-the-udt-icon-in-tray)
- [How can I OC/UV my CPU?](#how-can-i-ocuv-my-cpu)
- [What if I overclocked my GPU too much?](#what-if-i-overclocked-my-gpu-too-much)
- [Why is my Boot Logo not applied?](#why-is-my-boot-logo-not-applied)
- [Why do I see stuttering when using Smart Fn Lock?](#why-do-i-see-stuttering-when-using-smart-fn-lock)
- [Which generation is my laptop?](#which-generation-is-my-laptop)



#### Why do I get a message that Vantage is still running, even though I uninstalled it?

Starting from version 2.14.0, UDT is much more strict about detecting leftover processes related to Vantage. Vantage installs 3 components:

1. Lenovo Vantage app
2. Lenovo Vantage Service
3. System Interface Foundation V2 Device

The easiest solution is to go into UDT settings and select options to disable Lenovo Vantage, LegionZone and Hotkeys (only still installed ones are shown).

If you want to remove them instead, make sure that you uninstall all 3, otherwise some options in UDT will not be available. You can check Task Manager for any processes containing `Vantage` or `ImController`. You can also check this guide for more info: [Uninstalling System Interface Foundation V2 Device](https://support.lenovo.com/us/en/solutions/HT506070), if you have troubles getting rid of `ImController` processes.

#### Why is my antivirus reporting that the installer contains a virus/trojan/malware?

UDT makes use of many low-level Windows APIs that can be falsely flagged by antiviruses as suspicious, resulting in a false-positive. UDT is open source and can easily be audited by anyone who has any doubts as to what this software does. Release payloads and installers are built on GitHub with GitHub Actions, signed through Azure Trusted Signing, and verified before publication. Unsigned local builds should not be treated as official release artifacts.

If you downloaded the installer from this projects website, you shouldn't worry - the warning is a false-positive. That said, if you can help with resolving this issue, let's get in touch.

#### Can I customize hotkeys?

You can customize Fn+F9 hotkey in UDT settings. Other hotkeys can't be customized.

#### Can I customize Conservation mode threshold?

No. Conservation mode threshold is set in firmware to 60% (2021 and earlier) or 80% (2022 and later) and it can't be changed.

#### Can I customize fans in Quiet, Balance or Performance modes?

No, it isn't possible to customize how the fan works in power modes other than Custom.

#### Why can't I switch to Performance or Custom Power Mode on battery?

Starting with version 2.11.0, UDT's behavior was aligned with Vantage and Legion Zone and it does not allow using them without an appropriate power source.

If for whatever reason you want to use these modes on battery anyway, you can use `--allow-all-power-modes-on-battery` argument. Check [Arguments](#arguments) section for more details.

> [!WARNING]
> Power limits and other settings are not applied correctly on most devices when laptop is not connected to full power AC adapter and unpredictable and weird behavior is expected. Therefore, no support is provided for issues related to using this argument.*

#### Why does switching to Performance mode seem buggy, when AI Engine is enabled?

It seems that some BIOS versions indeed have weird issues when using Fn+Q. Only hope is to wait for Lenovo to fix it.

#### Why am I getting incompatible message after motherboard replacement?

Sometimes new motherboard does not contain correct model numbers and serial numbers. You should try [this tutorial](https://laptopwiki.eu/laptopwiki/guides/lenovo/legion_bios_lvarrecovery) to try and recover them.

#### Why isn't a game detected, even though Actions are configured properly?

Game detection feature is built on top of Windows' game detection, meaning UDT will react to EXE files that Windows considers "a game". That also means that if you nuked Xbox Game Bar from your installation, there is 99.9% chance this feature will not work.

Windows probably doesn't recognize all games properly, but you can mark any program as game in Xbox Game Bar settings (Win+G). You can find list of recognized games in registry: `HKEY_CURRENT_USER\System\GameConfigStore\Children`.

#### Can I use other RGB software while using UDT?

In general, yes. UDT will disable RGB controls when Vantage is running to avoid conflicts. If you use other RGB software like [L5P-Keyboard-RGB](https://github.com/4JX/L5P-Keyboard-RGB) or [OpenRGB](https://openrgb.org/), you can disable RGB in UDT to avoid conflicts with `--force-disable-rgbkb` or `--force-disable-spectrumkb` argument. Check [Arguments](#arguments) section for more details.

#### Will iCue RGB keyboards be supported?

No. Check out [OpenRGB](https://openrgb.org/) project.

#### Can I have more RGB effects?

Only options natively supported by hardware are available; adding support for custom effects is not planned. If you would like more customization check out [L5P-Keyboard-RGB](https://github.com/4JX/L5P-Keyboard-RGB) or [OpenRGB](https://openrgb.org/).

#### Can you add fan control to other models?

Fan control is available on Gen 7 and later models. Older models will not be supported due to technical limitations.

#### Why don't I see the custom tooltip when I hover the UDT icon in tray?

In Windows 10 and 11, Microsoft did plenty of changes to the tray, breaking a lot of things on the way. As a results custom tooltips not always work properly. Solution? Update your Windows and keep fingers crossed.

#### How can I OC/UV my CPU?

There are very good tools like [Intel XTU](https://www.intel.com/content/www/us/en/download/17881/intel-extreme-tuning-utility-intel-xtu.html) (which is used by Vantage) or [ThrottleStop](https://www.techpowerup.com/download/techpowerup-throttlestop/) made just for that.

#### What if I overclocked my GPU too much?

If you end up in a situation where your GPU is not stable and you can't boot into Windows, there are two things you can do:

1. Go into BIOS and try to find and option similar to "Enabled GPU Overclocking" and disable it, start Windows, and toggle the BIOS option again to Enabled.
2. Start Windows in Safe Mode, and delete `gpu_oc.json` file under the compatibility settings directory, which is located in `"%LOCALAPPDATA%\UniversalDeviceToolkit`.

#### Why is my Boot Logo not applied?

When you change the Boot Logo, UDT verifies that it is in the format that is correct format and correct resolution. If UDT shows that boot logo is applied, it means that the setting was correctly saved to UEFI. If you don't see the custom boot logo, it means that even though UEFI is configured and custom image is saved to UEFI partition, your UEFI for some reason does not render it. In this case the best idea is to try a different image, maybe in different format, edited with different image editor etc. If the boot logo is not shown after all these steps, it's probably a problem with your BIOS version.

#### Why do I see stuttering when using Smart Fn Lock?

On some BIOS versions, toggling Fn Lock causes a brief stutter and since Smart Fn Lock is basically an automatic toggle for Fn Lock, it is also affected by this issue. Try disabling "Fool proof Fn Lock" (or similar) option in BIOS - it was reported that it fixes stutter when toggling Fn Lock.

#### Why don't I see warranty infos in device information?

In latest version UDT removes this feature for Chinese models due to increasing unreliability. If you got warranty infos before it should be displayed normally, but after manually refreshing or deleting stored datas the infos will disappear. This change only affects users with a Chinese Legion laptop.

#### Which generation is my laptop?

Check the model number. Example model numbers are `16ACH6H` or `16IAX7`. The last number of the model number indicates generation.

## Arguments

Some, less frequently needed, features or options can be enabled by using additional arguments. These arguments can either be passed as parameters or added to `args.txt` file.

* `--trace` - enables logging to `%LOCALAPPDATA%\UniversalDeviceToolkit\logs`
* `--minimized` - starts UDT minimized to tray
* `--disable-tray-tooltip` - disables tray tooltip that is shown when you hover the cursors over tray icon
* `--allow-all-power-modes-on-battery` - allows using all Power Modes without AC adapter _(No support is provided when this argument is used)_
* `--force-disable-rgbkb` - disables all lighting features for 4-zone RGB keyboards
* `--force-disable-spectrumkb` - disables all lighting features for Spectrum per-key RGB keyboards
* `--force-disable-lenovolighting` - disables all lighting features related to panel logo, ports backlight and some white backlit keyboards
* `--experimental-gpu-working-mode` - changes GPU Working Mode switch to use experimental method, that is used by LegionZone _(No support is provided when this argument is used)_
* `--proxy-url=example.com` - specifies proxy server URL that UDT should use
* `--proxy-username=some_username` - if applicable, specifies proxy server username to use
* `--proxy-password=some_password` - if applicable, specifies proxy server password to use
* `--proxy-allow-all-certs` - if needed relaxes criteria needed to establish HTTPS/SSL connections via proxy server
* `--disable-update-checker` - disable update checks in UDT, in case you want to rely on winget, scoop etc.

If you decide to use the arguments with `args.txt` file:
1. Go to `%LOCALAPPDATA%\UniversalDeviceToolkit`
2. Create or edit `args.txt` file in there
3. Paste **one** argument per line
4. Start UDT

Arguments not listed above are no longer needed or available.

## How to collect logs?

In all troubleshooting situations, logs provide important information. **Always** attach logs to your issues. Critical error logs are saved automatically and saved under `"%LOCALAPPDATA%\UniversalDeviceToolkit\logs"`.

To collect logs:

1. Make sure that Universal Device Toolkit is not running (also gone from tray area).
2. Open `Run` (Win+R) and start the app with `--trace`. During the rename, the compatibility path may still be `"%LOCALAPPDATA%\Programs\UniversalDeviceToolkit\Lenovo Legion Toolkit.exe" --trace`.
3. UDT will start and in the title bar you should see: `[LOGGING ENABLED]`
4. Reproduce the issue you have (i.e. try to use the option that causes issues)
5. Close UDT (also make sure it's gone from tray area)
6. Again, in `Run` (Win+R) type `"%LOCALAPPDATA%\UniversalDeviceToolkit\logs"`
7. You should see at least one file. Theses are the logs you should attach to the issue.

## Contribution

I appreciate any feedback that you have, so please do not hesitate to report issues.
Pull Requests are also welcome, but make sure to check out [CONTRIBUTING.md](CONTRIBUTING.md) first!

#### Compatibility

> [!IMPORTANT]
> **Hardware-control requests** are limited to Lenovo Legion, IdeaPad Gaming, and LOQ series — please do not open issues asking for full Vantage-style control on other brands or unsupported Lenovo lines.
>
> **Basic-mode contributions are welcome**: Non-Lenovo and unsupported Lenovo PCs run in basic mode (plugins, system tools, language/theme/update/log workflows). Device-pack data, logs, and testing feedback for broader basic-mode coverage are appreciated.

It would be great to expand the list of compatible devices, but to do it your help is needed!

If you are willing to check if this app works correctly on your device that is currently unsupported, click _Continue_ on the popup you saw on startup. Universal Device Toolkit will start logging automatically so you can submit them if anything goes wrong.

*Remember that some functions may not function properly.*

I would appreciate it, if you create an issue here on GitHub with the results of your testing.

Make sure to include the following information in your issue:

1. Full model name (i.e. Legion 5 Pro 16ACH6H)
2. List of features that are working as expected.
3. List of features that seem to not work.
4. List of features that crash the app.

The more info you add, the better the app will get over time. If anything seems off, write down precisely what was wrong and attach logs (`%LOCALAPPDATA%\UniversalDeviceToolkit\log`).

## Localization

UDT localization is managed through Crowdin with a repository-level config at `crowdin.yml`.

- Source files: neutral `Resource.resx` in five modules:
  - `UniversalDeviceToolkit.Lib/Resources`
  - `UniversalDeviceToolkit.Lib.Automation/Resources`
  - `UniversalDeviceToolkit.Lib.Macro/Resources`
  - `UniversalDeviceToolkit.Lib.Plugins/Resources`
  - `UniversalDeviceToolkit.CLI/Resources` (`CLI.Resources.resx`)
  The Electron UI strings live in `UniversalDeviceToolkit.Electron/src/renderer/src/i18n/locales/` (TS modules).
- Target files: `Resource.<locale>.resx` (or `CLI.Resources.<locale>.resx` for CLI) beside each source file. Culture names use the BCP 47 canonical form (`zh-Hans`, `zh-Hant`, `pt-BR`, `nl-NL`, `uz-Latn-UZ`) — enforced by `Scripts/Assert-CultureNaming.ps1`.
- Locale mapping is defined in `crowdin.yml` (for example `zh-CN -> zh-Hans`, `zh-TW -> zh-Hant`, `pt-BR -> pt-BR`).

Typical CLI commands:

```bash
# upload source strings
crowdin upload sources --config crowdin.yml

# upload existing translations
crowdin upload translations --config crowdin.yml

# download translated files
crowdin download --config crowdin.yml
```

## Documentation

Additional documentation is indexed in [Docs/README.md](Docs/README.md). Topic docs in `Docs/`:

| Document | Description |
|----------|-------------|
| [ARCHITECTURE.md](Docs/ARCHITECTURE.md) | System architecture, components, and data flow |
| [DEPLOYMENT.md](Docs/DEPLOYMENT.md) | Build, test, deployment, and release procedures |
| [PLUGIN_DEVELOPMENT.md](Docs/Plugins/PLUGIN_DEVELOPMENT.md) | Plugin SDK and implementation guide |
| [RELEASE_AND_MIGRATION.md](Docs/Plugins/RELEASE_AND_MIGRATION.md) | Plugin release, repository migration, and legacy-client upgrade |
| [LanguagePacks.md](Docs/LanguagePacks.md) | Online language pack catalog protocol and lifecycle |
| [NetworkAcceleration.md](Docs/NetworkAcceleration.md) | Built-in network & acceleration module |
| [PROMOTION_EN.md](Docs/PROMOTION_EN.md) | Release and social promotion copy (English) |
| [PROMOTION_CN.md](Docs/PROMOTION_CN.md) | Release and social promotion copy (Chinese) |
| [COMMUNITY_OUTREACH.md](Docs/COMMUNITY_OUTREACH.md) | Community posting playbook and submission tracker |
| [SECURITY.md](Docs/SECURITY.md) | Security policy and best practices |
| [CODE_OF_CONDUCT.md](Docs/CODE_OF_CONDUCT.md) | Community guidelines and contribution standards |

### Screenshots

Captured at **1300×850** logical window size (pixel dimensions follow Windows display scale). README images are displayed at 700 px width.

| File | Description |
|------|-------------|
| `Assets/UDT_Promo.mp4` | 30-second trailer we cut ourselves (click the poster in the [Trailer](#trailer) section) |
| `Assets/UDT_Promo_poster.jpg` | Trailer poster |
| `Assets/Screenshot_main.png` | Main application interface (English, Dark theme) |
| `Assets/Screenshot_zh-hans.png` | Chinese localization interface (Dark theme) |

Refresh procedure (keep 1300×850 logical size): see [DEPLOYMENT.md](Docs/DEPLOYMENT.md#readme-screenshots). Brand binaries are unified under repo-root [`Assets/`](Assets/README.md).

### Troubleshooting

- **Application won't start?** Check [.NET 10.0 installation](#problems-with-net)
- **Features not working?** See [compatibility](#compatibility) section
- **Logs needed?** Follow [log collection](#how-to-collect-logs) guide
- **Still need help?** Open a [GitHub Issue](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/issues)

## License and Attribution

Universal Device Toolkit is distributed under the GNU GPL v3.0. See [LICENSE](LICENSE).

This project is a modified continuation derived from [Lenovo Legion Toolkit](https://github.com/BartoszCichecki/UniversalDeviceToolkit), originally created by Bartosz Cichecki. Original author attribution and copyright information are preserved in [NOTICE](NOTICE); Universal Device Toolkit changes are maintained by Universal Device Toolkit contributors.

---

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=SSC-STUDIO/UniversalDeviceToolkit&type=Date)](https://star-history.com/#SSC-STUDIO/UniversalDeviceToolkit&Date)

<div align="center">

### ⭐ Help us reach 1,000 stars! ⭐

If UDT makes your Legion (or any Windows PC) run leaner -- Fn+Q, RGB, dGPU, plugins, no Vantage bloat -- please give us a star. Every star helps us reach 1,000 and signals that the plugin model is worth building out.

[![Star this repo](https://img.shields.io/github/stars/SSC-STUDIO/UniversalDeviceToolkit?style=social&label=Star%20UDT)](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/stargazers)

**Contributors welcome!** Check out [CONTRIBUTING.md](CONTRIBUTING.md) to get started.

</div>
