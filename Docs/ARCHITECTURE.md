# Universal Device Toolkit Architecture

## Overview

Universal Device Toolkit (UDT, formerly Lenovo Legion Toolkit) is a lightweight desktop application with an Electron UI and a headless .NET backend: full Lenovo hardware control on supported Windows machines, plugin extensions, and safe basic-mode workflows on other PCs and on macOS/Linux. The application follows a modular architecture pattern with clear separation of concerns and treats plugin extensions as a primary expansion path.

## Quick Start

### For Users

1. **Download** the latest release from [GitHub Releases](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases)
2. **Install** the application by running the installer
3. **Launch** UDT and configure your preferred settings
4. **Use** supported hardware controls or basic-mode plugin and system tools

### For Developers

1. **Prerequisites**: Install .NET 10 SDK, Visual Studio 2022 and Node.js 20+
2. **Clone** the repository: `git clone https://github.com/SSC-STUDIO/UniversalDeviceToolkit.git`
3. **Build** the solution: `dotnet build UniversalDeviceToolkit.sln`
4. **Run** tests: see [TEST_DIAGNOSTICS.md](./TEST_DIAGNOSTICS.md) (`Tests.Contracts` → `Fast.Tests` → `Tests` → `Tests.Stateful`)
5. **Start the UI (Electron)**: `cd UniversalDeviceToolkit.Electron && npm ci && npm run dev`
   In Visual Studio, set the `UniversalDeviceToolkit.Electron` launcher project as
   the startup project and press F5 (its "Electron (npm run dev)" launch profile
   runs `npm run dev`). Do **not** set `UniversalDeviceToolkit.Host` as the startup
   project — it is a headless backend spawned automatically by Electron.
6. **Start** developing! See [AGENTS.md](../AGENTS.md) for detailed development guidelines.

## System Architecture

The client UI is an **Electron app** (Node.js + electron-vite + React) in
`UniversalDeviceToolkit.Electron/`. It talks over JSON-RPC (newline-delimited,
stdio) to the **UniversalDeviceToolkit.Host** process — a headless .NET backend
that hosts all business logic (hardware control, sensors, settings, plugins).
Electron's main process only owns the UI shell (window, tray, OSD, dialogs);
it forwards every other `bridge:invoke` call to the Host. The Host is spawned
automatically by Electron (dev: `bin/x64/Debug/.../Host.exe`; packaged:
`resources/host/`); it never shows a window.

```
+-----------------------------------------------------------------------+
|                        Universal Device Toolkit                          |
+-----------------------------------------------------------------------+
| Presentation Layer (Electron renderer: React + Ant Design + ECharts)    |
| +--------------------------------------------------------------------+ |
| | UniversalDeviceToolkit.Electron/src/renderer                    |  |  |
| | +- Pages, Components, Stores (Zustand), i18n                    |  |  |
| | +- api/*: typed bridge.invoke wrappers                          |  |  |
| +--------------------------------------------------------------------+ |
| Electron main process (window, tray, OSD, single-instance, dialogs)    |
|   └─ bridge:invoke ──► Host (JSON-RPC over stdio)                      |
| +--------------------------------------------------------------------+ |
| Host Layer (UniversalDeviceToolkit.Host: Rpc/Handlers/*)               |
| | UniversalDeviceToolkit.Host       (headless JSON-RPC server)      |  |
| | UniversalDeviceToolkit.CLI       | Automation     | Macro         |  |
| | UniversalDeviceToolkit.Lib.Automation | Toolkit.Lib.Macro         |  |
| +--------------------------------------------------------------------+ |
| Core Library Layer                                                      |
| +--------------------------------------------------------------------+ |
| | UniversalDeviceToolkit.Lib (assembly: UniversalDeviceToolkit.Lib)  |  |
| | +- Hardware Controllers (34 modules)                          |  |  |
| | +- Services (Settings, Messaging, IoC)                        |  |  |
| | +- Game Detection System                                      |  |  |
| | +- Plugin System                                              |  |  |
| | +- Native Interop (WMI, ACPI, USB/HID)                      |  |  |
| +--------------------------------------------------------------------+ |
+-----------------------------------------------------------------------+
| Infrastructure                                                          |
| +- Autofac (Dependency Injection)                                    |
| +- HID Sharp (Hardware Interface)                                    |
| +- LibreHardwareMonitorLib (System Monitoring)                        |
| +- Native Windows APIs (WMI, Power, etc.)                           |
+-----------------------------------------------------------------------+
```

