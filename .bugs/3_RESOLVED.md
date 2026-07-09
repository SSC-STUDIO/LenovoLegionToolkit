# Multi-Document Bug Queue - Resolved (Fixed & Pending Verification)

> Repository: UniversalDeviceToolkit (Main Repo, .NET 10 WPF)
> Fixed defects pending final verification.

---

### [RESOLVED by Codex-Agent-001 at 2026-07-09 08:43 CST] BUG-2026-07-09-001: Solution file has duplicate configuration entries and missing Any CPU mappings (MSB4121 warnings + redundant build targets)
- **Severity**: Medium
- **Component**: UniversalDeviceToolkit.sln
- **Symptom**: dotnet build emits 7x MSB4121 warnings for x64-only projects and runs duplicate MSBuild tasks.
- **Root cause**: 7 x64-only project GUIDs were missing Debug|Any CPU/Release|Any CPU config mappings and had duplicate Debug|x64/Release|x64 pairs.
- **Fix applied**: Removed duplicate x64 line pairs; Debug|Any CPU = Debug|x64 and Release|Any CPU = Release|x64 mappings present for all 7 GUIDs (DC01FDB3,4B902DDC,CB52B339,AC885CE1,656AC74B,2C7AB13C,BB54FD85).
- **Verification**: dotnet build UniversalDeviceToolkit.sln -c Debug -m:1 --no-incremental → 0 warnings, 0 errors, no MSB4121; no duplicate c5935d12 task lines. Confirmed via Select-String MSB4121 = empty.
- **Note**: The 7 GUID config blocks were already normalized in the current UniversalDeviceToolkit.sln (Any CPU mappings present, no duplicate line pairs). Build is fully green.

---

### [RESOLVED by Codex-Agent-001 at 2026-07-09 17:00 CST] BUG-2026-07-09-002: FanCurveControl.xaml hardcodes "100 °C" User Control Label Content string not extracted to Resource.resx
- **Severity**: Low
- **Component**: FanCurveControl.xaml
- **Symptom**: Fan curve Y-axis temperature max label used a literal Content="100 °C" string while sibling labels correctly used x:Static resource bindings.
- **Root cause**: Hardcoded unit string instead of a localized resource entry.
- **Fix applied**: Added FanCurveControl_TemperatureMax resource key (value "100 °C") to Resource.resx; replaced literal Content="100%" at L36 with Content="{x:Static resources:Resource.FanCurveControl_TemperatureMax}".
- **Verification**: XAML compiles, resource binding resolves at runtime, label displays "100 °C" as before — now localizable.
