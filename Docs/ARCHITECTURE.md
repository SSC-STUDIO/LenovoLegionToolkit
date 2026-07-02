# Universal Device Toolkit Architecture

## Overview

Universal Device Toolkit (UDT, formerly Lenovo Legion Toolkit) is a lightweight Windows WPF desktop application for supported Lenovo hardware control, plugin extensions, and safe basic-mode workflows on other PCs. The application follows a modular architecture pattern with clear separation of concerns and treats plugin extensions as a primary expansion path.

## Quick Start

### For Users

1. **Download** the latest release from [GitHub Releases](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases)
2. **Install** the application by running the installer
3. **Launch** UDT and configure your preferred settings
4. **Use** supported hardware controls or basic-mode plugin and system tools

### For Developers

1. **Prerequisites**: Install .NET 10 SDK and Visual Studio 2022
2. **Clone** the repository: `git clone https://github.com/SSC-STUDIO/UniversalDeviceToolkit.git`
3. **Build** the solution: `dotnet build UniversalDeviceToolkit.sln`
4. **Run** tests: `dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj`
5. **Start** developing! See [AGENTS.md](../AGENTS.md) for detailed development guidelines.

## System Architecture

```
+-----------------------------------------------------------------------+
|                        Universal Device Toolkit                          |
+-----------------------------------------------------------------------+
| Presentation Layer                                                       |
| +--------------------------------------------------------------------+ |
| | UniversalDeviceToolkit.WPF                                      |  |
| | +- Views (Pages, Windows, Controls)                           |  |  |
| | +- ViewModels (MVVM Pattern)                                  |  |  |
| | +- Resources (Styles, Templates, Assets)                      |  |  |
| +--------------------------------------------------------------------+ |
+-----------------------------------------------------------------------+
| Application Layer                                                       |
| +--------------------------------------------------------------------+ |
| | CLI               | Automation        | Macro                   |  |  |
| | UniversalDevice   | UniversalDevice   | UniversalDevice         |  |  |
| | Toolkit.CLI      | Toolkit.Lib.      | Toolkit.Lib.Macro       |  |  |
| |                  | Automation        |                         |  |  |
| +--------------------------------------------------------------------+ |
+-----------------------------------------------------------------------+
| Core Library Layer                                                      |
| +--------------------------------------------------------------------+ |
| | UniversalDeviceToolkit.Lib (assembly: LenovoLegionToolkit.Lib)  |  |
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

## Core Components

### 1. UniversalDeviceToolkit.WPF (Presentation Layer)

The main WPF application implementing MVVM architecture:

- **Pages/**: Main application pages (Dashboard, Settings, Features)
- **Windows/**: Application windows (MainWindow, SettingsWindow)
- **Controls/**: Custom reusable UI controls
- **ViewModels/**: Business logic and state management
- **Behaviors/**: Attached behaviors for XAML
- **Utils/**: UI-related utilities

### 2. UniversalDeviceToolkit.Lib (Core Library; assembly `LenovoLegionToolkit.Lib`)

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
??? plugin.manifest.json    # Authoring manifest (also packaged for compatibility)
??? plugin.json             # Generated runtime manifest output (legacy-compatible)
??? plugin.dll              # Main plugin assembly
??? [dependencies]          # Additional assemblies (e.g. LenovoLegionToolkit.Plugins.SDK.dll)
??? [resources]             # Plugin resources
```

Official plugins are built and published from the separate [UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins) repository; the host loads their packaged output.

### Plugin Types

Host-side categories (legacy):

1. **Feature Plugins**: Add new automation features
2. **Integration Plugins**: Third-party service integrations
3. **Tool Plugins**: Standalone utilities

Author scaffolding in [UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins) uses `settings-only`, `feature-settings`, and `runtime-optimization` templates, which map to settings pages, feature + settings pages, and Windows optimization integrations respectively.

### Plugin Lifecycle

```
Loading �?Initialization �?Registration �?Activation �?Shutdown
  �?           �?             �?             �?          �?  �?           ??????????????????????????????????????????�?  �?                        Active State
  �?  ??�?Disabled/Failed �?Unloaded
```