## Platform Notes

The Electron UI shell is cross-platform, but each OS gets its platform-native
window chrome and capabilities. Implementation map (all under
`UniversalDeviceToolkit.Electron/src/main/`):

| Surface | Windows | macOS | Linux | Implementation |
|---|---|---|---|---|
| Title bar | Frameless custom title bar with right-aligned window buttons (Mica background material) | Native title bar with traffic lights (hiddenInset) + vibrancy | Frameless custom title bar with right-aligned window buttons | `index.ts` `createWindow()` (`frame: false` / `titleBarStyle: 'hiddenInset'` branch); renderer `TitleBar.tsx` hides its buttons on `darwin` |
| Menu bar | Auto-hidden (frameless) | Native system menu bar (App/File/Edit/View/Window/Help roles) | Auto-hidden (frameless) | `menu.ts` `installApplicationMenu()` — macOS only; `hasNativeMenuBar()` |
| Tray | Tray icon + custom flyout (navigation, quick actions, open/close) | Tray icon + custom flyout | Tray icon + custom flyout | `tray.ts` `initTray()` — all platforms |
| OSD overlay | Transparent always-on-top window fed by Host sensor data | Same window; no meaningful sensor data in basic mode | Same window; no meaningful sensor data in basic mode | `osd-window.ts` |
| System power actions (restart/shutdown/sleep) | Via `shutdown.exe` | Unavailable (spawn fails) | Unavailable (spawn fails) | `system-power.ts` |
| Windows power plans | Via `powercfg` | Unavailable | Unavailable | `power-plans.ts` |
| App lifecycle | Quit when last window closes | Stays running (macOS convention); window recreated on `activate` | Quit when last window closes | `index.ts` `window-all-closed` / `activate` |
| Start on login | Host scheduled task (`app.setAutorun`) launching the Electron shell via `UDT_SHELL_PATH` | Electron login item (`app.setLoginItemSettings`) | XDG autostart `.desktop` | Settings page picks the channel by `bridge.platform` |

The Host backend (`.NET`) is Windows-first: it targets the Windows TFM
`net10.0-windows10.0.26100.0` and drives hardware through WMI/registry/vendor
drivers, so hardware control is meaningful on Windows only. macOS/Linux run the
Electron UI in basic mode (plugins, system optimization, themes, updates,
logs). Per-platform Host publish RIDs (`win-x64` / `osx-arm64` / `osx-x64` /
`linux-x64`) are documented in [DEPLOYMENT.md](DEPLOYMENT.md).

### Shell exceptions (stay in Electron main)

These `bridge:invoke` methods are answered by the Electron main process, not
Host JSON-RPC. They are OS-shell or installer concerns and are **not** migrated
to Host in this phase:

| Method | Owner |
|---|---|
| `powerPlans.getList` / `powerPlans.setActive` | `power-plans.ts` (`powercfg`, Windows only) |
| `power.restart` / `power.shutdown` / `power.sleep` | `system-power.ts` |
| `update.getRelease` / `update.download` / `update.launchInstaller` | `update-downloader.ts` (GitHub release + installer launch) |
| `device.info` | Main-process device snapshot (falls back to `system.info` in the UI) |
| `dialog:*`, `log.open-folder`, `status-window.show` | Native dialogs, folders, tray status popup |

Non-Windows Host builds register the Windows-only RPC names as `-32099`
(`Not supported on this platform.`) so the renderer never waits on unknown-method
errors. Plugin marketplace methods stay registered on every platform.

