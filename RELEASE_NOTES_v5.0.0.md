# GitHub Release Notes — v5.0.0-preview.20260706001

## 🎉 Universal Device Toolkit v5.0.0 Preview

**Download**: 
- 📦 **Full Installer**: `UniversalDeviceToolkitSetup-Full.exe` (recommended)
- 🌐 **Online Installer**: `UniversalDeviceToolkitSetup-Online.exe` (smaller download)

---

## ✨ What's New

### 🔌 Plugin Extensions Overhaul
- Hot-reload support — modify plugins without restarting UDT
- Sandbox isolation for plugin safety
- Dependency resolution and version management
- Plugin UI capability resolver (plugins can now expose UI)

### 🧪 Testing & Quality Assurance
- **2300+ unit tests** passing (0 failures!)
- **FlaUI + WinRT OCR automated UI tests** — base infrastructure ready
- Fixed 7 pre-existing test failures
- Global hook leak fixed — **no more system stutter after app exit** 🎯

### 🌍 Internationalization
- 25+ languages supported
- Simplified Chinese (zh-Hans) translation completed
- Community-driven translation system

### 🎨 UI/UX Improvements
- Dashboard layout customizations now persisted
- Theme applied immediately on startup (no more dark flash)
- Larger default typography for high-DPI
- Smoother page transitions and button feedback
- Card subtitle text no longer overflows

### 🐛 Bug Fixes
- Update flow no longer falls back to GitHub (installer filename pattern fixed)
- External links now open correctly (Win32Exception fixed)
- Settings page crash on open (missing ColorPicker.Models.dll) fixed
- Language selector and main window no longer pop up simultaneously
- Windows Optimization checkboxes now surface apply failures via snackbar

---

## 💡 Why UDT?

| | UDT | Lenovo Vantage |
|---|---|---|
| Background service | **None** | Required |
| Telemetry / account | **None** | Required |
| Open source | **Yes (GPL-3.0)** | No |
| Plugin support | **Yes** | No |
| Memory usage | **< 50 MB** | 200+ MB |

---

## 🆕 For Developers

### Plugin Development
See [CONTRIBUTING.md](CONTRIBUTING.md) for plugin development guide.

### Running Tests
```bash
# Unit tests (2343 tests, < 3 min)
dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj -c Debug

# FlaUI tests (requires admin + desktop session)
.\run_flaui_tests_admin.ps1
```

### Building from Source
```bash
dotnet build UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj -c Release
```

---

## 📊 By the Numbers
- ⭐ **18 stars** → **Help us reach 100! Star this repo if UDT helps you**
- 🍴 Fork-friendly GPL-3.0 license
- 🧪 **2343 tests** (2323 passing, 20 skipped)
- 🌍 **25+ languages**
- 📦 Available on **winget** + GitHub Releases

---

## 🔗 Links
- 📥 **Download**: [GitHub Releases](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest)
- 📚 **Docs**: [Contributing Guide](CONTRIBUTING.md)
- 💬 **Discussions**: [Community Discussions](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/discussions)
- 🐛 **Bug Reports**: [Issue Tracker](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/issues)

---

## 💡 Want to Contribute?
- 🌍 **Translators wanted** — help localize UDT to your language
- 🔌 **Plugin developers** — build extensions for niche hardware
- 🧪 **Testers** — run FlaUI tests on your hardware
- ⭐ **Star this repo** — help more people discover UDT!

---

**UDT = Your PC, Your Rules.**
