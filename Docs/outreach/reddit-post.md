# Reddit r/LenovoLegion 发帖内容（已更新 v4.2.1）

## Title
Ditched Vantage for this open-source Legion tool — sharing in case it helps someone (v4.2.1)

## Body

Not affiliated, just a user. I got tired of Vantage (background services, Lenovo account, random popups) but still wanted Fn+Q power modes, RGB, and dGPU controls on my Legion.

Been using **Universal Device Toolkit** (formerly Legion Toolkit, now maintained at SSC-STUDIO). Does what I need day-to-day without the bloat. No telemetry, GPL-3.0 if you want to read the code.

**What's new in v4.2.1:**
- 2343/2343 tests passing (full CI automation)
- Multi-language UI strings fixed — no more raw resource keys showing up
- Driver update detection gracefully degrades when WMI is unavailable (used to hang)
- Plugin Extensions page icons render correctly in both Dark and Light themes

Trade-off worth knowing: it **doesn't run a separate background service** — keep it in the tray if you want Fn+Q sync, macros, etc. Fully closing it means some stuff stops working. That's intentional.

Plugin page is nice if you want CPU/GPU/network extras without cramming everything into the base app. Not on a supported Legion? There's a "basic mode" with plugins and general tools; don't expect full hardware control on random brands.

- https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest
- `winget install SSC-STUDIO.LenovoLegionToolkit`

Happy to answer compatibility questions if you've got a specific model.