Host JSON-RPC errors keep their numeric code in the message as `[UDT:<code>]`
so the UI can map `-1006` (elevation), `-1010` (missing NetworkProxy), `-1011`
(Hosts mode refused), `-1012` (start refused), and `-32099`.

## Core Components

### 1. UniversalDeviceToolkit.Electron (Presentation Layer)

The Electron client implementing the React UI and the window shell:

- **`src/renderer/`**: Pages (Dashboard, Keyboard, Automation, Macro, Optimization, Plugins, Settings), Components, Zustand stores, `api/*` typed bridge wrappers, `i18n/locales/*` (TS modules). Live sensor panels live in `components/dashboard/`; feature cards and GPU extras live in `components/dashboard-parity/` (WPF dashboard-control parity, not a second app).
- **`src/main/`**: Main process shell — window creation (`index.ts`), tray (`tray.ts`), OSD (`osd-window.ts`), macOS menu (`menu.ts`), single-instance, dialogs, host client (`host-client.ts`)
- **`src/preload/`**: Context-isolated bridge (`index.ts`), plugin webview host (`plugin-host.ts`)

### 2. UniversalDeviceToolkit.Lib (Core Library; assembly `UniversalDeviceToolkit.Lib`)

The heart of the application containing:

#### Controllers (34 hardware modules)
- `PowerModeController`: Power mode management
- `FanController`: Fan speed control and curves
- `RGBController`: Keyboard and lighting control
- `GPUController`: GPU mode switching (dGPU, Hybrid, iGPU)
- `MacroController`: Macro key handling
- `CameraController`: Camera power management
- And 29 more specialized controllers

#### Services
- `SettingsService`: Persistent configuration storage
- `UpdateService`: Application updates
- `PackageDownloader`: Driver/firmware updates
- `GameDetectionService`: Active game detection
- `PluginManager`: Dynamic plugin loading

#### Features
- `IAutomationFeature`: Automated actions based on triggers
- `ITriggerFeature`: Event-driven automation

#### Native Interop
- `Native.cs`: P/Invoke declarations for Windows APIs
- WMI integration for hardware queries
- ACPI communication for firmware access

### 3. UniversalDeviceToolkit.Lib.Automation

Automation system implementing a rule-based engine:

- **Triggers**: Application launch, game detection, AC plugged/unplugged
- **Conditions**: Time-based, power state, user presence
- **Actions**: Power mode change, fan curve, RGB profile, macro activation

### 4. UniversalDeviceToolkit.Lib.Macro

Macro recording and playback system:

- Key sequence recording
- Macro storage and management
- Integration with hardware macro keys

### 5. UniversalDeviceToolkit.CLI

Command-line interface for headless operation:

- Power mode queries and changes
- Status monitoring
- Automation rule management

## Plugin System Architecture

UDT supports dynamic plugin loading through a structured API:

```
Plugin Structure (runtime, in host plugins directory):
+-- plugin.manifest.json    # Authoring manifest (also packaged for compatibility)
+-- plugin.json             # Generated runtime manifest output (legacy-compatible)
+-- plugin.dll              # Main plugin assembly
+-- [dependencies]          # Additional assemblies (e.g. UniversalDeviceToolkit.Plugins.SDK.dll)
+-- [resources]             # Plugin resources
```

Official plugins live under `Plugins/Official/` in this repository. They are built by the monorepo plugin workflows. Stable 1.x packages publish as assets of the rolling `plugin-catalog` release; 6.0 preview 2.x packages publish to `plugin-catalog-preview`. The host loads packaged output from the catalog that matches its version label.

### Plugin Types

Host-side categories (legacy):

1. **Feature Plugins**: Add new automation features
2. **Integration Plugins**: Third-party service integrations
3. **Tool Plugins**: Standalone utilities

Author scaffolding under `Plugins/Templates/PluginArchetypes/` uses `settings-only`, `feature-settings`, and `runtime-optimization` templates, which map to settings pages, feature + settings pages, and Windows optimization integrations respectively.

### Plugin Lifecycle

