# Reddit r/dotnet Post Draft ！ Week 2

**Title:**
> Universal Device Toolkit ！ open-source Vantage alternative for Legion laptops, built with .NET 10 and WPF (plugin-extensible)

**Body:**

Hey r/dotnet,

I'm the maintainer of **Universal Device Toolkit** ([GitHub](https://github.com/SSC-STUDIO/UniversalDeviceToolkit)), a GPL-3.0 open-source Windows app that replaces Lenovo's Vantage for hardware control on Legion/LOQ/IdeaPad Gaming laptops ！ and runs in a basic plugin mode on any PC.

**Why it exists:** Vantage requires a background service, an account, and ships telemetry. UDT does the same Fn+Q modes, fan curves, keyboard RGB, dGPU switching, and battery conservation with zero background services, zero telemetry, and zero accounts.

**Technical highlights for the .NET crowd:**

- **C# / WPF on .NET 10** ！ MVVM architecture with Autofac DI
- **2,500+ unit tests** across 3 test projects (unit, plugin, cross-platform), plus FlaUI + WinRT OCR UI verification
- **78+ language localizations** via Crowdin ！ 152 `.resx` files, CI enforces key completeness across all locales
- **Plugin system** ！ first-class, sandboxed, hot-reloadable. Plugins install/update/configure from inside the app.
- **WMI async timeout enforcement** ！ all WMI queries use async extensions with2,500ms hard timeouts (prevents RDP deadlocks)
- **Zero `.ConfigureAwait(false)` in WPF UI/ViewModels** ！ enforced as a project rule with automated scanning
- **CLI** (`llt.exe`) for scripting and automation

**Performance:** ~50-100 MB idle memory, <2s startup, <1% CPU idle. For comparison, Vantage typically uses200-400 MB.

**Links:**
- GitHub: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
- Latest release: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest
- Scoop install: `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket; scoop install ssc-studio/lenovolegiontoolkit`

I'd love feedback on the plugin architecture and the .NET engineering choices. Contributions welcome!

---

**Posting notes:**
- Post on a Tuesday-Thursday between8-10 AM PT
- Attach `Assets/Screenshot_main.png` as an image post or link
- Disclosure: "I'm the maintainer" ！ required by r/dotnet rules
- Reply to every comment within2 hours for algorithm boost
