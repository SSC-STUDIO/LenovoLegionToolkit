# English promotion copy

Replace version numbers and links before posting. **Tone rule**: write like you're replying on Reddit or telling a friend — mention real pain points, be honest about trade-offs, skip press-release words like "empower" or "first-class."

---

## Off the cuff

**One line**

Open-source Legion laptop tool — power modes and RGB without Vantage bloat or a Lenovo account.

**Two sentences**

I uninstalled Vantage and switched to Universal Device Toolkit (used to be called Legion Toolkit). Fn+Q, RGB, and dGPU stuff still work, it doesn't run a background service, and there's no account nonsense. GitHub: SSC-STUDIO/UniversalDeviceToolkit.

---

## Release notes

### Short (GitHub Release)

Universal Device Toolkit vX.Y.Z is out.

Same idea as before: stay light, no background service, no telemetry. Plugin Extensions page still lets you install/update/remove add-ons. If you had Lenovo Legion Toolkit, upgrade in place — settings stick around.

Two installers: Full (languages + device data bundled) and Online (smaller, grabs resources in-app). Grab it from GitHub Releases or winget; check the SHA256 file on the release page if you mirror it.

### Long (forum post)

Universal Device Toolkit vX.Y.Z is out. Quick rundown:

- Brand name is UDT now; old Legion Toolkit installs upgrade cleanly
- Full vs Online installers — pick offline-complete or small download
- Plugin page for CPU/GPU/network/shell/mouse stuff without bloating the main app
- winget/Scoop still use `LenovoLegionToolkit` IDs so upgrades don't break — same app, legacy package name

**Get it**

- GitHub: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest
- winget: `winget install SSC-STUDIO.LenovoLegionToolkit`
- Scoop: `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket && scoop install ssc-studio/lenovolegiontoolkit`

---

## Ready-to-post

### Twitter / X

Uninstalled Lenovo Vantage on my Legion — using Universal Device Toolkit instead. Fn+Q, RGB, dGPU, no account, no telemetry, GPL-3.0. `winget install SSC-STUDIO.LenovoLegionToolkit`  
https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest

### Reddit (r/LenovoLegion — paste as-is)

**Title**: Ditched Vantage for this open-source Legion tool — sharing in case it helps someone

**Body**:

Not affiliated, just a user. I got tired of Vantage (background services, Lenovo account, random popups) but still wanted Fn+Q power modes, RGB, and dGPU controls on my Legion.

Been using **Universal Device Toolkit** (formerly Legion Toolkit, now maintained at SSC-STUDIO). Does what I need day-to-day without the bloat. No telemetry, GPL-3.0 if you want to read the code.

Trade-off worth knowing: it **doesn't run a separate background service** — keep it in the tray if you want Fn+Q sync, macros, etc. Fully closing it means some stuff stops working. That's intentional.

Plugin page is nice if you want CPU/GPU/network extras without cramming everything into the base app. Not on a supported Legion? There's a "basic mode" with plugins and general tools; don't expect full hardware control on random brands.

- https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest
- `winget install SSC-STUDIO.LenovoLegionToolkit`

Happy to answer compatibility questions if you've got a specific model.

### Discord / chat

legion gang — if vantage annoys you, try UDT. fn+q + rgb + dgpu, no account. `winget install SSC-STUDIO.LenovoLegionToolkit`. keep it in tray tho, no bg service by design

### Hacker News (Show HN comment)

Maintainer here / user here — UDT is the community continuation of Lenovo Legion Toolkit. Lightweight Windows utility for Legion/LOQ: power modes, RGB, battery, plugins. No telemetry, GPL-3.0. We kept the old winget ID so upgrades don't break. Honest limit: full hardware control is for supported Lenovo gaming laptops; other PCs get basic mode + plugins.

---

## When someone asks "why not Vantage?"

> Vantage does a lot but feels heavy — account, services, telemetry. UDT is the "I just want Fn+Q and RGB without the baggage" option. Open source, plugin add-ons, CLI if you're into that. You trade always-on background services for keeping the app in the tray when you need reactive features. Not every laptop gets full hardware control.

| | UDT | Vantage |
|---|:---:|:---:|
| Background | Tray only, no extra service | Always on |
| Account | Nope | Yes |
| Open source | GPL-3.0 | No |
| Random Dell laptop | Basic mode + plugins | Nope |

---

## Tags

`#LenovoLegion` `#LOQ` `#OpenSource` `#Windows` `#LenovoVantage`

---

## Before you post

- [ ] Latest release URL
- [ ] Screenshot: `Assets/Screenshot_main.png`
- [ ] Mirrors include SHA256
