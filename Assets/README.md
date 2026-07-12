# Assets (single source of truth)

All product brand and marketing media live **here** under the repository root.

| Path | Use |
|---|---|
| `Icon.ico` | Application icon, tray (`AssetResources.icon`), installer `SetupIconFile` |
| `Logo.png` | README, site, about UI resource |
| `Default_exe.png` | Fallback process icon in automation UI |
| `og-preview.png` | Open Graph / social preview |
| `Screenshot_*.png` | README screenshots |
| `Brand/` | Trace symbol SVGs, tray PNGs, multi-size PNG icons |

## App project wiring

`UniversalDeviceToolkit.WPF` does **not** keep a second copy of brand binaries.
It links root files via MSBuild `Link="Assets\..."` so pack URIs stay `Assets/...`.

Code generation only:

- `UniversalDeviceToolkit.WPF/Assets/AssetResources.resx` → embeds `../../Assets/Icon.ico`
- `UniversalDeviceToolkit.WPF/Assets/AssetResources.Designer.cs`

Do not reintroduce `UniversalDeviceToolkit.WPF/Assets/Icon.ico` or `Logo.png`.