```
Loading -> Initialization -> Registration -> Activation -> Shutdown
  |              |                |              |           |
  |              +----------------+--------------+-----------+
  |                              Active State
  +-> Disabled/Failed -> Unloaded
```

## Data Flow

### Power Mode Change Flow

```
User Action (UI)
      -> Renderer api/ bridge.invoke('feature.setPowerMode', ...)
      -> Electron main (bridge:invoke) -> Host JSON-RPC
      -> PowerModeController.SetModeAsync() (Host)
      -> WMI Call (\\ROOT\WMI\Lenovo_Path)
      -> ACPI Communication
      -> Hardware Response
      -> Windows Power Plan Sync
      -> State Update Broadcast (bridge:event)
      -> UI Refresh
```

### Game Detection Flow

```
GameDetectionService (Background Monitor)
      -> Window Title / Process Matching
      -> Plugin Notifications
      -> Automation Rules Evaluation
      -> Automatic Actions Execution
```

### Bridge RPC error codes

Error codes are defined once in `UniversalDeviceToolkit.Host/Rpc/BridgeErrorCodes.cs`
and mapped to localized messages by the renderer (`src/renderer/src/api/bridge.ts`).

- `-32601` unknown method, `-32602` invalid params, `-32603` internal error,
  `-32800` request cancelled (JSON-RPC protocol range, produced by
  `BridgeRpcServer` and handler argument validation).
- `-32099` platform not supported: whole Windows-only domain on a portable
  host. The method list lives in `Rpc/RpcMethodNames.cs` - the single source
  shared by the Windows registration check (`Program.VerifyRpcSurface`) and
  the portable stubs, so the two surfaces cannot drift.
- `-32001` God Mode not supported by the device generation.
- `-1001` feature not supported, `-1002` AC power required, `-1004` undefined
  state, `-1005` macro hooks failed, `-1006` elevation required,
  `-1010` NetworkProxy.exe missing, `-1011` hosts mode refused,
  `-1012` network start refused (application-level conditions).

## Technology Stack

| Layer | Technology/Framework |
|-------|---------------------|
| UI Framework | Electron 43 + React 19 (electron-vite, Ant Design, ECharts) |
| UI Logic | React components + Zustand stores; `api/*` typed bridge wrappers |
| Backend | .NET 10 headless Host (`UniversalDeviceToolkit.Host`) over JSON-RPC (stdio) |
| Architecture | Clean Architecture (UI shell ↔ Host ↔ Core Lib) |
| DI Container | Autofac (Host) |
| Hardware Access | WMI, ACPI, Windows native APIs (Windows only) |
| Monitoring | Built-in sensors and controller queries |
| Settings | JSON file storage |
| Updates | GitHub Releases API |
| Localization | Crowdin + Electron i18n TS modules + `.resx` satellites |

## Namespace and assembly naming

User-facing product names and **primary plugin/host ABI** are both **Universal Device Toolkit**:

| Surface | Primary identity |
| --- | --- |
| Product / Electron process | Universal Device Toolkit |
| Core Lib assembly / namespaces | `UniversalDeviceToolkit.Lib` |
| Plugins host assembly / namespaces | `UniversalDeviceToolkit.Lib.Plugins` |
| Windows IPC CLI executable | `udt-cli.exe` (`AssemblyName` = `udt-cli`) |
| Cross-platform diagnostics CLI | `udt` (`UniversalDeviceToolkit.CrossPlatform`) |

Phase 3 hard cutover from `LenovoLegionToolkit.Lib*` is **complete**. Remaining LLT tokens (legacy IPC pipe `LenovoLegionToolkit-IPC-0`, `BrandCompatibility.Legacy*`, dual-written `LLT_*` env keys, legacy `LenovoLegionToolkit.Plugins.*` load prefixes, packaging IDs) are deliberate compatibility surfaces — not the primary ABI.

See **[NamespaceMigration.md](./NamespaceMigration.md)** for the RootNamespace/AssemblyName inventory, completed Phases 0–3, and remaining legacy compat notes.

