# Lenovo Legion Toolkit Architecture

## Overview

Lenovo Legion Toolkit (LLT) is a lightweight Windows WPF desktop application designed to replace Lenovo Vantage for advanced hardware control on Legion series laptops. The application follows a modular architecture pattern with clear separation of concerns.

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Lenovo Legion Toolkit                             │
├─────────────────────────────────────────────────────────────────────────┤
│  Presentation Layer                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  LenovoLegionToolkit.WPF                                         │   │
│  │  ├── Views (Pages, Windows, Controls)                           │   │
│  │  ├── ViewModels (MVVM Pattern)                                  │   │
│  │  └── Resources (Styles, Templates, Assets)                      │   │
│  └─────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────┤
│  Application Layer                                                       │
│  ┌───────────────────┬───────────────────┬─────────────────────────┐  │
│  │ CLI               │ Automation        │ Macro                   │  │
│  │ LenovoLegionToolkit│ LenovoLegionToolkit│ LenovoLegionToolkit    │  │
│  │ .CLI              │ .Lib.Automation   │ .Lib.Macro              │  │
│  └───────────────────┴───────────────────┴─────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────────┤
│  Core Library Layer                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  LenovoLegionToolkit.Lib                                         │   │
│  │  ├── Hardware Controllers (34 modules)                          │   │
│  │  ├── Services (Settings, Messaging, IoC)                        │   │
│  │  ├── Game Detection System                                      │   │
│  │  ├── Plugin System                                              │   │
│  │  └── Native Interop (WMI, ACPI, USB/HID)                      │   │
│  └─────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────┤
│  Infrastructure                                                          │
│  ├── Autofac (Dependency Injection)                                    │
│  ├── HID Sharp (Hardware Interface)                                   │
│  ├── LibreHardwareMonitorLib (System Monitoring)                       │
│  └── Native Windows APIs (WMI, Power, etc.)                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. LenovoLegionToolkit.WPF (Presentation Layer)

The main WPF application implementing MVVM architecture:

- **Pages/**: Main application pages (Dashboard, Settings, Features)
- **Windows/**: Application windows (MainWindow, SettingsWindow)
- **Controls/**: Custom reusable UI controls
- **ViewModels/**: Business logic and state management
- **Behaviors/**: Attached behaviors for XAML
- **Utils/**: UI-related utilities

### 2. LenovoLegionToolkit.Lib (Core Library)

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

### 3. LenovoLegionToolkit.Lib.Automation

Automation system implementing a rule-based engine:

- **Triggers**: Application launch, game detection, AC plugged/unplugged
- **Conditions**: Time-based, power state, user presence
- **Actions**: Power mode change, fan curve, RGB profile, macro activation

### 4. LenovoLegionToolkit.Lib.Macro

Macro recording and playback system:

- Key sequence recording
- Macro storage and management
- Integration with hardware macro keys

### 5. LenovoLegionToolkit.CLI

Command-line interface for headless operation:

- Power mode queries and changes
- Status monitoring
- Automation rule management

## Plugin System Architecture

LLT supports dynamic plugin loading through a structured API:

```
Plugin Structure:
├── Plugin.json              # Plugin manifest
├── plugin.dll              # Main plugin assembly
├── [dependencies]          # Additional assemblies
└── [resources]             # Plugin resources
```

### Plugin Types

1. **Feature Plugins**: Add new automation features
2. **Integration Plugins**: Third-party service integrations
3. **Tool Plugins**: Standalone utilities

### Plugin Lifecycle

```
Loading → Initialization → Registration → Activation → Shutdown
  │            │              │              │           │
  │            └──────────────┴──────────────┴───────────┘
  │                         Active State
  │
  └─→ Disabled/Failed → Unloaded
```

## Data Flow

### Power Mode Change Flow

```
User Action (UI)
      ↓
PowerModeSelectorViewModel
      ↓
PowerModeController.SetModeAsync()
      ↓
WMI Call (\\ROOT\WMI\Lenovo_Path)
      ↓
ACPI Communication
      ↓
Hardware Response
      ↓
Windows Power Plan Sync
      ↓
State Update Broadcast
      ↓
UI Refresh
```

### Game Detection Flow

```
GameDetectionService (Background Monitor)
      ↓
Window Title / Process Matching
      ↓
Plugin Notifications
      ↓
Automation Rules Evaluation
      ↓
Automatic Actions Execution
```

## Technology Stack

| Layer | Technology/Framework |
|-------|---------------------|
| UI Framework | WPF (.NET 8.0) |
| Architecture | MVVM, Clean Architecture |
| DI Container | Autofac |
| Hardware Access | HID Sharp, WMI, ACPI |
| Monitoring | LibreHardwareMonitorLib |
| Settings | JSON file storage |
| Updates | GitHub Releases API |
| Localization | Crowdin (20+ languages) |

## Key Design Decisions

1. **No Background Service**: Application runs only when user is logged in
2. **No Telemetry**: Complete user privacy
3. **Lightweight**: Minimal resource footprint
4. **Plugin Extensibility**: Dynamic module loading
5. **Cross-Generation Support**: Unified API across Legion Gen 6-9

## Platform Compatibility

- **Windows**: 10 (1809+), 11 (x64 only)
- **Hardware**: Legion Gen 6-9, Ideapad Gaming, LOQ series
- **Dependencies**: .NET 8.0 Desktop Runtime, Lenovo drivers

## Performance Characteristics

- **Memory Usage**: ~50-100 MB (idle)
- **CPU Usage**: <1% (idle), <5% (active monitoring)
- **Startup Time**: <2 seconds
- **Power Impact**: Negligible on battery

## Security Considerations

- Local-only operation (no cloud dependencies)
- Hardware-level access (requires admin for some features)
- Plugin sandboxing (limited permissions)
- Secure update mechanism (signature verification)

## Future Architecture Goals

- [ ] Plugin SDK documentation and examples
- [ ] Web-based management interface (optional)
- [ ] Mobile companion app (future consideration)
- [ ] Cloud sync for settings (privacy-first design)
- [ ] Enhanced telemetry option (opt-in only)
