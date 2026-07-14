# Multi-language quality & global UI modernization

**Status:** In progress (2026-07-12)  
**Does not replace:** uncommitted startup-lag fixes; leave plugin `ai/`, `build_out.txt`, `build_vivetool.log` untouched.

## Localization

### Culture propagation

`LocalizationHelper.SetLanguageInternal` applies:

1. Thread UI culture  
2. Main WPF `Resource.Culture`  
3. Lib / Automation / Macro `Resource.Culture`  
4. All loaded plugin resource types (`SetPluginResourceCultures`)

After delayed plugin load, call `LocalizationHelper.SetPluginResourceCultures()` again (already done from plugin host paths).

### Fallback order

Exact culture → non-Chinese parents → English.  
Chinese parents are **skipped** unless the requested culture is Chinese.  
Japanese may contain kanji/Japanese punctuation; auditor allows `ja*`.

### Resource quality auditor

`UniversalDeviceToolkit.Lib.Utils.ResourceQualityAuditor` scans `.resx` trees for:

- XML parse errors / duplicate keys  
- Format placeholder mismatch vs English  
- East-Asian contamination in non CJK locales  
- Missing keys (informational — English fallback is expected this round)

## UI radius scale

| Token | Value | Use |
|---|---|---|
| `CornerRadiusCompact` / `SM` / `Small` | 8 | badges, compact chrome |
| `CornerRadiusControl` / `MD` | 12 | buttons, inputs, list items |
| `CornerRadiusCard` / `LG` | 18 | cards, plugin panels |
| `CornerRadiusSurface` / `XL` | 20 | dialogs, main content surface |
| `CornerRadiusRound` | 999 | pills / capsule chips |

Shared styles: `Styles/ButtonStyles.xaml`, `Styles/ControlStyles.xaml` (loaded after WPF-UI defaults).

OSD user-configurable corner radius remains independent; only defaults/preview align to the scale.

## Tests

- `UniversalDeviceToolkit.Tests/WPF/LocalizationAndResourceQualityTests.cs`