## Data Flow

### Power Mode Change Flow

```
User Action (UI)
      �?PowerModeSelectorViewModel
      �?PowerModeController.SetModeAsync()
      �?WMI Call (\\ROOT\WMI\Lenovo_Path)
      �?ACPI Communication
      �?Hardware Response
      �?Windows Power Plan Sync
      �?State Update Broadcast
      �?UI Refresh
```

### Game Detection Flow

```
GameDetectionService (Background Monitor)
      �?Window Title / Process Matching
      �?Plugin Notifications
      �?Automation Rules Evaluation
      �?Automatic Actions Execution
```

## Technology Stack

| Layer | Technology/Framework |
|-------|---------------------|
| UI Framework | WPF (.NET 10.0) |
| Architecture | MVVM, Clean Architecture |
| DI Container | Autofac |
| Hardware Access | WMI, ACPI, Windows native APIs |
| Monitoring | Built-in sensors and controller queries |
| Settings | JSON file storage |
| Updates | GitHub Releases API |
| Localization | Crowdin + `crowdin.yml` mapping for multi-module `.resx` files |

## Key Design Decisions

1. **No Background Service**: Application runs only when user is logged in
2. **No Telemetry**: Complete user privacy
3. **Lightweight**: Minimal resource footprint
4. **Plugin Extensibility**: Dynamic module loading for device-specific workflows
5. **Catalog-backed Device Support**: Data-driven hardware/basic-mode profiles across Lenovo families and common PC vendors

## Platform Compatibility

- **Windows**: 10 (1809+), 11 (x64 only)
- **Hardware (code-driven detection)**:
  - Hardware-control profiles: Legion 5/Slim 5/Pro 5, Legion 7/Pro 7/9, Legion Go, LOQ, IdeaPad Gaming, ThinkBook, YOGA, Lenovo Slim, selected legacy Lenovo gaming families
  - Basic-mode profiles: ThinkPad, ThinkCentre, ThinkStation, IdeaCentre, Legion desktop, XiaoXin, V series, Motorola, ASUS, MECHREVO/Mechanical Revolution, Dell, HP, Acer, MSI, Microsoft Surface, GIGABYTE/AORUS, Razer, Samsung, HUAWEI, Xiaomi/Redmi, HONOR, LG, Framework, Panasonic, Dynabook/Toshiba, Fujitsu, VAIO, MEDION, XMG/SCHENKER, System76, Star Labs, Slimbook, Clevo/Tongfang, and generic PCs
  - v4.0 adds a local device-support simulation matrix for ASUS, MECHREVO, HP, Dell, Acer, Xiaomi, and Huawei machine profiles, plus generic CPU/GPU sensor fallback for non-Lenovo basic mode
  - Chinese model naming variants are recognized where hardware control is supported (for example `R7000`, `R9000`, `Y7000`, `Y9000`)
  - Vendor matching normalizes common BIOS/DMI formatting differences so punctuation, casing, spacing, diacritics, and company suffix variants do not block a basic-mode match
  - Detection source: `UniversalDeviceToolkit.Lib/DeviceSupport/CatalogDeviceSupportProvider.cs` and `UniversalDeviceToolkit.Lib/DeviceSupport/LenovoDeviceSupportProvider.cs`
- **Dependencies**: .NET 10.0 Desktop Runtime; Lenovo drivers are required only for Lenovo hardware-specific controls

## Performance Characteristics

- **Memory Usage**: ~50-100 MB (idle)
- **CPU Usage**: <1% (idle), <5% (active monitoring)
- **Startup Time**: <2 seconds
- **Power Impact**: Negligible on battery

## Security Considerations

- Local-only operation (no cloud dependencies)
- Hardware-level access (requires admin for some features)
- Plugin isolation through dedicated load contexts and manifest checks
- Secure update mechanism (signature verification)

## Future Architecture Goals

- [ ] Plugin SDK documentation and examples
- [ ] Web-based management interface (optional)
- [ ] Mobile companion app (future consideration)
- [ ] Cloud sync for settings (privacy-first design)
- [ ] Enhanced telemetry option (opt-in only)
