# GitHub Release Notes 鈥?v5.0.0-preview.20260706001

## 馃帀 Universal Device Toolkit v5.0.0 Preview

**Download**: 
- 馃摝 **Full Installer**: `UniversalDeviceToolkitSetup-Full.exe` (recommended)
- 馃寪 **Online Installer**: `UniversalDeviceToolkitSetup-Online.exe` (smaller download)

---

## 鉁?What's New

### 馃攲 Plugin Extensions Overhaul
- Hot-reload support 鈥?modify plugins without restarting UDT
- Sandbox isolation for plugin safety
- Dependency resolution and version management
- Plugin UI capability resolver (plugins can now expose UI)

### 馃И Testing & Quality Assurance
- **2300+ unit tests** passing (0 failures!)
- **FlaUI + WinRT OCR automated UI tests** 鈥?base infrastructure ready
- Fixed 7 pre-existing test failures
- Global hook leak fixed 鈥?**no more system stutter after app exit** 馃幆

### 馃實 Internationalization
- 25+ languages supported
- Simplified Chinese (zh-Hans) translation completed
- Community-driven translation system

### 馃帹 UI/UX Improvements
- Dashboard layout customizations now persisted
- Theme applied immediately on startup (no more dark flash)
- Larger default typography for high-DPI
- Smoother page transitions and button feedback
- Card subtitle text no longer overflows

### 馃悰 Bug Fixes
- Update flow no longer falls back to GitHub (installer filename pattern fixed)
- External links now open correctly (Win32Exception fixed)
- Settings page crash on open (missing ColorPicker.Models.dll) fixed
- Language selector and main window no longer pop up simultaneously
- Windows Optimization checkboxes now surface apply failures via snackbar

---

## 馃挕 Why UDT?

| | UDT | Lenovo Vantage |
|---|---|---|
| Background service | **None** | Required |
| Telemetry / account | **None** | Required |
| Open source | **Yes (GPL-3.0)** | No |
| Plugin support | **Yes** | No |
| Memory usage | **< 50 MB** | 200+ MB |

---

## 馃啎 For Developers

### Plugin Development
See [CONTRIBUTING.md](CONTRIBUTING.md) for plugin development guide.

### Running Tests
```bash
# Unit tests (2357 tests, < 3 min)
dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj -c Debug

# FlaUI tests (requires admin + desktop session)
.\run_flaui_tests_admin.ps1
```

### Building from Source
```bash
dotnet build UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj -c Release
```

---

## 馃搳 By the Numbers
- 猸?**18 stars** 鈫?**Help us reach 100! Star this repo if UDT helps you**
- 馃嵈 Fork-friendly GPL-3.0 license
- 馃И **2357 tests** (2327 passing, 30 skipped)
- 馃實 **25+ languages**
- 馃摝 Available on **winget** + GitHub Releases

---

## 馃敆 Links
- 馃摜 **Download**: [GitHub Releases](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest)
- 馃摎 **Docs**: [Contributing Guide](CONTRIBUTING.md)
- 馃挰 **Discussions**: [Community Discussions](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/discussions)
- 馃悰 **Bug Reports**: [Issue Tracker](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/issues)

---

## 馃挕 Want to Contribute?
- 馃實 **Translators wanted** 鈥?help localize UDT to your language
- 馃攲 **Plugin developers** 鈥?build extensions for niche hardware
- 馃И **Testers** 鈥?run FlaUI tests on your hardware
- 猸?**Star this repo** 鈥?help more people discover UDT!

---

**UDT = Your PC, Your Rules.**