## Key Design Decisions

1. **No Background Service**: Application runs only when user is logged in
2. **No Telemetry**: Complete user privacy
3. **Lightweight**: Minimal resource footprint
4. **Plugin Extensibility**: Dynamic module loading for device-specific workflows
5. **Catalog-backed Device Support**: Data-driven hardware/basic-mode profiles across Lenovo families and common PC vendors
6. **Primary plugin ABI is UDT-named**: Core Lib assemblies are `UniversalDeviceToolkit.Lib*`; host still accepts selected legacy plugin prefixes and dual pipes during transition (see [NamespaceMigration.md](./NamespaceMigration.md))

## Platform Compatibility

- **Windows**: 10 (1809+), 11 (x64 only) — full hardware control + basic mode
- **macOS / Linux**: Electron client in basic mode (plugins, system optimization, themes, updates, logs; no Lenovo hardware control); hardware control is Windows-only
- **Hardware (code-driven detection)**:
  - Hardware-control profiles: Legion 5/Slim 5/Pro 5, Legion 7/Pro 7/9, Legion Go, LOQ, IdeaPad Gaming, ThinkBook, YOGA, Lenovo Slim, selected legacy Lenovo gaming families
  - Basic-mode profiles: ThinkPad, ThinkCentre, ThinkStation, IdeaCentre, Legion desktop, XiaoXin, V series, Motorola, ASUS, MECHREVO/Mechanical Revolution, Dell, HP, Acer, MSI, Microsoft Surface, GIGABYTE/AORUS, Razer, Samsung, HUAWEI, Xiaomi/Redmi, HONOR, LG, Framework, Panasonic, Dynabook/Toshiba, Fujitsu, VAIO, MEDION, XMG/SCHENKER, System76, Star Labs, Slimbook, Clevo/Tongfang, and generic PCs
  - v4.0 adds a local device-support simulation matrix for ASUS, MECHREVO, HP, Dell, Acer, Xiaomi, and Huawei machine profiles, plus generic CPU/GPU sensor fallback for non-Lenovo basic mode
  - Chinese model naming variants are recognized where hardware control is supported (for example `R7000`, `R9000`, `Y7000`, `Y9000`)
  - Vendor matching normalizes common BIOS/DMI formatting differences so punctuation, casing, spacing, diacritics, and company suffix variants do not block a basic-mode match
  - Detection source: `UniversalDeviceToolkit.Lib/DeviceSupport/CatalogDeviceSupportProvider.cs` and `UniversalDeviceToolkit.Lib/DeviceSupport/LenovoDeviceSupportProvider.cs`
- **Dependencies**: .NET 10.0 Desktop Runtime; Lenovo drivers are required only for Lenovo hardware-specific controls

## Performance Characteristics

- **Memory Usage**: about 400MB typical with the dashboard and sensors running (Electron UI + .NET Host). Tray-only idle is lower (about 250-350MB) because auxiliary windows are destroyed and sensor/GPU polling stops.
- **CPU Usage**: <1% (tray idle), <5% (active monitoring)
- **Startup Time**: <2 seconds
- **Power Impact**: Electron uses EcoQoS when every window is hidden; the Host stays at Normal priority so hotkeys and automation stay responsive.
- **Installers**: Windows ships a Full offline NSIS package and an Online nsis-web stub (<= 15MB) that downloads the `.nsis.7z` payload from the GitHub Release. Host publish output is pruned (`Scripts/Prune-ShippingFootprint.ps1`); the Online payload keeps English satellites only.

## Security Considerations

- Local-only operation (no cloud dependencies)
- Hardware-level access (requires admin for some features)
- Plugin isolation through dedicated load contexts and manifest checks
- Secure update mechanism (signature verification)

## Future Architecture Goals

- [ ] Plugin SDK documentation and examples
- [ ] Web-based management interface (optional)
- Mobile and Android companion apps are out of scope and are not supported.
- [ ] Cloud sync for settings (privacy-first design)
- [ ] Enhanced telemetry option (opt-in only)
