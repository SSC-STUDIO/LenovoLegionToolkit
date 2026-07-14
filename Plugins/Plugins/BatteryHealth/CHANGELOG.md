# Battery Health

## [Unreleased]

### Added
- Initial plugin scaffold
- Full feature page redesign following the CustomMouse pattern (WpfFallbackHelper fallback, DynamicResource theme binding, CornerRadius cards, SymbolIcon glyphs, animated status pills)
- Full settings page redesign following the CustomMouse pattern (hero card, monitoring toggle, threshold sliders with value labels, notifications toggle, save/reload action bar)
- Real async WMI battery diagnostics via BatteryHealthService (Win32_Battery, 3000ms timeout, BatteryHealthStatus enum, BatteryHealthReport with typed Status and WearPercentage)
- Full localization accessor (BatteryHealthText, 40+ keys/format helpers) with neutral/en/zh-Hans resx resources and #nullable enable
- Store promotion: generated store-entry.json and merged battery-health into root store.json with Universal Device Toolkit branding (32 supported languages, BatteryCharge24 icon, #16A34A background)

### Fixed
- Threshold validation: settings UI rejects critical >= low with inline SettingsInvalidThresholds status
- Threshold theory inline-data bug; 16/16 unit tests green (0 warnings, 0 errors)

### Notes
- Internal UniversalDeviceToolkit.Plugins.BatteryHealth namespaces retained for host ABI compatibility (host vendored as UniversalDeviceToolkit.Lib); user-visible/store text branded as Universal Device Toolkit (cross-repo rename TODO, see BUGS.md M-010)
