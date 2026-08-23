# Assets (single source of truth)

All product brand and marketing media live **here** under the repository root.

| Path | Use |
|---|---|
| `Icon.ico` | Application icon, tray, Electron `buildResources` |
| `Logo.png` | README, site, about UI resource |
| `Default_exe.png` | Fallback process icon in automation UI |
| `og-preview.png` | Open Graph / social preview |
| `Screenshot_*.png` | README screenshots |
| `UDT_Promo.mp4` | 30-second README trailer |
| `UDT_Promo_poster.jpg` | Trailer poster |
| `Brand/` | Trace symbol SVGs, tray PNGs, multi-size PNG icons |

The Electron shell consumes these via `UniversalDeviceToolkit.Electron/buildResources` and `resources/`. Do not reintroduce a WPF `AssetResources.resx` copy of brand binaries.
